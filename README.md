# TranscriptExtractor

Transcript-local extraction pipeline for law-enforcement interview and transcript processing.

The current solution accepts a transcript, creates a queued extraction job, stores a trusted JSON extraction document, and can render an infographic-style report through an HTML-backed PDF endpoint. The PDF renderer is currently a stub and will be replaced later with a real engine.

## Solution Layout

- `src/TranscriptExtractor.Api` - intake, status, extraction retrieval, and PDF endpoints
- `src/TranscriptExtractor.Worker` - background extraction worker and orchestration wiring
- `src/TranscriptExtractor.Core` - entities, DbContext, prompt loading, extraction orchestration, report composition, and HTML/PDF abstractions
- `tests/TranscriptExtractor.Tests` - unit and integration-style coverage
- `prompts/lvpd` - system prompt, user prompt template, and JSON schema text

## Configuration

Both app projects currently expose the same top-level configuration sections:

- `ConnectionStrings:TranscriptExtractor`
- `PromptAssets:Directory`
- `PromptAssets:Version`
- `Reports:TemplateVersion`
- `OpenAI:BaseUrl`
- `OpenAI:Model`
- `OpenAI:ApiKey`

Current note:

- The runtime wiring still uses EF Core InMemory for development scaffolding.
- The connection string is documented now so we can switch to PostgreSQL cleanly when we replace the temporary provider setup.
- `OpenAI:ApiKey` should be supplied from user secrets or environment variables, not committed settings.

## Prompt Assets

The extraction request is built from:

- `prompts/lvpd/system.txt`
- `prompts/lvpd/user.txt`
- `prompts/lvpd/schema.json.txt`

The worker loads those files through the prompt asset loader and injects transcript text plus schema text into the final request payload.

## Local Run

Build the solution:

```powershell
dotnet build TranscriptExtractor.slnx
```

Run the test suite:

```powershell
dotnet test tests\TranscriptExtractor.Tests\TranscriptExtractor.Tests.csproj
```

Run the API:

```powershell
dotnet run --project src\TranscriptExtractor.Api\TranscriptExtractor.Api.csproj
```

Run the worker:

```powershell
dotnet run --project src\TranscriptExtractor.Worker\TranscriptExtractor.Worker.csproj
```

## Current Status

Implemented so far:

- transcript intake endpoint
- transcript status endpoint
- extraction retrieval endpoint
- prompt asset loading
- extraction request building
- worker orchestration
- report composition
- HTML report rendering
- stub PDF endpoint

Still planned:

- durable database provider wiring
- real extraction client implementation
- real PDF rendering engine
