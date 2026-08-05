namespace KbAgent.Api.Services;

public interface IOllamaLoadBalancer
{
    /// <summary>Returns the base URL of a healthy Ollama backend, round-robin across configured backends.</summary>
    /// <exception cref="InvalidOperationException">No backend is configured, or none respond to a health check.</exception>
    Task<string> GetAvailableBackendAsync(CancellationToken ct = default);
}
