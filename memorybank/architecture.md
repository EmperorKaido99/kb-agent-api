# Architecture

## Project Structure

```
kb-agent-api/
├── src/
│   ├── KbAgent.Api/              ASP.NET Core 8 minimal-API project (the gateway API)
│   │   ├── Program.cs            DI wiring, middleware pipeline, endpoint mapping
│   │   ├── Configuration/        Strongly-typed options (Ollama, Qdrant, Chunking, Rag, ApiUsers, KnowledgeFolder, Ocr)
│   │   ├── Models/                Request/response records
│   │   ├── Services/              OllamaClient, OllamaLoadBalancer, QdrantVectorStore, ChunkingService, RagService,
│   │   │                          KnowledgeFolderIngestService, JsonFileIngestStateStore
│   │   ├── Services/Extraction/   Per-format text extractors + factory (docx/pptx/xlsx/pdf/txt/md/image-OCR)
│   │   ├── Middleware/            ApiKeyAuthMiddleware
│   │   └── Dockerfile
│   └── KbAgent.Api.Tests/        xUnit unit tests (mocked Ollama/Qdrant dependencies; extractors verified with
│                                  real generated files)
├── knowledge-base/               Default local folder scanned by KnowledgeFolderIngestService (demo content only)
├── docker-compose.yml            API + Qdrant, one-command standup
└── KbAgent.sln
```

## Key Modules

| Module | Responsibility |
|--------|---------------|
| `OllamaClient` | REST calls to a single Ollama backend (`/api/tags` health, `/api/embeddings`, `/api/generate`) |
| `OllamaLoadBalancer` | Round-robins across configured Ollama backends, skipping unhealthy ones |
| `QdrantVectorStore` | Collection management, chunk upsert, similarity search, delete-by-source against Qdrant |
| `ChunkingService` | Splits raw text into overlapping character chunks for embedding |
| `RagService` | Orchestrates both flows: Ask (embed → search → prompt → generate) and Ingest (delete old chunks →
  chunk → embed → upsert) |
| `ApiKeyAuthMiddleware` | Validates `Authorization: Basic` credentials on `/api/*` against `IApiUserStore` when at least one user exists |
| `ApiTokenHasher` / `BasicAuthCredentialParser` | Token generation/hashing and pure Basic-Auth header parsing (both unit-testable without HTTP plumbing) |
| `JsonFileApiUserStore` | Persists username → token-hash pairs to a JSON file; created via `dotnet run -- create-user <username>` |
| `Services/Extraction/*` | `IDocumentTextExtractor` per format (Word/PowerPoint/Excel via OOXML SDK & ClosedXML,
  PDF via PdfPig, images via the Tesseract CLI) + `DocumentTextExtractorFactory` dispatching by file extension |
| `KnowledgeFolderIngestService` | `BackgroundService`: timer-driven recursive scan of a configured folder,
  extracts + ingests new/changed files, retries failures on the next scan |
| `JsonFileIngestStateStore` / `KnowledgeFolderChangeDetector` | Persists and diffs per-file fingerprints
  (size + last-write-time) so unchanged files are skipped between scans |

## Data Flow

**Ask (`POST /api/ask`):**
```
Client → /api/ask → RagService.AskAsync
  → OllamaLoadBalancer picks a healthy backend
  → OllamaClient.EmbedAsync(question)      [chosen backend]
  → QdrantVectorStore.SearchAsync(embedding, topK)
  → build grounded prompt from retrieved chunks
  → OllamaClient.GenerateAsync(prompt)      [same backend]
  → AskResponse { answer, sources }
```

**Ingest (`POST /api/ingest`):**
```
Client → /api/ingest → RagService.IngestAsync
  → QdrantVectorStore.EnsureCollectionAsync
  → ChunkingService.Chunk(text)
  → for each chunk: OllamaClient.EmbedAsync(chunk)
  → QdrantVectorStore.UpsertChunksAsync(chunks)
  → IngestResponse { source, chunkCount }
```

Both flows use the same load-balanced backend for the duration of a single request (picked once per request, not
per call) so a request's embedding and generation calls land on the same laptop.

**Folder scan (background, replaces roadmap Step 5's cron):**
```
Timer tick → KnowledgeFolderIngestService.ScanOnceAsync
  → enumerate files under KnowledgeFolder:Path, compute fingerprint (size + last-write-time) per file
  → KnowledgeFolderChangeDetector.GetChangedOrNewFiles(current, previous fingerprints)
  → for each changed/new file: DocumentTextExtractorFactory picks an extractor by extension
  → extractor.ExtractTextAsync(fileStream) → RagService.IngestAsync(relativePath, text)
  → only successfully-ingested files' fingerprints are persisted (JsonFileIngestStateStore) — a failed file
    (e.g. Qdrant/Ollama temporarily down) is retried on the next scan, not silently marked done
```

## Deployment (per roadmap Phase 1)

`docker-compose.yml` runs the API and Qdrant as containers, and mounts a host folder (`KB_FOLDER_PATH`, default
`./knowledge-base`) read-only into the API container for `KnowledgeFolderIngestService` to scan — this can be a
plain folder or a OneDrive/Google Drive desktop-sync mount, both are just local paths from the container's
perspective. A named volume (`api-users-data`) persists `api-users.json` across container rebuilds. The Dockerfile
installs the Tesseract OCR engine (`apt-get install tesseract-ocr`) for image text extraction; running outside
Docker requires Tesseract installed separately on the host, and OCR is skipped gracefully (logged, not a crash) if
it isn't found. A reverse proxy (Nginx/Traefik) and Cloudflare Tunnel sit in front for external exposure — those
are infra/ops concerns outside this repo, but the API supports them via `ApiKeyAuthMiddleware` (per-user Basic
Auth) and by not hard-redirecting to HTTPS outside Development (TLS is terminated by the reverse proxy).
