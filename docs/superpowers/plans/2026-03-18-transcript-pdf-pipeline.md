# Transcript PDF Pipeline Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a new `TranscriptExtractor` solution that ingests one transcript, stores a trusted LLM extraction JSON document, and renders an infographic-style PDF on demand.

**Architecture:** Use a small .NET solution with `TranscriptExtractor.Api`, `TranscriptExtractor.Worker`, `TranscriptExtractor.Core`, and `TranscriptExtractor.Tests`. The API handles intake and retrieval, the worker performs prompt-driven extraction and persists the JSON result, and a report composition layer turns saved extraction JSON into HTML/PDF output on demand. Model access stays behind a pluggable extraction client interface so different model implementations can be swapped without changing the worker pipeline.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core with PostgreSQL, xUnit, FluentAssertions, and HTML-first PDF rendering.

---

## Planned File Structure

- `TranscriptExtractor.slnx` - solution root
- `src/TranscriptExtractor.Api/` - HTTP endpoints and report delivery
- `src/TranscriptExtractor.Worker/` - background extraction worker
- `src/TranscriptExtractor.Core/` - EF Core entities, DbContext, services, pluggable extraction client abstractions, prompt assets, report composition
- `tests/TranscriptExtractor.Tests/` - unit and integration-oriented tests
- `prompts/lvpd/` - system prompt, user prompt template, and JSON schema text
- `docs/superpowers/specs/` - approved design spec
- `docs/superpowers/plans/` - implementation plans

### Task 1: Scaffold Solution Structure

**Files:**
- Create: `TranscriptExtractor.slnx`
- Create: `src/TranscriptExtractor.Api/TranscriptExtractor.Api.csproj`
- Create: `src/TranscriptExtractor.Worker/TranscriptExtractor.Worker.csproj`
- Create: `src/TranscriptExtractor.Core/TranscriptExtractor.Core.csproj`
- Create: `tests/TranscriptExtractor.Tests/TranscriptExtractor.Tests.csproj`
- Create: `Directory.Build.props`
- Create: `.gitignore`

- [ ] **Step 1: Scaffold the solution and projects**

Run:
`dotnet new sln -n TranscriptExtractor`
`dotnet new web -n TranscriptExtractor.Api -o src/TranscriptExtractor.Api`
`dotnet new worker -n TranscriptExtractor.Worker -o src/TranscriptExtractor.Worker`
`dotnet new classlib -n TranscriptExtractor.Core -o src/TranscriptExtractor.Core`
`dotnet new xunit -n TranscriptExtractor.Tests -o tests/TranscriptExtractor.Tests`

Expected: all project folders exist.

- [ ] **Step 2: Add project references and solution entries**

Run:
`dotnet sln add src/TranscriptExtractor.Api/TranscriptExtractor.Api.csproj`
`dotnet sln add src/TranscriptExtractor.Worker/TranscriptExtractor.Worker.csproj`
`dotnet sln add src/TranscriptExtractor.Core/TranscriptExtractor.Core.csproj`
`dotnet sln add tests/TranscriptExtractor.Tests/TranscriptExtractor.Tests.csproj`
`dotnet add src/TranscriptExtractor.Api/TranscriptExtractor.Api.csproj reference src/TranscriptExtractor.Core/TranscriptExtractor.Core.csproj`
`dotnet add src/TranscriptExtractor.Worker/TranscriptExtractor.Worker.csproj reference src/TranscriptExtractor.Core/TranscriptExtractor.Core.csproj`
`dotnet add tests/TranscriptExtractor.Tests/TranscriptExtractor.Tests.csproj reference src/TranscriptExtractor.Core/TranscriptExtractor.Core.csproj`
`dotnet add tests/TranscriptExtractor.Tests/TranscriptExtractor.Tests.csproj reference src/TranscriptExtractor.Api/TranscriptExtractor.Api.csproj`

Expected: references resolve cleanly.

- [ ] **Step 3: Add shared build settings and ignore rules**

Create `Directory.Build.props` with nullable and implicit usings enabled. Add `.gitignore` entries for `bin/`, `obj/`, `.superpowers/`, and local output directories.

- [ ] **Step 4: Verify the empty scaffold builds**

Run: `dotnet build TranscriptExtractor.slnx`
Expected: build succeeds.

- [ ] **Step 5: Commit**

Run:
`git add .`
`git commit -m "chore: scaffold transcript extractor solution"`

### Task 2: Add Core Domain and Persistence Foundations

**Files:**
- Create: `src/TranscriptExtractor.Core/Entities/Transcript.cs`
- Create: `src/TranscriptExtractor.Core/Entities/ExtractionJob.cs`
- Create: `src/TranscriptExtractor.Core/Entities/ExtractionDocument.cs`
- Create: `src/TranscriptExtractor.Core/TranscriptExtractorDbContext.cs`
- Create: `src/TranscriptExtractor.Core/Entities/ExtractionJobStatus.cs`
- Create: `src/TranscriptExtractor.Core/Validation/ExtractionDocumentValidator.cs`
- Create: `tests/TranscriptExtractor.Tests/Core/TranscriptEntityTests.cs`
- Create: `tests/TranscriptExtractor.Tests/Core/ExtractionJobTests.cs`
- Create: `tests/TranscriptExtractor.Tests/Core/ExtractionDocumentValidatorTests.cs`

- [ ] **Step 1: Write failing tests for core entity defaults, transitions, and extraction document referential validation**
- [ ] **Step 2: Run tests to verify they fail for the expected reason**
- [ ] **Step 3: Implement the minimal entities and DbContext**
- [ ] **Step 4: Run targeted tests, then the full test suite**
- [ ] **Step 5: Commit**

