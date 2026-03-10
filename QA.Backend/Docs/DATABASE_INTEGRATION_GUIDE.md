# Database Integration Guide

## Current State

Database persistence is intentionally not implemented yet.

The backend is prepared for DB integration with:
- `DatabaseOptions` class (`Options/DatabaseOptions.cs`)
- `Database:ConnectionString` config key in `appsettings.json`

## Where to Add Connection String

Use one of these:
- `QA.Backend/appsettings.Development.json` (local development only)
- environment variable `DATABASE__CONNECTIONSTRING`
- secure production secret store

## Recommended Future Tables / Entities

When persistence is added, start with:
- `QuestionHistory` (question text, timestamp, user/session id)
- `AnswerHistory` (answer text, AI model, latency, status)
- `RequestLogs` (request id, endpoint, status code, error details)
- `Users` (if auth is added later)
- `Sessions` (conversation grouping)
- `KnowledgeBaseMetadata` (source file path, hash, last reload time)

## Why Current Structure Supports DB Later

- `QaService` already centralizes request orchestration
- Controllers are thin; persistence can be added in service layer
- Options/config pipeline already supports adding DB config
- Request/response contracts are separate from persistence models

## Suggested Next Steps for DB Integration

1. Add EF Core package and `AppDbContext`.
2. Add simple entities for `QuestionHistory` and `AnswerHistory` first.
3. Persist each `/api/questions/ask` request and response in `QaService`.
4. Add migrations and local SQL database setup.
5. Add read endpoints for question history if needed.

Keep this incremental to avoid overengineering.
