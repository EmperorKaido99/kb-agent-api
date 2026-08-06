using System.Text.Json;
using KbAgent.Api.Configuration;
using Microsoft.Extensions.Options;

namespace KbAgent.Api.Services;

public sealed class JsonFileIngestStateStore(IOptions<KnowledgeFolderOptions> options) : IIngestStateStore
{
    public async Task<IReadOnlyDictionary<string, string>> LoadAsync(CancellationToken ct = default)
    {
        var path = options.Value.StateFilePath;
        if (!File.Exists(path))
        {
            return new Dictionary<string, string>();
        }

        await using var stream = File.OpenRead(path);
        var data = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(stream, cancellationToken: ct);
        return data ?? new Dictionary<string, string>();
    }

    public async Task SaveAsync(IReadOnlyDictionary<string, string> fingerprints, CancellationToken ct = default)
    {
        var path = options.Value.StateFilePath;
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, fingerprints, cancellationToken: ct);
    }
}