### Task 3: Add Prompt Asset Loading and Extraction Request Builder

**Files:**
- Create: `prompts/lvpd/system.txt`
- Create: `prompts/lvpd/user.txt`
- Create: `prompts/lvpd/schema.json.txt`
- Create: `src/TranscriptExtractor.Core/Prompts/PromptAssets.cs`
- Create: `src/TranscriptExtractor.Core/Prompts/FilePromptAssetLoader.cs`
- Create: `src/TranscriptExtractor.Core/Extraction/ExtractionRequestBuilder.cs`
- Create: `tests/TranscriptExtractor.Tests/Prompts/ExtractionRequestBuilderTests.cs`

- [ ] **Step 1: Write failing tests that prove transcript text and schema text are injected into the user prompt template**
- [ ] **Step 2: Run targeted tests and confirm the failure is due to missing implementation**
- [ ] **Step 3: Implement prompt asset loading and request building with fixed version metadata**
- [ ] **Step 4: Run targeted tests, then full suite**
- [ ] **Step 5: Commit**

### Task 4: Add Transcript Intake and Status APIs

**Files:**
- Create: `src/TranscriptExtractor.Api/Contracts/CreateTranscriptRequest.cs`
- Create: `src/TranscriptExtractor.Api/Contracts/TranscriptStatusResponse.cs`
- Modify: `src/TranscriptExtractor.Api/Program.cs`
- Create: `tests/TranscriptExtractor.Tests/Api/TranscriptEndpointsTests.cs`

- [ ] **Step 1: Write failing endpoint tests for transcript creation and status retrieval**
- [ ] **Step 2: Run targeted tests and verify RED**
- [ ] **Step 3: Implement minimal API endpoints backed by the DbContext**
- [ ] **Step 4: Run targeted tests, then full suite**
- [ ] **Step 5: Commit**

### Task 5: Add Worker Extraction Flow

**Files:**
- Modify: `src/TranscriptExtractor.Worker/Program.cs`
- Create: `src/TranscriptExtractor.Core/Extraction/ITranscriptExtractionClient.cs`
- Create: `src/TranscriptExtractor.Core/Extraction/OpenAiTranscriptExtractionClient.cs`
- Create: `src/TranscriptExtractor.Core/Extraction/TranscriptExtractionOrchestrator.cs`
- Create: `tests/TranscriptExtractor.Tests/Worker/TranscriptExtractionOrchestratorTests.cs`

- [ ] **Step 1: Write failing tests for queued-job claim, success persistence, and failure handling**
- [ ] **Step 2: Run targeted tests and verify RED**
- [ ] **Step 3: Implement minimal orchestrator and worker wiring**
- [ ] **Step 4: Run targeted tests, then full suite**
- [ ] **Step 5: Commit**

### Task 6: Add Extraction Retrieval and Report Composition

**Files:**
- Create: `src/TranscriptExtractor.Core/Reports/TranscriptReportViewModel.cs`
- Create: `src/TranscriptExtractor.Core/Reports/TranscriptReportComposer.cs`
- Modify: `src/TranscriptExtractor.Api/Program.cs`
- Create: `tests/TranscriptExtractor.Tests/Reports/TranscriptReportComposerTests.cs`
- Create: `tests/TranscriptExtractor.Tests/Api/ExtractionEndpointsTests.cs`

- [ ] **Step 1: Write failing tests for composing timeline, allegations, statement summaries, object cards, relationship items, and locations from JSON fixtures**
- [ ] **Step 2: Run targeted tests and verify RED**
- [ ] **Step 3: Implement the minimal JSON-to-view-model composition layer and extraction retrieval endpoint**
- [ ] **Step 4: Run targeted tests, then full suite**
- [ ] **Step 5: Commit**

### Task 7: Add HTML Report Template and PDF Endpoint

**Files:**
- Create: `src/TranscriptExtractor.Core/Reports/TranscriptReportHtmlRenderer.cs`
- Create: `src/TranscriptExtractor.Core/Reports/ITranscriptPdfRenderer.cs`
- Create: `src/TranscriptExtractor.Core/Reports/StubTranscriptPdfRenderer.cs`
- Create: `src/TranscriptExtractor.Core/Reports/ReportTemplateVersion.cs`
- Modify: `src/TranscriptExtractor.Api/Program.cs`
- Create: `tests/TranscriptExtractor.Tests/Reports/TranscriptReportHtmlRendererTests.cs`
- Create: `tests/TranscriptExtractor.Tests/Api/PdfEndpointTests.cs`

- [ ] **Step 1: Write failing tests for HTML section rendering, template version exposure, and PDF endpoint readiness behavior**
- [ ] **Step 2: Run targeted tests and verify RED**
- [ ] **Step 3: Implement the minimal infographic HTML renderer and a temporary PDF renderer abstraction**
- [ ] **Step 4: Run targeted tests, then full suite**
- [ ] **Step 5: Commit**

### Task 8: Add Configuration and Developer Docs

**Files:**
- Modify: `src/TranscriptExtractor.Api/appsettings.json`
- Modify: `src/TranscriptExtractor.Worker/appsettings.json`
- Create: `README.md`

- [ ] **Step 1: Document connection strings, prompt asset paths, and OpenAI settings**
- [ ] **Step 2: Add a short local run guide for API and worker**
- [ ] **Step 3: Run `dotnet build TranscriptExtractor.slnx` and the full test suite again**
- [ ] **Step 4: Commit**
