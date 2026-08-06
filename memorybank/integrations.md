# Integrations

## External APIs

| Service | Purpose | Auth Method | Notes |
|---------|---------|-------------|-------|
| Ollama (local, per backend) | LLM inference (`/api/generate`) and embeddings (`/api/embeddings`); health via `/api/tags` | None (local network trust, per roadmap) | One instance per laptop, default port `11434`. Base URLs configured via `Ollama__BackendBaseUrls` |
| Qdrant | Vector storage and similarity search for knowledge base chunks | None by default (gRPC, local/private network) | Runs as a Docker container; connection via `Qdrant__Host`/`Qdrant__Port` |

## Local Tools (not network services)

| Tool | Purpose | Notes |
|------|---------|-------|
| Tesseract OCR | Extracts text from standalone image files in the knowledge folder | Invoked as a CLI subprocess (`ImageOcrTextExtractor`), not a library binding — avoids native-binding platform mismatches. Installed via apt in the Docker image; must be installed separately if running outside Docker. Missing binary degrades gracefully (logged warning, OCR skipped for that file) rather than crashing. |
