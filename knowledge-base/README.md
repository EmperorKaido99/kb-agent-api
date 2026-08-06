# Knowledge base folder (demo default)

Drop `.docx`, `.pptx`, `.xlsx`, `.pdf`, `.txt`, `.md`, or image (`.png`/`.jpg`/`.jpeg`/`.bmp`/`.tif`/`.tiff`)
files in here. `KnowledgeFolderIngestService` scans this folder on a timer (`KnowledgeFolder__ScanIntervalMinutes`,
default 15) and automatically ingests anything new or changed.

This is just the **default** path used when you run `docker compose up` without setting `KB_FOLDER_PATH`. To
point at your real documents instead — including a OneDrive/Google Drive folder synced to your laptop via their
desktop app — set the `KB_FOLDER_PATH` environment variable to that folder before running Docker Compose, e.g.:

```bash
KB_FOLDER_PATH="/home/you/OneDrive/Knowledge Base" docker compose up --build
```

Files actually committed to this repo under this folder (other than this README) are demo/test content only.
