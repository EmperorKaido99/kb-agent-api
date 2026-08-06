using KbAgent.Api.Models;

namespace KbAgent.Api.Services;

public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken ct = default);

    Task UpsertChunksAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default);

    Task<IReadOnlyList<SourceSnippet>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken ct = default);

    /// <summary>Deletes all previously stored chunks for a source, so re-ingesting it doesn't duplicate points.</summary>
    Task DeleteBySourceAsync(string source, CancellationToken ct = default);
}
