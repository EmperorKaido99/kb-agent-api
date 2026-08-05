# Features

## Active Features

| Feature | Description | Status |
|---------|-------------|--------|
| Grounded question answering (`POST /api/ask`) | Answers a question using RAG over the internal knowledge base, grounded via Qdrant retrieval + Ollama generation | ✅ Implemented |
| Knowledge base ingestion (`POST /api/ingest`) | Chunks, embeds, and stores a source document into the vector store | ✅ Implemented |
| Ollama load balancing | Routes requests round-robin across configured Ollama backends (the two laptops), skipping unhealthy ones | ✅ Implemented |
| API key authentication | Protects `/api/*` with an `X-Api-Key` header when a key is configured | ✅ Implemented |
| Health check (`GET /health`) | Reports API status and per-backend Ollama health | ✅ Implemented |
| Dockerized deployment | `docker-compose.yml` stands up the API + Qdrant | ✅ Implemented |
| Scheduled re-indexing (cron) | Roadmap Step 5 — not implemented; ingestion is currently on-demand via `/api/ingest` | ⏳ Not started |
| Reverse proxy + Cloudflare Tunnel exposure | Roadmap Step 6 — infra/ops setup outside this repo | ⏳ Not started |
| Phase 2: distributed inference across laptops (llama.cpp rpc-server) | Explicitly out of scope for Phase 1 per the roadmap | ⏳ Future |
