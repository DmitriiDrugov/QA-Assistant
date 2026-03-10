# Backend Overview

## What This Backend Does

`QA.Backend` exposes the original console prototype as HTTP APIs so a frontend can call it.

Main flow:
1. Receive a question from `/api/questions/ask`
2. Load and chunk `knowledge_base.txt`
3. Find the best chunk with simple keyword matching
4. Call the AI provider with context + question
5. Return answer + metadata in JSON

## Request Flow

1. `QuestionsController.Ask` validates input
2. `QaService` coordinates all steps
3. `KnowledgeBaseService` provides chunks
4. `SearchService` selects the best chunk
5. `OpenAiService` calls external AI
6. API returns `AskQuestionResponse`

## Main Services

- `KnowledgeBaseService`
  - Reads the file path from config
  - Validates file existence/readability
  - Splits text into chunks
  - Supports status and reload endpoints

- `SearchService`
  - Keyword-based scoring, close to console behavior
  - Returns most relevant chunk

- `OpenAiService`
  - Isolates provider HTTP logic
  - Builds chat completion payload
  - Parses AI response content

- `QaService`
  - Orchestrates the full pipeline
  - Applies question-length validation
  - Returns final response object

## Configuration Locations

- `appsettings.json` for defaults
- `appsettings.Development.json` for local overrides
- Environment variables for secrets (`AI__APIKEY`)
- Typed options classes in `Options/`

## Why This Is Frontend-Ready

- Stable JSON endpoint contracts
- Predictable status codes
- CORS policy configurable by origin list
- Swagger enabled in development
