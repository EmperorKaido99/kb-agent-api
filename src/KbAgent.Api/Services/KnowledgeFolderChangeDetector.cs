namespace KbAgent.Api.Services;

/// <summary>Pure diff logic: which files are new or changed since the last scan, by fingerprint comparison.</summary>
public static class KnowledgeFolderChangeDetector
{
    public static IReadOnlyList<string> GetChangedOrNewFiles(
        IReadOnlyDictionary<string, string> currentFingerprints,
        IReadOnlyDictionary<string, string> previousFingerprints)
    {
        return currentFingerprints
            .Where(kv => !previousFingerprints.TryGetValue(kv.Key, out var previous) || previous != kv.Value)
            .Select(kv => kv.Key)
            .ToList();
    }

    public static string ComputeFingerprint(long fileSizeBytes, DateTime lastWriteTimeUtc) =>
        $"{fileSizeBytes}:{lastWriteTimeUtc.Ticks}";
}
