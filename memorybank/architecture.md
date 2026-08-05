# Architecture

## Project Structure

```
kb-agent-api/
├── src/
│   ├── KbAgent.Api/              ASP.NET Core 8 minimal-API project (the gateway API)
│   │   ├── Program.cs            DI wiring, middleware pipeline, endpoint mapping
│   │   ├── Configuration/        Strongly-typed options (Ollama, Qdrant, Chunking, Rag, ApiKey)
│   │   ├── Models/                Request/response records
│   │   ├── Services/              OllamaClient, OllamaLoadBalancer, QdrantVectorStore, ChunkingService, RagService
│   │   ├── Middleware/            ApiKeyAuthMiddleware
│   │   └── Dockerfile
│   └── KbAgent.Api.Tests/        xUnit unit tests (mocked Ollama/Qdrant dependencies)
├── docker-compose.yml            API + Qdrant, one-command standup
└── KbAgent.sln
```

## Key Modules

| Module | Responsibility |
|--------|---------------|
| `OllamaClient` | REST calls to a single Ollama backend (`/api/tags` health, `/api/embeddings`, `/api/generate`) |
| `OllamaLoadBalancer` | Round-robins across configured Ollama backends, skipping unhealthy ones |
| `QdrantVectorStore` | Collection management, chunk upsert, similarity search against Qdrant |
| `ChunkingService` | Splits raw text into overlapping character chunks for embedding |
| `RagService` | Orchestrates both flows: Ask (embed → search → prompt → generate) and Ingest (chunk → embed → upsert) |
| `ApiKeyAuthMiddleware` | Validates `X-Api-Key` on `/api/*` when a key is configured |

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

## Deployment (per roadmap Phase 1)

`docker-compose.yml` runs the API and Qdrant as containers. A reverse proxy (Nginx/Traefik) and Cloudflare Tunnel
sit in front for external exposure — those are infra/ops concerns outside this repo, but the API supports them via
`ApiKeyAuthMiddleware` (API-key auth) and by not hard-redirecting to HTTPS outside Development (TLS is terminated
by the reverse proxy).
