namespace KbAgent.Api.Configuration;

public sealed class QdrantOptions
{
    public const string SectionName = "Qdrant";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6334;
    public bool UseTls { get; set; }
    public string CollectionName { get; set; } = "kb-chunks";
    public int VectorSize { get; set; } = 768;
}
