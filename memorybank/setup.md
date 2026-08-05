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
| `ApiKey__Key` | Required `X-Api-Key` header value for `/api/*`. Empty disables auth (local dev only) | No | `""` |

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

### Try it

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
