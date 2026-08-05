namespace KbAgent.Api.Services;

public interface IOllamaClient
{
    Task<bool> IsHealthyAsync(string backendBaseUrl, CancellationToken ct = default);

    Task<float[]> EmbedAsync(string backendBaseUrl, string model, string text, CancellationToken ct = default);

    Task<string> GenerateAsync(string backendBaseUrl, string model, string prompt, CancellationToken ct = default);
}
