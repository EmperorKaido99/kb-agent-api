# KB Ingest

## Overview
Loads a source document into the knowledge base so [RAG Ask](./rag-ask.md) can retrieve it — chunk, embed, store.

## Business Rules
- Text is split into fixed-size, overlapping character chunks (`Chunking__ChunkSizeChars` / `ChunkOverlapChars`,
  default 1000/200) so retrieval can match smaller, more relevant passages instead of whole documents.
- Every chunk is embedded individually and stored with its source name and text as payload, so `RAG Ask` can cite
  which document an answer came from.
- The Qdrant collection is created on first use if it doesn't already exist (`EnsureCollectionAsync`).
- If chunking produces zero chunks (e.g. blank text), nothing is embedded or upserted — `chunkCount: 0` is
  returned rather than erroring.
- `/api/*` requires a matching `X-Api-Key` header whenever `ApiKey__Key` is configured.

## Data Flow

```
Client → POST /api/ingest → RagService.IngestAsync → QdrantVectorStore.EnsureCollectionAsync
                                                     → ChunkingService.Chunk(text)
                                                     → OllamaClient.EmbedAsync(chunk) [per chunk]
                                                     → QdrantVectorStore.UpsertChunksAsync
                                                     → IngestResponse { source, chunkCount }
```

### Step-by-step:
1. Client sends `POST /api/ingest` with `{ "source": "...", "text": "..." }`.
2. `ApiKeyAuthMiddleware` validates `X-Api-Key` (if a key is configured).
3. `RagService.IngestAsync` ensures the Qdrant collection exists.
4. `OllamaLoadBalancer` picks one healthy backend for the whole ingest request.
5. `ChunkingService.Chunk` splits `text` into overlapping chunks.
6. Each chunk is embedded via that backend's `/api/embeddings`.
7. All chunks (with embeddings + source/text payload) are upserted into Qdrant in one call.
8. Response returns the source name and how many chunks were stored.

## Validation Rules

| Field | Rule | Where Enforced | Error Message |
|-------|------|----------------|---------------|
| source | Must not be empty/whitespace | API (`/api/ingest` handler) | "Source and text must not be empty." (400) |
| text | Must not be empty/whitespace | API (`/api/ingest` handler) | "Source and text must not be empty." (400) |
| X-Api-Key | Must match configured `ApiKey__Key` when one is set | `ApiKeyAuthMiddleware` | "Missing or invalid API key." (401) |

## Key Files

| File | Role |
|------|------|
| `src/KbAgent.Api/Program.cs` | Maps `POST /api/ingest` |
| `src/KbAgent.Api/Services/RagService.cs` | Orchestrates the ingest flow |
| `src/KbAgent.Api/Services/ChunkingService.cs` | Splits text into overlapping chunks |
| `src/KbAgent.Api/Services/QdrantVectorStore.cs` | Collection creation + upsert |

## Edge Cases & Gotchas
- Re-ingesting the same source with the same text produces new chunk IDs (random GUIDs) each time — there's no
  dedup/upsert-by-source-name yet, so repeated ingestion of the same document duplicates chunks in Qdrant.
- Scheduled re-indexing (roadmap Step 5, cron) is not implemented — ingestion is currently on-demand only via this
  endpoint.

## Related Components
- [rag-ask.md](./rag-ask.md) — consumes what this flow stores
