using KbAgent.Api.Models;

namespace KbAgent.Api.Services;

public interface IVectorStore
{
    Task EnsureCollectionAsync(CancellationToken ct = default);

    Task UpsertChunksAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default);

    Task<IReadOnlyList<SourceSnippet>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken ct = default);
}
