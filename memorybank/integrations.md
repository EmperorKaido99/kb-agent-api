# Integrations

## External APIs

| Service | Purpose | Auth Method | Notes |
|---------|---------|-------------|-------|
| Ollama (local, per backend) | LLM inference (`/api/generate`) and embeddings (`/api/embeddings`); health via `/api/tags` | None (local network trust, per roadmap) | One instance per laptop, default port `11434`. Base URLs configured via `Ollama__BackendBaseUrls` |
| Qdrant | Vector storage and similarity search for knowledge base chunks | None by default (gRPC, local/private network) | Runs as a Docker container; connection via `Qdrant__Host`/`Qdrant__Port` |
