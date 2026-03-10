# Frontend Integration Guide

## Calling the Backend

Main endpoint for UI apps:
- `POST /api/questions/ask`

Request body:

```json
{
  "question": "What is the refund policy?"
}
```

Response body:

```json
{
  "success": true,
  "question": "What is the refund policy?",
  "answer": "...",
  "matchedChunk": "...",
  "source": "knowledge_base"
}
```

## fetch Example

```javascript
async function askQuestion(question) {
  const response = await fetch("https://localhost:7172/api/questions/ask", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ question })
  });

  const data = await response.json();

  if (!response.ok) {
    throw new Error(data.message || "Request failed");
  }

  return data;
}
```

## axios Example

```javascript
import axios from "axios";

export async function askQuestion(question) {
  const { data } = await axios.post("https://localhost:7172/api/questions/ask", {
    question
  });
  return data;
}
```

## UI State Handling

Recommended basic states:
- `loading`: disable submit button and show spinner
- `success`: render `answer` and optional `matchedChunk`
- `error`: render `message` from API error body

## CORS Notes

Allowed origins are configured in:
- `Cors:AllowedOrigins` in `appsettings*.json`

For local frontend dev, include your frontend URL (for example `http://localhost:5173` for Vite).

## Local Example

- Backend: `https://localhost:7172`
- Frontend (React/Vite): `http://localhost:5173`
- Add frontend origin to `Cors:AllowedOrigins`
