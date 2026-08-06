namespace KbAgent.Api.Configuration;

public sealed class KnowledgeFolderOptions
{
    public const string SectionName = "KnowledgeFolder";

    /// <summary>Root folder scanned recursively for documents. Empty/unset disables folder-based ingestion.</summary>
    public string Path { get; set; } = "";

    public int ScanIntervalMinutes { get; set; } = 15;

    /// <summary>Where per-file fingerprints are persisted, to detect new/changed files between scans.</summary>
    public string StateFilePath { get; set; } = "knowledge-folder-state.json";
}
