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
| Multi-format document ingestion | Extracts text from `.docx`, `.pptx`, `.xlsx`, `.pdf`, `.txt`/`.md`, and OCRs standalone images (`.png`/`.jpg`/`.jpeg`/`.bmp`/`.tiff`) via Tesseract | ✅ Implemented |
| Scheduled re-indexing (folder watcher) | `KnowledgeFolderIngestService` scans a configured local folder on a timer, auto-ingesting new/changed files — replaces roadmap Step 5's "cron" with an in-process equivalent. Failed files are retried on the next scan rather than marked done. | ✅ Implemented |
| Idempotent re-ingest | Re-ingesting a source (manually or via the folder scan) deletes its previous chunks before upserting new ones — no more duplicate chunks in Qdrant | ✅ Implemented |
| Reverse proxy + Cloudflare Tunnel exposure | Roadmap Step 6 — infra/ops setup outside this repo | ⏳ Not started |
| Audio transcription | Explicitly deferred — user's KB content is PowerPoint/Word/PDF/Excel; add only if a real need for audio ingestion is confirmed (heavier dependency: local Whisper model) | ⏳ Deferred |
| Scanned/image-only PDF OCR | PDF text-layer extraction only; pages with no text layer (scanned PDFs) are skipped rather than OCR'd — would need PDF page-to-image rendering, a bigger addition | ⏳ Not started |
| Phase 2: distributed inference across laptops (llama.cpp rpc-server) | Explicitly out of scope for Phase 1 per the roadmap | ⏳ Future |
