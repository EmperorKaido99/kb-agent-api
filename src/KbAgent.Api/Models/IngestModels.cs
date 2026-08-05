namespace KbAgent.Api.Models;

public sealed record IngestRequest(string Source, string Text);

public sealed record IngestResponse(string Source, int ChunkCount);

public sealed record DocumentChunk(string Id, string Source, string Text, float[] Embedding);
