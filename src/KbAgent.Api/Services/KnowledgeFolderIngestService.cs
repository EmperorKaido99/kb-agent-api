using KbAgent.Api.Configuration;
using KbAgent.Api.Services.Extraction;
using Microsoft.Extensions.Options;

namespace KbAgent.Api.Services;

/// <summary>
/// Timer-driven "cron" replacement (see roadmap Step 5): periodically scans a configured local folder for
/// new/changed documents, extracts their text, and feeds it into the existing ingest pipeline. The folder can be
/// a plain local directory or a OneDrive/Google Drive desktop-sync mount — both are just local paths to this
/// service.
/// </summary>
public sealed class KnowledgeFolderIngestService(
    IServiceScopeFactory scopeFactory,
    IIngestStateStore stateStore,
    IDocumentTextExtractorFactory extractorFactory,
    IOptions<KnowledgeFolderOptions> options,
    ILogger<KnowledgeFolderIngestService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var folderPath = options.Value.Path;
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            logger.LogInformation("KnowledgeFolder:Path is not configured — folder-based ingestion is disabled.");
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.ScanIntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                await ScanOnceAsync(folderPath, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Knowledge folder scan failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ScanOnceAsync(string folderPath, CancellationToken ct)
    {
        if (!Directory.Exists(folderPath))
        {
            logger.LogWarning("Knowledge folder {Path} does not exist — skipping this scan.", folderPath);
            return;
        }

        var previous = await stateStore.LoadAsync(ct);
        var current = new Dictionary<string, string>();

        foreach (var filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
        {
            ct.ThrowIfCancellationRequested();
            if (extractorFactory.GetExtractor(filePath) is null)
            {
                continue;
            }

            var relativePath = Path.GetRelativePath(folderPath, filePath);
            var fileInfo = new FileInfo(filePath);
            current[relativePath] = KnowledgeFolderChangeDetector.ComputeFingerprint(fileInfo.Length, fileInfo.LastWriteTimeUtc);
        }

        var filesToProcess = KnowledgeFolderChangeDetector.GetChangedOrNewFiles(current, previous);
        if (filesToProcess.Count == 0)
        {
            logger.LogInformation("Knowledge folder scan: no new or changed files under {Path}.", folderPath);
            await stateStore.SaveAsync(current, ct);
            return;
        }

        logger.LogInformation("Knowledge folder scan: {Count} new/changed file(s) to ingest.", filesToProcess.Count);

        using var scope = scopeFactory.CreateScope();
        var ragService = scope.ServiceProvider.GetRequiredService<IRagService>();

        // Only persist fingerprints for files that actually succeeded. A failed file (e.g. Qdrant/Ollama
        // temporarily unreachable) must NOT be recorded as processed, so the next scan retries it instead of
        // silently treating a never-ingested file as done.
        var stateToSave = new Dictionary<string, string>(current);
        foreach (var relativePath in filesToProcess)
        {
            ct.ThrowIfCancellationRequested();
            var succeeded = await IngestFileAsync(folderPath, relativePath, ragService, ct);
            if (!succeeded)
            {
                stateToSave.Remove(relativePath);
            }
        }

        await stateStore.SaveAsync(stateToSave, ct);
    }

    private async Task<bool> IngestFileAsync(string folderPath, string relativePath, IRagService ragService, CancellationToken ct)
    {
        var fullPath = Path.Combine(folderPath, relativePath);
        var extractor = extractorFactory.GetExtractor(fullPath);
        if (extractor is null)
        {
            return false;
        }

        try
        {
            string text;
            await using (var stream = File.OpenRead(fullPath))
            {
                text = await extractor.ExtractTextAsync(stream, ct);
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                logger.LogWarning("No text extracted from {File} — skipping ingest.", relativePath);
                // Nothing to retry differently next time without the file changing, so this counts as "handled".
                return true;
            }

            var result = await ragService.IngestAsync(relativePath, text, ct);
            logger.LogInformation("Ingested {File}: {ChunkCount} chunk(s).", relativePath, result.ChunkCount);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Failed to ingest {File}. Will retry on the next scan.", relativePath);
            return false;
        }
    }
}
