using KbAgent.Api.Configuration;
using KbAgent.Api.Services;
using Microsoft.Extensions.Options;

namespace KbAgent.Api.Tests.Services;

public class ChunkingServiceTests
{
    [Fact]
    public void Chunk_EmptyText_ReturnsNoChunks()
    {
        var service = CreateService(chunkSize: 10, overlap: 2);

        var result = service.Chunk("");

        Assert.Empty(result);
    }

    [Fact]
    public void Chunk_TextShorterThanChunkSize_ReturnsSingleChunk()
    {
        var service = CreateService(chunkSize: 100, overlap: 10);

        var result = service.Chunk("short text");

        var chunk = Assert.Single(result);
        Assert.Equal("short text", chunk);
    }

    [Fact]
    public void Chunk_TextLongerThanChunkSize_ReturnsOverlappingChunks()
    {
        var service = CreateService(chunkSize: 10, overlap: 3);
        var text = new string('a', 25);

        var result = service.Chunk(text);

        // step = chunkSize - overlap = 7; starts: 0, 7, 14, 21 -> 4 chunks
        Assert.Equal(4, result.Count);
        Assert.All(result, chunk => Assert.True(chunk.Length <= 10));
    }

    [Fact]
    public void Chunk_CoversEntireText()
    {
        var service = CreateService(chunkSize: 5, overlap: 1);
        var text = "abcdefghijklmno";

        var result = service.Chunk(text);

        Assert.True(result.Count > 0);
        Assert.EndsWith("o", result[^1]);
    }

    private static ChunkingService CreateService(int chunkSize, int overlap)
    {
        var options = Options.Create(new ChunkingOptions
        {
            ChunkSizeChars = chunkSize,
            ChunkOverlapChars = overlap,
        });
        return new ChunkingService(options);
    }
}
