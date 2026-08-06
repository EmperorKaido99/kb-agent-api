using KbAgent.Api.Configuration;
using KbAgent.Api.Models;
using Microsoft.Extensions.Options;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace KbAgent.Api.Services;

public sealed class QdrantVectorStore(QdrantClient client, IOptions<QdrantOptions> options) : IVectorStore
{
    private readonly QdrantOptions _options = options.Value;

    public async Task EnsureCollectionAsync(CancellationToken ct = default)
    {
        var exists = await client.CollectionExistsAsync(_options.CollectionName, ct);
        if (!exists)
        {
            await client.CreateCollectionAsync(
                _options.CollectionName,
                new VectorParams { Size = (ulong)_options.VectorSize, Distance = Distance.Cosine },
                cancellationToken: ct);
        }
    }

    public async Task UpsertChunksAsync(IReadOnlyList<DocumentChunk> chunks, CancellationToken ct = default)
    {
        var points = chunks.Select(chunk =>
        {
            var point = new PointStruct
            {
                Id = new PointId { Uuid = chunk.Id },
                Vectors = chunk.Embedding,
            };
            point.Payload["source"] = chunk.Source;
            point.Payload["text"] = chunk.Text;
            return point;
        }).ToList();

        await client.UpsertAsync(_options.CollectionName, points, cancellationToken: ct);
    }

    public async Task DeleteBySourceAsync(string source, CancellationToken ct = default)
    {
        await client.DeleteAsync(_options.CollectionName, Conditions.MatchKeyword("source", source), cancellationToken: ct);
    }

    public async Task<IReadOnlyList<SourceSnippet>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken ct = default)
    {
        var results = await client.QueryAsync(
            _options.CollectionName,
            query: queryEmbedding,
            limit: (ulong)topK,
            cancellationToken: ct);

        return results
            .Select(point => new SourceSnippet(
                point.Payload["source"].StringValue,
                point.Payload["text"].StringValue,
                point.Score))
            .ToList();
    }
}
