# KB Ingest

## Overview
Loads a source document into the knowledge base so [RAG Ask](./rag-ask.md) can retrieve it — chunk, embed, store.

## Business Rules
- Text is split into fixed-size, overlapping character chunks (`Chunking__ChunkSizeChars` / `ChunkOverlapChars`,
  default 1000/200) so retrieval can match smaller, more relevant passages instead of whole documents.
- Every chunk is embedded individually and stored with its source name and text as payload, so `RAG Ask` can cite
  which document an answer came from.
- The Qdrant collection is created on first use if it doesn't already exist (`EnsureCollectionAsync`).
- Re-ingesting a source (same source name) **deletes its previously stored chunks first**, so re-ingestion is
  idempotent — no duplicate chunks in Qdrant, whether triggered manually or by the
  [knowledge folder watcher](./knowledge-folder-watcher.md).
- If chunking produces zero chunks (e.g. blank text), nothing is embedded or upserted — `chunkCount: 0` is
  returned rather than erroring.
- `/api/*` requires HTTP Basic Auth (username + token) whenever at least one user exists in `ApiUsers__FilePath`.

## Data Flow

```
Client → POST /api/ingest → RagService.IngestAsync → QdrantVectorStore.EnsureCollectionAsync
                                                     → QdrantVectorStore.DeleteBySourceAsync(source)
                                                     → ChunkingService.Chunk(text)
                                                     → OllamaClient.EmbedAsync(chunk) [per chunk]
                                                     → QdrantVectorStore.UpsertChunksAsync
                                                     → IngestResponse { source, chunkCount }
```

### Step-by-step:
1. Client sends `POST /api/ingest` with `{ "source": "...", "text": "..." }` (or the
   [folder watcher](./knowledge-folder-watcher.md) calls the same method internally after extracting text from a
   PowerPoint/Word/Excel/PDF/image file).
2. `ApiKeyAuthMiddleware` validates `Authorization: Basic` credentials against the user store (if any users exist).
3. `RagService.IngestAsync` ensures the Qdrant collection exists, then deletes any chunks already stored for this
   `source` (via a Qdrant payload-filter delete).
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
| Authorization | Must be `Basic base64(username:token)` matching a stored user, when any users exist | `ApiKeyAuthMiddleware` | "Missing or invalid credentials." (401) |

## Key Files

| File | Role |
|------|------|
| `src/KbAgent.Api/Program.cs` | Maps `POST /api/ingest` |
| `src/KbAgent.Api/Services/RagService.cs` | Orchestrates the ingest flow |
| `src/KbAgent.Api/Services/ChunkingService.cs` | Splits text into overlapping chunks |
| `src/KbAgent.Api/Services/QdrantVectorStore.cs` | Collection creation, delete-by-source, upsert |

## Edge Cases & Gotchas
- Chunk IDs are still random GUIDs, but the delete-by-source step before upsert means this doesn't matter for
  duplication — old points for that source are gone before the new ones are written.
- If a file shrinks between ingests (fewer chunks than before), this is still correct: delete-by-source removes
  *all* old chunks for that source before the (smaller) new set is upserted, so there are no orphaned old chunks.

## Related Components
- [rag-ask.md](./rag-ask.md) — consumes what this flow stores
- [knowledge-folder-watcher.md](./knowledge-folder-watcher.md) — automatically drives this flow from a folder of
  PowerPoint/Word/Excel/PDF/image files on a schedule
