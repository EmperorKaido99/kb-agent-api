using KbAgent.Api.Configuration;
using Microsoft.Extensions.Options;

namespace KbAgent.Api.Services;

/// <summary>Round-robins across configured Ollama backends, skipping ones that fail a health check.</summary>
public sealed class OllamaLoadBalancer(IOllamaClient ollamaClient, IOptions<OllamaOptions> options) : IOllamaLoadBalancer
{
    private readonly object _lock = new();
    private int _nextIndex;

    public async Task<string> GetAvailableBackendAsync(CancellationToken ct = default)
    {
        var backends = options.Value.BackendBaseUrls;
        if (backends.Count == 0)
        {
            throw new InvalidOperationException("No Ollama backends are configured.");
        }

        for (var attempt = 0; attempt < backends.Count; attempt++)
        {
            var backend = backends[NextIndex(backends.Count)];
            if (await ollamaClient.IsHealthyAsync(backend, ct))
            {
                return backend;
            }
        }

        throw new InvalidOperationException("No healthy Ollama backend is currently available.");
    }

    private int NextIndex(int count)
    {
        lock (_lock)
        {
            var index = _nextIndex % count;
            _nextIndex = (_nextIndex + 1) % count;
            return index;
        }
    }
}
