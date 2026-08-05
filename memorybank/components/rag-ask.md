# RAG Ask

## Overview
Answers a user's question using the internal knowledge base rather than a model's general training — the core
proof-of-concept flow from the Phase 1 roadmap.

## Business Rules
- Answers must be grounded in retrieved knowledge base chunks when any are found; the prompt instructs the model
  to say it doesn't know rather than guess when the context doesn't contain the answer.
- If no chunks are found (e.g. empty/unpopulated knowledge base), the model is told explicitly and falls back to
  general knowledge, so the caller can distinguish a grounded answer from an ungrounded one via the `sources` list.
- A single request uses one Ollama backend for both the question embedding and the answer generation (picked once
  per request), so the two calls stay consistent with each other.
- `/api/*` requires a matching `X-Api-Key` header whenever `ApiKey__Key` is configured.

## Data Flow

```
Client → POST /api/ask → RagService.AskAsync → OllamaLoadBalancer (pick healthy backend)
                                              → OllamaClient.EmbedAsync(question)
                                              → QdrantVectorStore.SearchAsync(embedding, topK)
                                              → OllamaClient.GenerateAsync(grounded prompt)
                                              → AskResponse { answer, sources }
```

### Step-by-step:
1. Client sends `POST /api/ask` with `{ "question": "..." }`.
2. `ApiKeyAuthMiddleware` validates `X-Api-Key` (if a key is configured).
3. `RagService.AskAsync` asks `OllamaLoadBalancer` for a healthy backend (round-robin, skips unhealthy ones).
4. The question is embedded via that backend's `/api/embeddings`.
5. `QdrantVectorStore.SearchAsync` retrieves the top-K most similar chunks (`Rag__TopK`, default 4).
6. A prompt is built embedding the retrieved chunks as numbered, sourced context.
7. The same backend's `/api/generate` produces the answer.
8. Response returns the answer plus the source snippets used (source name, text, similarity score).

## Validation Rules

| Field | Rule | Where Enforced | Error Message |
|-------|------|----------------|---------------|
| question | Must not be empty/whitespace | API (`/api/ask` handler) | "Question must not be empty." (400) |
| X-Api-Key | Must match configured `ApiKey__Key` when one is set | `ApiKeyAuthMiddleware` | "Missing or invalid API key." (401) |

## Key Files

| File | Role |
|------|------|
| `src/KbAgent.Api/Program.cs` | Maps `POST /api/ask`, wires DI |
| `src/KbAgent.Api/Services/RagService.cs` | Orchestrates the ask flow |
| `src/KbAgent.Api/Services/OllamaLoadBalancer.cs` | Picks a healthy Ollama backend |
| `src/KbAgent.Api/Services/OllamaClient.cs` | Embed/generate calls to Ollama |
| `src/KbAgent.Api/Services/QdrantVectorStore.cs` | Similarity search |
| `src/KbAgent.Api/Middleware/ApiKeyAuthMiddleware.cs` | API-key auth |

## Edge Cases & Gotchas
- No backends configured, or all fail their health check → `RagService.AskAsync` throws `InvalidOperationException`
  (surfaces as a 500 today; no explicit mapping to a friendlier status code yet).
- Empty knowledge base → `sources` comes back empty and the answer is explicitly caveated as ungrounded.

## Related Components
- [kb-ingest.md](./kb-ingest.md) — populates the knowledge base this flow searches
