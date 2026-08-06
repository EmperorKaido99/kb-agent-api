namespace KbAgent.Api.Services;

/// <summary>Persists per-file fingerprints (relative path → fingerprint) between knowledge-folder scans.</summary>
public interface IIngestStateStore
{
    Task<IReadOnlyDictionary<string, string>> LoadAsync(CancellationToken ct = default);

    Task SaveAsync(IReadOnlyDictionary<string, string> fingerprints, CancellationToken ct = default);
}
