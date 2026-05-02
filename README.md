<div align="center">

# QA Assistant

**A REST API backend that turns a plain-text knowledge base into an AI-powered Q&A service**

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-4479A1?style=for-the-badge&logo=mysql&logoColor=white)](https://www.mysql.com/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ED?style=for-the-badge&logo=docker&logoColor=white)](https://www.docker.com/)
[![Render](https://img.shields.io/badge/Deploy-Render-46E3B7?style=for-the-badge&logo=render&logoColor=white)](https://render.com/)

A student project built by a team of 5 · ASP.NET Core · JWT Auth · OpenRouter LLM

</div>

---

## What it does

You send a question. The API searches a knowledge base file for the most relevant chunk of text, passes it to an LLM, and returns a plain-text answer. No markdown, no fluff — just the answer.

Originally a console prototype, now a deployable HTTP API with auth, conversation history, and per-user AI configuration.

```
User question
     │
     ▼
┌─────────────┐     ┌──────────────────┐     ┌───────────────┐
│  Validation │────▶│  Keyword search  │────▶│  LLM response │
│  (≤500 ch.) │     │  over KB chunks  │     │  via OpenRouter│
└─────────────┘     └──────────────────┘     └───────────────┘
                                                      │
                                                      ▼
                                             Plain-text answer
                                           + matched source chunk
```

---

## Table of Contents

- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Features](#features)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [API Reference](#api-reference)
- [Deployment](#deployment)
- [Team](#team)

---

## Tech Stack

| | |
|---|---|
| **Language / Framework** | C# · .NET 8 · ASP.NET Core |
| **Database** | MySQL 8.0 via Entity Framework Core 8 + Pomelo |
| **Authentication** | JWT Bearer (HS256) |
| **AI Provider** | OpenRouter API (configurable model, free tier works) |
| **API Docs** | Swagger / Swashbuckle |
| **Containerisation** | Docker (multi-stage, SDK → Runtime) |
| **Hosting** | Render.com |
| **CI** | GitHub Actions |

---

## Project Structure

```
QA-Assistant/
├── QA.Backend/
│   ├── Controllers/
│   │   ├── QuestionsController.cs      # POST /api/questions/ask
│   │   ├── AuthController.cs           # Register, login, /me
│   │   ├── ConversationsController.cs  # Chat history
│   │   ├── AiSettingsController.cs     # Per-user model config
│   │   ├── KnowledgeController.cs      # KB status & reload
│   │   └── HealthController.cs
│   ├── Services/
│   │   ├── QaService.cs                # Orchestrates the full Q&A flow
│   │   ├── KnowledgeBaseService.cs     # Loads & chunks the KB file
│   │   ├── SearchService.cs            # Keyword scoring over chunks
│   │   ├── OpenAiService.cs            # OpenRouter HTTP client
│   │   └── AuraModelService.cs         # Alternative model integration
│   ├── Data/                           # EF Core context + entities
│   ├── Models/                         # Request / response DTOs
│   ├── Options/                        # Typed config classes
│   ├── Migrations/
│   └── Program.cs
├── knowledge_base.txt                  # The knowledge base (plain text)
├── Dockerfile
└── render.yaml
```

---

## Features

**Q&A pipeline**
- Keyword-based relevance scoring over chunked knowledge base (chunk size configurable, default 800 chars)
- Headers in KB get a scoring bonus — good for structured documents
- LLM called via OpenRouter; system prompt enforces plain-text output
- Returns the answer, the matched chunk, and the source label

**Knowledge base management**
- Loaded from a file at startup, kept in memory as a singleton
- `POST /api/knowledge/reload` hot-reloads without restarting the server
- Thread-safe via `SemaphoreSlim`

**Authentication**
- Email + password registration, hashed with ASP.NET Core's `PasswordHasher`
- JWT access tokens, 24-hour expiry by default
- Standard `[Authorize]` on protected routes

**Conversation history**
- Conversations and messages stored in MySQL
- Full history retrievable per conversation ID

**Per-user AI settings**
- Each user can set a custom model endpoint and system prompt
- Stored in `AiModelSettingsEntity`, 1:1 with users

---

## Getting Started

**Requirements**
- .NET SDK 8.0+
- MySQL 8.0 (local or remote)
- [OpenRouter](https://openrouter.ai/) API key (free tier is enough)

```bash
# Clone
git clone https://github.com/dmitriidrugov/qa-assistant.git
cd qa-assistant

# Restore
dotnet restore

# Set required env vars
export ASPNETCORE_ENVIRONMENT=Development
export AI__APIKEY="sk-or-xxxxxxxx"
export DATABASE__CONNECTIONSTRING="Server=localhost;Database=qa_db;User=root;Password=secret;"

# Apply migrations
dotnet ef database update --project QA.Backend

# Run
dotnet run --project QA.Backend
```

Once running:
- API base: `https://localhost:7172`
- Swagger: `https://localhost:7172/swagger`
- Health: `https://localhost:7172/api/health`

**Docker**

```bash
docker build -t qa-assistant .

docker run -p 10000:10000 \
  -e AI__APIKEY="sk-or-xxxxxxxx" \
  -e DATABASE__CONNECTIONSTRING="Server=host.docker.internal;Database=qa_db;User=root;Password=secret;" \
  qa-assistant
```

---

## Configuration

Secrets are passed as environment variables — they override anything in `appsettings.json`.

| Environment variable | Description | Required |
|---|---|:---:|
| `AI__APIKEY` | OpenRouter API key | ✅ |
| `DATABASE__CONNECTIONSTRING` | MySQL connection string | ✅ |
| `ASPNETCORE_ENVIRONMENT` | `Development` or `Production` | — |
| `JWT__KEY` | JWT signing secret (change in production) | — |

<details>
<summary>appsettings.json defaults</summary>

```json
{
  "KnowledgeBase": {
    "FilePath": "knowledge_base.txt",
    "ChunkSize": 800,
    "MaxQuestionLength": 500
  },
  "Ai": {
    "Provider": "OpenRouter",
    "BaseUrl": "https://openrouter.ai/api",
    "Model": "openai/gpt-oss-20b:free",
    "ApiKey": "",
    "TimeoutSeconds": 60
  },
  "Database": {
    "ConnectionString": ""
  },
  "Jwt": {
    "Issuer": "QA.Backend",
    "Audience": "AURA.Frontend",
    "AccessTokenExpiresHours": 24
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "http://localhost:3000"]
  }
}
```

</details>

---

## API Reference

| Method | Path | Description | Auth |
|---|---|---|:---:|
| `POST` | `/api/questions/ask` | Ask a question | — |
| `POST` | `/auth/register` | Create an account | — |
| `POST` | `/auth/login` | Get a JWT token | — |
| `GET` | `/auth/me` | Current user info | 🔒 |
| `GET` | `/api/knowledge/status` | KB load state & chunk count | — |
| `POST` | `/api/knowledge/reload` | Reload KB from disk | — |
| `GET` | `/api/conversations/{id}` | Fetch conversation history | 🔒 |
| `POST` | `/api/conversations/{id}/messages` | Add a message | 🔒 |
| `GET` / `PUT` | `/api/ai-settings` | User AI model config | 🔒 |
| `GET` | `/api/health` | Health check | — |

**Example**

```bash
curl -X POST https://your-app.onrender.com/api/questions/ask \
  -H "Content-Type: application/json" \
  -d '{"question": "How do I reset my password?"}'
```

```json
{
  "success": true,
  "question": "How do I reset my password?",
  "answer": "Go to hr.company.com, open Security settings, and click Reset Password. A link will be sent to your registered email.",
  "matchedChunk": "...",
  "source": "knowledge_base"
}
```

Full interactive docs at `/swagger`.

---

## Deployment

Configured for [Render.com](https://render.com/) out of the box (`render.yaml`).

1. Fork the repo
2. Create a new Web Service on Render, point it at the repo
3. Set `AI__APIKEY` and `DATABASE__CONNECTIONSTRING` in the Render environment
4. Render builds the Docker image and deploys automatically

Health check path: `/api/health` · Port: `10000`

---

## Team

Built as a university group project by 5 people.

| Name | Role |
|---|---|
| **Oussama Azzouz** | Team Lead |
| **Dmitrii Drugov** | Backend |
| **Mustafa Celik** | AI Integration |
| **Merinisa Mederova** | Database |
| **Karam Ebnelwalid** | Database |
| **Metry Peter Atef** | Frontend |

---

<div align="center">
  <sub>University project · 2026</sub>
</div>
