using KbAgent.Api.Configuration;
using KbAgent.Api.Middleware;
using KbAgent.Api.Models;
using KbAgent.Api.Services;
using KbAgent.Api.Services.Extraction;
using Microsoft.Extensions.Options;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OllamaOptions>(builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection(QdrantOptions.SectionName));
builder.Services.Configure<ChunkingOptions>(builder.Configuration.GetSection(ChunkingOptions.SectionName));
builder.Services.Configure<RagOptions>(builder.Configuration.GetSection(RagOptions.SectionName));
builder.Services.Configure<ApiUsersOptions>(builder.Configuration.GetSection(ApiUsersOptions.SectionName));
builder.Services.Configure<KnowledgeFolderOptions>(builder.Configuration.GetSection(KnowledgeFolderOptions.SectionName));
builder.Services.Configure<OcrOptions>(builder.Configuration.GetSection(OcrOptions.SectionName));

// `dotnet run -- create-user <username>` short-circuits before the web host starts — generates a token,
// stores its hash, and prints the plaintext token once. No auth required to run this (it's a local CLI command).
if (args.Length > 0 && string.Equals(args[0], "create-user", StringComparison.OrdinalIgnoreCase))
{
    return await RunCreateUserCommandAsync(args, builder.Configuration);
}

builder.Services.AddHttpClient<IOllamaClient, OllamaClient>((sp, client) =>
{
    var ollamaOptions = sp.GetRequiredService<IOptions<OllamaOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(ollamaOptions.RequestTimeoutSeconds);
});

builder.Services.AddSingleton(sp =>
{
    var qdrantOptions = sp.GetRequiredService<IOptions<QdrantOptions>>().Value;
    return new QdrantClient(qdrantOptions.Host, qdrantOptions.Port, qdrantOptions.UseTls);
});
builder.Services.AddSingleton<IVectorStore, QdrantVectorStore>();
builder.Services.AddSingleton<IChunkingService, ChunkingService>();
builder.Services.AddSingleton<IOllamaLoadBalancer, OllamaLoadBalancer>();
builder.Services.AddScoped<IRagService, RagService>();

builder.Services.AddSingleton<IDocumentTextExtractor, PlainTextExtractor>();
builder.Services.AddSingleton<IDocumentTextExtractor, WordTextExtractor>();
builder.Services.AddSingleton<IDocumentTextExtractor, PowerPointTextExtractor>();
builder.Services.AddSingleton<IDocumentTextExtractor, ExcelTextExtractor>();
builder.Services.AddSingleton<IDocumentTextExtractor, PdfTextExtractor>();
builder.Services.AddSingleton<IDocumentTextExtractor, ImageOcrTextExtractor>();
builder.Services.AddSingleton<IDocumentTextExtractorFactory, DocumentTextExtractorFactory>();
builder.Services.AddSingleton<IIngestStateStore, JsonFileIngestStateStore>();
builder.Services.AddHostedService<KnowledgeFolderIngestService>();
builder.Services.AddSingleton<IApiUserStore, JsonFileApiUserStore>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // TLS is terminated by the reverse proxy in front of this API outside dev (see roadmap Step 5).
    app.UseHttpsRedirection();
}

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseMiddleware<ApiKeyAuthMiddleware>());

app.MapGet("/health", async (IOllamaClient ollamaClient, IOptions<OllamaOptions> ollamaOptions, CancellationToken ct) =>
{
    var backendStatuses = new List<object>();
    foreach (var backend in ollamaOptions.Value.BackendBaseUrls)
    {
        backendStatuses.Add(new { backend, healthy = await ollamaClient.IsHealthyAsync(backend, ct) });
    }

    return Results.Ok(new { status = "ok", backends = backendStatuses });
})
.WithName("Health");

app.MapPost("/api/ask", async (AskRequest request, IRagService ragService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest("Question must not be empty.");
    }

    var response = await ragService.AskAsync(request.Question, ct);
    return Results.Ok(response);
})
.WithName("Ask");

app.MapPost("/api/ingest", async (IngestRequest request, IRagService ragService, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Source) || string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest("Source and text must not be empty.");
    }

    var response = await ragService.IngestAsync(request.Source, request.Text, ct);
    return Results.Ok(response);
})
.WithName("Ingest");

app.Run();
return 0;

static async Task<int> RunCreateUserCommandAsync(string[] args, IConfiguration configuration)
{
    if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
    {
        Console.Error.WriteLine("Usage: dotnet run -- create-user <username>");
        return 1;
    }

    var username = args[1];
    var usersOptions = configuration.GetSection(ApiUsersOptions.SectionName).Get<ApiUsersOptions>() ?? new ApiUsersOptions();
    var store = new JsonFileApiUserStore(Options.Create(usersOptions));

    var users = new Dictionary<string, string>(await store.LoadAsync());
    var (rawToken, tokenHash) = ApiTokenHasher.GenerateToken();
    users[username] = tokenHash;
    await store.SaveAsync(users);

    Console.WriteLine($"User '{username}' created (credential file: {usersOptions.FilePath}).");
    Console.WriteLine();
    Console.WriteLine("Token — save this now, it will not be shown again:");
    Console.WriteLine(rawToken);
    Console.WriteLine();
    Console.WriteLine("Use it as HTTP Basic Auth:");
    Console.WriteLine($"  curl -u {username}:{rawToken} -H \"Content-Type: application/json\" \\");
    Console.WriteLine("    -d '{\"question\":\"...\"}' http://localhost:8080/api/ask");
    return 0;
}
