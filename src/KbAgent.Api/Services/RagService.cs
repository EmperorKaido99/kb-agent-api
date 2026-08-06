using KbAgent.Api.Configuration;
using KbAgent.Api.Models;
using Microsoft.Extensions.Options;

namespace KbAgent.Api.Services;

/// <summary>Orchestrates the RAG flow: embed → vector search → prompt → generate, and the ingest flow.</summary>
public sealed class RagService(
    IOllamaClient ollamaClient,
    IOllamaLoadBalancer loadBalancer,
    IVectorStore vectorStore,
    IChunkingService chunkingService,
    IOptions<OllamaOptions> ollamaOptions,
    IOptions<RagOptions> ragOptions) : IRagService
{
    private readonly OllamaOptions _ollamaOptions = ollamaOptions.Value;
    private readonly RagOptions _ragOptions = ragOptions.Value;

    public async Task<AskResponse> AskAsync(string question, CancellationToken ct = default)
    {
        var backend = await loadBalancer.GetAvailableBackendAsync(ct);
        var questionEmbedding = await ollamaClient.EmbedAsync(backend, _ollamaOptions.EmbeddingModel, question, ct);
        var sources = await vectorStore.SearchAsync(questionEmbedding, _ragOptions.TopK, ct);

        var prompt = BuildPrompt(question, sources);
        var answer = await ollamaClient.GenerateAsync(backend, _ollamaOptions.GenerationModel, prompt, ct);

        return new AskResponse(answer.Trim(), sources);
    }

    public async Task<IngestResponse> IngestAsync(string source, string text, CancellationToken ct = default)
    {
        await vectorStore.EnsureCollectionAsync(ct);

        // Delete any chunks from a previous ingest of this source first, so re-ingesting (e.g. from the
        // scheduled folder scan picking up an edited file) replaces rather than duplicates them.
        await vectorStore.DeleteBySourceAsync(source, ct);

        var backend = await loadBalancer.GetAvailableBackendAsync(ct);
        var chunkTexts = chunkingService.Chunk(text);

        var chunks = new List<DocumentChunk>(chunkTexts.Count);
        foreach (var chunkText in chunkTexts)
        {
            var embedding = await ollamaClient.EmbedAsync(backend, _ollamaOptions.EmbeddingModel, chunkText, ct);
            chunks.Add(new DocumentChunk(Guid.NewGuid().ToString(), source, chunkText, embedding));
        }

        if (chunks.Count > 0)
        {
            await vectorStore.UpsertChunksAsync(chunks, ct);
        }

        return new IngestResponse(source, chunks.Count);
    }

    private static string BuildPrompt(string question, IReadOnlyList<SourceSnippet> sources)
    {
        if (sources.Count == 0)
        {
            return $"""
                No matching internal knowledge base context was found for this question. Say so explicitly,
                then answer using your general knowledge if you can.

                Question: {question}
                """;
        }

        var context = string.Join(
            "\n\n",
            sources.Select((s, i) => $"[{i + 1}] (source: {s.Source})\n{s.Text}"));

        return $"""
            Answer the question using ONLY the context below. If the context doesn't contain the answer,
            say you don't know rather than guessing. Cite sources by their [number] where relevant.

            Context:
            {context}

            Question: {question}
            """;
    }
}
