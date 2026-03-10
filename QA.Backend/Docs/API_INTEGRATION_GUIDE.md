# API Integration Guide

## Base URL

Local development base URL is shown by `dotnet run` (for example `https://localhost:7172`).

## Endpoints

### 1) GET /api/health

Purpose: health check.

Example response:

```json
{
  "success": true,
  "status": "Healthy",
  "service": "QA.Backend",
  "environment": "Development",
  "timestampUtc": "2026-03-08T22:30:00Z"
}
```

### 2) POST /api/questions/ask

Purpose: run full Q&A pipeline.

Request:

```json
{
  "question": "What is the refund policy?"
}
```

Success response (`200 OK`):

```json
{
  "success": true,
  "question": "What is the refund policy?",
  "answer": "The refund policy is ...",
  "matchedChunk": "Relevant knowledge base excerpt...",
  "source": "knowledge_base"
}
```

Error response (`400 Bad Request`):

```json
{
  "success": false,
  "message": "Question is required.",
  "details": null
}
```

Possible statuses:
- `200` successful answer
- `400` invalid input (empty/whitespace/too long)
- `500` internal backend issue (knowledge base/search/config)
- `502` external AI provider failure

### 3) GET /api/knowledge/status

Purpose: check knowledge base load state.

Response (`200 OK`):

```json
{
  "success": true,
  "isLoaded": true,
  "chunkCount": 3,
  "configuredFilePath": "knowledge_base.txt",
  "resolvedFilePath": "D:\\Projects\\backend\\Q-A-project\\knowledge_base.txt",
  "lastLoadedUtc": "2026-03-08T22:30:00Z"
}
```

### 4) POST /api/knowledge/reload

Purpose: force knowledge base reload from disk.

Success response: same shape as `/api/knowledge/status`.

## Testing

### Swagger

Run backend and open `/swagger` in development.

### curl examples

```bash
curl -X GET "https://localhost:7172/api/health"
```

```bash
curl -X POST "https://localhost:7172/api/questions/ask" \
  -H "Content-Type: application/json" \
  -d '{"question":"How do I reset my password?"}'
```

## AI Configuration

Set API key via environment variable:

```powershell
$env:AI__APIKEY = "your-real-api-key"
```

Optional overrides:
- `AI__BASEURL`
- `AI__CHATCOMPLETIONSPATH`
- `AI__MODEL`
- `AI__SYSTEMPROMPT`
