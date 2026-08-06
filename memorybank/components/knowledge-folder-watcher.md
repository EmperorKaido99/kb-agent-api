# Knowledge Folder Watcher

## Overview
Automatically keeps the knowledge base in sync with a folder of source documents (PowerPoint, Word, Excel, PDF,
plain text/Markdown, and images) — the roadmap's "cron re-indexing" (Step 5), built as an in-process scheduled
service instead of OS-level cron.

## Business Rules
- The watched folder is entirely configurable (`KnowledgeFolder__Path`) — a plain local folder or a
  OneDrive/Google Drive folder synced to disk via their desktop apps both work identically, since the app only
  ever sees a local path.
- Folder-based ingestion is **off by default** (empty `Path`) — must be explicitly configured, so `dotnet run`
  in local dev doesn't unexpectedly start scanning the filesystem.
- Only files with a supported extension are considered: `.docx`, `.pptx`, `.xlsx`, `.pdf`, `.txt`, `.md`,
  `.png`/`.jpg`/`.jpeg`/`.bmp`/`.tif`/`.tiff`. Anything else (audio, zip, etc.) is silently skipped.
- Change detection is a cheap fingerprint (file size + last-write-time), not a full content hash — new/changed
  files are re-ingested, unchanged files are skipped, on every scan (interval: `KnowledgeFolder__ScanIntervalMinutes`,
  default 15 min).
- **A file is only marked "processed" in the state file if ingestion actually succeeded.** If Ollama or Qdrant is
  temporarily unreachable, the file's fingerprint is *not* persisted, so it's retried on the next scan instead of
  silently being treated as done forever.
- Re-ingesting a changed file replaces its old chunks (via `RagService.IngestAsync`'s delete-by-source step) —
  no duplicate chunks in Qdrant, even across many scan cycles of an edited file.
- A file that extracts to empty/whitespace text (e.g. an image OCR fails, or a scanned PDF with no text layer) is
  logged as skipped and *is* marked processed — there's nothing that would change about it without the file
  itself changing.
- Scanned/image-only PDF pages (no text layer) are skipped, not OCR'd — out of scope for this pass.
- Audio files are not supported — deferred pending confirmed need (heavier dependency: local speech-to-text).

## Data Flow

```
Timer tick → KnowledgeFolderIngestService.ScanOnceAsync
  → enumerate KnowledgeFolder:Path recursively, compute fingerprint per supported file
  → KnowledgeFolderChangeDetector.GetChangedOrNewFiles(current, previous)
  → for each changed/new file:
      DocumentTextExtractorFactory.GetExtractor(path) → extractor.ExtractTextAsync(stream)
      → RagService.IngestAsync(relativePath, text)
  → persist fingerprints for files that succeeded only (JsonFileIngestStateStore)
```

### Step-by-step:
1. On a timer (`PeriodicTimer`), the background service checks whether `KnowledgeFolder:Path` exists.
2. It enumerates every file under that path recursively, filtering to extensions a registered extractor handles.
3. For each file, it computes a fingerprint (`size:lastWriteTimeUtcTicks`) and compares against the previous
   scan's saved fingerprints (`KnowledgeFolder:StateFilePath`, a JSON file).
4. New/changed files are extracted (format-specific extractor) and ingested via the same `RagService.IngestAsync`
   used by `POST /api/ingest` — so folder-based and manual ingestion share identical chunking/embedding/storage
   logic and the idempotent-replace behavior.
5. Only files that ingested successfully have their new fingerprint saved; failures are dropped from the saved
   state so they're retried next cycle.

## Validation Rules

| Condition | Behavior |
|-----------|----------|
| `KnowledgeFolder:Path` empty | Folder scanning disabled entirely (logged once, service exits cleanly) |
| Configured folder doesn't exist | Scan skipped for that cycle, retried next interval (e.g. transient mount issue) |
| Unsupported file extension | Silently skipped (not an error) |
| Extraction yields empty text | Logged warning, file marked processed (nothing to retry without a file change) |
| Ingest call fails (Ollama/Qdrant down) | Logged error, file NOT marked processed — retried next scan |

## Key Files

| File | Role |
|------|------|
| `src/KbAgent.Api/Services/KnowledgeFolderIngestService.cs` | The `BackgroundService` — scan loop, orchestration |
| `src/KbAgent.Api/Services/KnowledgeFolderChangeDetector.cs` | Pure diff logic (fingerprint comparison) |
| `src/KbAgent.Api/Services/JsonFileIngestStateStore.cs` | Persists fingerprints to a JSON file on disk |
| `src/KbAgent.Api/Services/Extraction/*` | Per-format text extractors + `DocumentTextExtractorFactory` |
| `src/KbAgent.Api/Configuration/KnowledgeFolderOptions.cs` | Path, scan interval, state file path |
| `src/KbAgent.Api/Services/RagService.cs` | `IngestAsync` — shared with manual `/api/ingest`, now idempotent |

## Edge Cases & Gotchas
- The Tesseract NuGet package (charlesw/tesseract) only ships **Windows** native binaries — using it directly
  would silently fail at runtime in the Linux Docker container. `ImageOcrTextExtractor` shells out to the
  `tesseract` CLI instead, which works identically cross-platform as long as Tesseract is installed (it is, in
  the Docker image; must be installed separately for bare-metal `dotnet run`).
- PDF extraction (`PdfPig`) only reads the text layer — a scanned PDF with no embedded text produces no output
  for that file's pages and is logged as such, not OCR'd.
- Change detection uses size+mtime, not a content hash — a file rewritten with identical bytes but a refreshed
  mtime (e.g. re-saved without edits) is treated as changed and re-ingested. Harmless (idempotent replace), just
  not maximally efficient.
- Deleting a file from the watched folder does **not** delete its chunks from Qdrant — the watcher only reacts to
  new/changed files, not removals. Not handled in this pass.

## Related Components
- [kb-ingest.md](./kb-ingest.md) — the underlying ingest pipeline this watcher drives automatically
- [rag-ask.md](./rag-ask.md) — consumes what this flow keeps up to date
