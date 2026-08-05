using KbAgent.Api.Configuration;
using Microsoft.Extensions.Options;

namespace KbAgent.Api.Services;

/// <summary>Splits text into fixed-size, overlapping character chunks for embedding.</summary>
public sealed class ChunkingService(IOptions<ChunkingOptions> options) : IChunkingService
{
    public IReadOnlyList<string> Chunk(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var chunkSize = Math.Max(1, options.Value.ChunkSizeChars);
        var overlap = Math.Clamp(options.Value.ChunkOverlapChars, 0, chunkSize - 1);
        var step = chunkSize - overlap;

        var chunks = new List<string>();
        for (var start = 0; start < text.Length; start += step)
        {
            var length = Math.Min(chunkSize, text.Length - start);
            var chunk = text.Substring(start, length).Trim();
            if (chunk.Length > 0)
            {
                chunks.Add(chunk);
            }

            if (start + length >= text.Length)
            {
                break;
            }
        }

        return chunks;
    }
}
