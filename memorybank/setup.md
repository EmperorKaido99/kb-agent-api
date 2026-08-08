# Setup

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://docs.docker.com/get-docker/) + Docker Compose (for containerized run)
- [Ollama](https://ollama.com) running on each machine acting as a backend, with the generation and embedding
  models pulled, e.g.:
  ```bash
  ollama pull qwen3:1.7b
  ollama pull nomic-embed-text
  ```
- [Tesseract OCR](https://github.com/tesseract-ocr/tesseract) — only needed for extracting text from standalone
  image files in the knowledge folder. Already installed in the Docker image; if running `dotnet run` directly on
  the host, install it separately (`apt-get install tesseract-ocr tesseract-ocr-eng` on Debian/Ubuntu, or the
  Windows installer from the Tesseract project). Missing Tesseract doesn't crash the app — image OCR is just
  skipped with a logged warning.

## Environment Variables

Configuration uses the standard ASP.NET Core `Section__Key` convention (env vars override `appsettings.json`).

| Variable | Purpose | Required | Default |
|----------|---------|----------|---------|
| `Ollama__BackendBaseUrls__0`, `__1`, ... | Base URL of each Ollama backend (one per laptop) | Yes | `http://laptop1.local:11434`, `http://laptop2.local:11434` (placeholders) |
| `Ollama__GenerationModel` | Model used for answer generation | No | `qwen3:1.7b` |
| `Ollama__EmbeddingModel` | Model used for embeddings | No | `nomic-embed-text` |
| `Ollama__RequestTimeoutSeconds` | HTTP timeout per Ollama call | No | `60` |
| `Qdrant__Host` | Qdrant hostname | No | `localhost` |
| `Qdrant__Port` | Qdrant gRPC port | No | `6334` |
| `Qdrant__CollectionName` | Qdrant collection name | No | `kb-chunks` |
| `Qdrant__VectorSize` | Embedding dimensionality (must match the embedding model) | No | `768` |
| `Chunking__ChunkSizeChars` | Characters per chunk | No | `1000` |
| `Chunking__ChunkOverlapChars` | Overlap between chunks | No | `200` |
| `Rag__TopK` | Number of chunks retrieved per question | No | `4` |
| `ApiUsers__FilePath` | Where username→token-hash credentials are stored. No users in the file = auth disabled (local dev only) | No | `api-users.json` |
| `KnowledgeFolder__Path` | Local folder scanned for documents (plain folder, or a OneDrive/Google Drive desktop-sync mount — both are just paths). Empty disables folder scanning | No | `""` (disabled) |
| `KnowledgeFolder__ScanIntervalMinutes` | How often the folder is rescanned for new/changed files | No | `15` |
| `KnowledgeFolder__StateFilePath` | Where per-file fingerprints are persisted between scans | No | `knowledge-folder-state.json` |
| `Ocr__TesseractExecutable` | Name/path of the Tesseract CLI binary | No | `tesseract` |
| `Ocr__Language` | Tesseract language code | No | `eng` |
| `Ocr__TimeoutSeconds` | Max time per image OCR call before it's killed | No | `60` |

## Getting Started

```bash
# Restore, build, and run the API locally (uses appsettings.Development.json — single localhost Ollama backend, no API key)
cd src/KbAgent.Api
dotnet run

# Or run the whole stack (API + Qdrant) with Docker Compose from the repo root
docker compose up --build
```

The API listens on `http://localhost:8080` (or the `dotnet run` dev port). Point `Ollama__BackendBaseUrls` at your
two laptops' Ollama instances (default Ollama port is `11434`) before using it for real.

### Creating API users

`/api/*` requires HTTP Basic Auth (`Authorization: Basic base64(username:token)`) once at least one user exists.
With no users created, auth is disabled — fine for local dev, not for anything exposed to the internet.

```bash
# Local (dotnet run):
cd src/KbAgent.Api
dotnet run -- create-user alice

# Docker Compose (writes into the api-users-data volume the running container also reads):
docker compose run --rm api dotnet KbAgent.Api.dll create-user alice
```

This prints the plaintext token exactly once — copy it immediately, only its hash is stored. Use it as:

```bash
curl -u alice:<token> -H "Content-Type: application/json" \
  -d '{"question":"..."}' http://localhost:8080/api/ask
```

Run the command again with a different username to add more users; there's no delete/revoke command yet —
remove an entry from `api-users.json` (or the `api-users-data` volume) directly to revoke it.

### Populating the knowledge base from a folder

Set `KB_FOLDER_PATH` before `docker compose up` to point at your real documents — including a folder synced by
the OneDrive or Google Drive desktop app, since that's just a local path once synced:

```bash
KB_FOLDER_PATH="/home/you/OneDrive/Knowledge Base" docker compose up --build
```

Drop `.docx`, `.pptx`, `.xlsx`, `.pdf`, `.txt`, `.md`, or image files in that folder — `KnowledgeFolderIngestService`
picks up new/changed files automatically on its scan interval (`KnowledgeFolder__ScanIntervalMinutes`, default 15
minutes). Without `KB_FOLDER_PATH` set, Docker Compose defaults to the repo's `./knowledge-base` folder.

### Try it

Add `-u <username>:<token>` to these if you've created an API user (see above) — otherwise auth is disabled.

```bash
# Ingest a document
curl -X POST http://localhost:8080/api/ingest \
  -H "Content-Type: application/json" \
  -d '{"source": "policy.md", "text": "Refunds are available within 30 days of purchase."}'

# Ask a question
curl -X POST http://localhost:8080/api/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "What is the refund policy?"}'

# Health check
curl http://localhost:8080/health
```

## Running Tests

```bash
dotnet test
```
