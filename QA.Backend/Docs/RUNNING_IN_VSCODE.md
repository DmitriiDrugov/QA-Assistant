# Running in VS Code

## Prerequisites

- .NET SDK 8.0+
- VS Code with C# extension

## Commands

Run from repository root (`d:\Projects\backend\Q-A-project`):

```bash
dotnet restore
dotnet build

dotnet run --project QA.Backend
```

## Configure Environment Variables (PowerShell)

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:AI__APIKEY = "your-real-api-key"
```

Optional:

```powershell
$env:KNOWLEDGEBASE__FILEPATH = "knowledge_base.txt"
$env:AI__MODEL = "gpt-4o-mini"
```

## Local Testing

1. Start backend with `dotnet run --project QA.Backend`
2. Open Swagger from the startup URL + `/swagger`
3. Test:
   - `GET /api/health`
   - `POST /api/questions/ask`
   - `GET /api/knowledge/status`
   - `POST /api/knowledge/reload`

## Postman Notes

- Set `Content-Type: application/json` for POST requests
- For ask endpoint, send `{ "question": "..." }`
- Expect `400/500/502` with JSON error body for failures
