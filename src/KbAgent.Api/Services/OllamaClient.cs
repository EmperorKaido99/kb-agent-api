using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace KbAgent.Api.Services;

/// <summary>Talks to a single Ollama backend's REST API (https://github.com/ollama/ollama/blob/main/docs/api.md).</summary>
public sealed class OllamaClient(HttpClient httpClient, ILogger<OllamaClient> logger) : IOllamaClient
{
    public async Task<bool> IsHealthyAsync(string backendBaseUrl, CancellationToken ct = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(CombineUrl(backendBaseUrl, "/api/tags"), ct);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(ex, "Health check failed for Ollama backend {BackendUrl}", backendBaseUrl);
            return false;
        }
    }

    public async Task<float[]> EmbedAsync(string backendBaseUrl, string model, string text, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            CombineUrl(backendBaseUrl, "/api/embeddings"),
            new EmbeddingRequest(model, text),
            ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken: ct);
        return result?.Embedding ?? throw new InvalidOperationException(
            $"Ollama backend {backendBaseUrl} returned an empty embedding response.");
    }

    public async Task<string> GenerateAsync(string backendBaseUrl, string model, string prompt, CancellationToken ct = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            CombineUrl(backendBaseUrl, "/api/generate"),
            new GenerateRequest(model, prompt, Stream: false),
            ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GenerateResponse>(cancellationToken: ct);
        return result?.Response ?? throw new InvalidOperationException(
            $"Ollama backend {backendBaseUrl} returned an empty generate response.");
    }

    private static string CombineUrl(string baseUrl, string path) => $"{baseUrl.TrimEnd('/')}{path}";

    private sealed record EmbeddingRequest(string Model, string Prompt);

    private sealed record EmbeddingResponse([property: JsonPropertyName("embedding")] float[] Embedding);

    private sealed record GenerateRequest(string Model, string Prompt, bool Stream);

    private sealed record GenerateResponse([property: JsonPropertyName("response")] string Response);
}
