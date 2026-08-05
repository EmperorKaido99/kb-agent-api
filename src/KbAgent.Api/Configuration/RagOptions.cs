namespace KbAgent.Api.Configuration;

public sealed class RagOptions
{
    public const string SectionName = "Rag";

    public int TopK { get; set; } = 4;
}
