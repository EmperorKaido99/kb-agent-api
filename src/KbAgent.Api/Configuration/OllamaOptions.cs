namespace KbAgent.Api.Configuration;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";

    public List<string> BackendBaseUrls { get; set; } = [];
    public string GenerationModel { get; set; } = "qwen3:1.7b";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public int RequestTimeoutSeconds { get; set; } = 60;
}
