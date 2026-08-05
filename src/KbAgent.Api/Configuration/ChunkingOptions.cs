namespace KbAgent.Api.Configuration;

public sealed class ChunkingOptions
{
    public const string SectionName = "Chunking";

    public int ChunkSizeChars { get; set; } = 1000;
    public int ChunkOverlapChars { get; set; } = 200;
}
