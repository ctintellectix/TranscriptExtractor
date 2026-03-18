# Transcript PDF Pipeline Design

Date: 2026-03-18

## Overview

This project is a transcript-local law-enforcement intelligence pipeline. One interview or transcript is ingested, processed through a fixed prompt-driven extraction call, saved as trusted structured JSON, and rendered into a final infographic-style PDF on demand.

The design intentionally reuses the strongest parts of the current `LvmpdIntel` starter shape:

- an API for intake and retrieval,
- an asynchronous worker for extraction,
- a shared core/data layer,
- database-backed job state and persistence.

Unlike the current project, this design does not build a cross-transcript intelligence graph. The extraction output is scoped to a single transcript and is treated as the canonical source of truth for downstream PDF generation.

## Goals

- Accept one transcript and produce one final PDF.
- Use a single fixed prompt template in v1.
- Persist the exact model JSON as the canonical extraction document.
- Generate the PDF on demand from saved JSON.
- Produce a compact investigative infographic similar to the provided sample PDF.
- Keep the schema and orchestration simple enough to iterate quickly.

## Non-Goals

- Cross-transcript entity linking.
- Contradiction detection across multiple transcripts.
- Prompt profile selection in v1.
- Manual repair or reinterpretation of model output.
- Complex relational normalization of all extracted entities.

## Source Materials

The design is based on the following agreed extraction contract:

- JSON schema with transcript metadata, people, locations, objects, statements, described events, relationship claims, allegations, emotional/behavioral cues, and contradictions.
- System prompt requiring transcript-local extraction only, no invented facts, no cross-transcript linking, and exact JSON schema conformance.
- User prompt template that injects transcript text and JSON schema text into the request.
- Sample PDF showing an infographic-style output with timeline, allegations, statements, key objects, relationships, and key locations.

## Product Shape

The system processes each transcript through four stages:

1. Intake
2. Extraction
3. Persistence
4. On-demand PDF rendering

The extraction result is trusted as authoritative content. The application is responsible for orchestration, traceability, validation at the envelope level, and presentation, not for second-guessing or re-deriving the model's findings.

## Architecture

### API

The API is responsible for:

- accepting transcript submissions,
- returning transcript and job status,
- returning saved extraction content when needed,
- rendering or returning the final PDF on demand.

Suggested endpoints:

- `POST /transcripts`
- `GET /transcripts/{id}`
- `GET /transcripts/{id}/extraction`
- `GET /reports/transcripts/{id}/pdf`

### Worker

The worker is responsible for:

- polling queued extraction jobs,
- building the prompt payload,
- calling the model,
- saving the exact JSON output,
- updating job state for success or failure.

The worker should remain asynchronous even though the product is transcript-local. This keeps model latency, retries, and operational failure handling out of the request/response path.

The worker should depend on a pluggable extraction client abstraction rather than a single hard-coded model implementation. This preserves the simple v1 runtime shape while allowing different model implementations to be swapped in through configuration and dependency injection later.

### Core/Data Layer

The shared core layer defines:

- EF Core entities,
- database context,
- extraction orchestration abstractions,
- extraction client interfaces and provider-specific implementations,
- prompt asset loading,
- PDF composition abstractions.

### PDF Composition Layer

The PDF generator is a presentation layer over the saved JSON document. It transforms atomic extraction content into infographic sections. This layer is where grouping, labeling, numbering, ordering, and layout decisions live.

## Data Model

The design keeps the relational schema small and uses JSON for the extraction payload.

### Transcript

Represents the original submission.

Suggested fields:

- `Id`
- `ReceivedAt`
- `TranscriptText`
- `CaseNumber` or external reference if available
- `InterviewDateTime` if known separately from receipt time
- `Location`
- `Interviewer`
- `SourceType`
- optional source file metadata

### ExtractionJob

Tracks processing state.

Suggested fields:

- `Id`
- `TranscriptId`
- `Status` (`Queued`, `Processing`, `Completed`, `Failed`)
- `CreatedAt`
- `UpdatedAt`
- `StartedAt`
- `CompletedAt`
- `RetryCount`
- `Error`
- `Model`
- `PromptVersion`

### ExtractionDocument

Stores the canonical JSON result.

Suggested fields:

- `Id`
- `TranscriptId`
- `ExtractionJobId`
- `Json` (`jsonb`)
- `CreatedAt`
- `Model`
- `PromptVersion`
- `ReportTemplateVersion`
- optional request identifiers for traceability

### Optional PdfArtifact

Not required for v1. Add later if caching or download auditing becomes important.

Possible future fields:

- `Id`
- `TranscriptId`
- `TemplateVersion`
- `StoragePath`
- `Hash`
- `GeneratedAt`

## Prompting Model

V1 uses a single fixed prompt contract made of three assets:

- system prompt,
- user prompt template,
- JSON schema text.

The worker builds the request by injecting:

- transcript text into `<<<TRANSCRIPT_TEXT>>>`,
- schema text into `<<<JSON OUTPUT SCHEMA>>>`.

The prompt assets should be stored outside inline worker logic, either as configuration-backed files or versioned prompt files in the repo. Each extraction should record the prompt version used so a generated PDF can always be traced back to the exact extraction contract.

Although v1 uses a single prompt profile, the extraction path should still be designed behind an interface boundary. The recommended shape is:

- prompt asset loader,
- extraction request builder,
- extraction client interface,
- provider-specific extraction client implementation.

This keeps model selection flexible without introducing a separate extraction service before it is needed.

## Extraction Rules

The following rules are design-critical:

- model output is the source of truth,
- extraction is transcript-local only,
- unknown values remain `null`,
- inferred content must be explicitly labeled,
- invalid or empty JSON is a failed extraction,
- the system does not "repair" or reinterpret failed output in v1.

This boundary is important. The app should validate enough to determine whether the output is usable, but should not transform it into a different semantic truth model.

## Processing Flow

### Intake

`POST /transcripts` stores the transcript and creates an `ExtractionJob` in `Queued` state.

### Extraction

The worker claims the oldest queued job, marks it `Processing`, builds the prompt payload, calls the model, and persists the exact JSON result into `ExtractionDocument`.

### Completion

On success:

- `ExtractionDocument` is saved,
- job becomes `Completed`,
- model and prompt version metadata are stored.

On failure:

- job becomes `Failed`,
- error summary is recorded,
- no PDF is generated automatically.

### Report Rendering

`GET /reports/transcripts/{id}/pdf` loads the saved extraction JSON and composes the infographic PDF on demand.

If extraction is:

- `Queued` or `Processing`: return a clear not-ready response,
- `Failed`: return extraction failure details appropriate for the client,
- `Completed`: render the PDF from saved JSON.

## PDF Design

The target output style is a compact investigative infographic matching the provided sample rather than a prose-first report.

### Primary Sections

Recommended PDF sections for v1:

- transcript header / metadata
- timeline of events
- allegations
- statements
- key objects
- relationships
- key locations
- emotional/behavioral cues
- contradictions

### Mapping from JSON to PDF

- `transcript_metadata` -> document header
- `described_events` -> timeline
- `allegations` -> allegations panel
- `statements` -> statement summaries
- `objects` -> key objects cards/list
- `relationship_claims` -> relationships section
- `locations` -> key locations section
- `emotional_behavioral_cues` -> cues section
- `contradictions` -> contradictions section
- `people` -> names, roles, speaker labels, relationship endpoints, timeline participants

### Composition Rules

Because the JSON is atomic and graph-like, the PDF layer must compose it into presentation groups. It should:

- sort items into stable visual sections,
- resolve human-readable labels from temporary IDs,
- number timeline items and locations,
- summarize dense graph data into readable blocks,
- preserve evidence-oriented language and confidence context where useful.

The PDF layer should not introduce new factual claims. It may reorganize, label, and format content, but must stay faithful to the saved JSON.

## Layout Strategy

Use an HTML-to-PDF or similar templated rendering approach rather than handwritten PDF drawing primitives if possible. The sample output is design-forward and sectioned, which is easier to iterate in HTML/CSS.

Suggested approach:

- render a structured HTML report,
- apply a stable print stylesheet,
- convert to PDF using the chosen renderer.

Benefits:

- easier visual iteration,
- better typography and spacing control,
- simpler section composition,
- faster adjustment to future layout changes.

## Validation Strategy

Even though the JSON is trusted as truth, the system still needs envelope validation.

Validate:

- response is valid JSON,
- top-level shape is present,
- critical arrays/objects exist or default safely,
- IDs are structurally usable for rendering,
- referenced IDs resolve within the same document before rendering.

This referential validation should cover relationships like:

- `statements.speaker_id` -> `people.person_id`
- `described_events.participants` -> `people.person_id`
- `described_events.location_id` -> `locations.location_id`
- `described_events.source_statement_id` -> `statements.statement_id`
- `relationship_claims.subject.entity_id` and `relationship_claims.object.entity_id` -> matching entity collections
- `relationship_claims.source_statement_id` -> `statements.statement_id`
- `allegations.alleged_perpetrator_id`, `allegations.victim_id`, `allegations.reported_by` -> `people.person_id`
- `allegations.location_id` -> `locations.location_id`
- `emotional_behavioral_cues.person_id` -> `people.person_id`
- `emotional_behavioral_cues.observed_during_statement_id` -> `statements.statement_id`
- `contradictions.person_id` -> `people.person_id`
- `contradictions.statement_ids` -> `statements.statement_id`

If referential integrity fails, the extraction should remain stored as returned, but the document should be marked unusable for PDF rendering until the issue is resolved or explicitly overridden.

Do not:

- rewrite field meanings,
- infer missing relationships,
- repair contradictory content,
- merge duplicate entities across transcripts.

## Error Handling

### Extraction Errors

Examples:

- model call failed,
- non-success API response,
- invalid JSON,
- empty content,
- schema-incompatible content.

Behavior:

- mark job `Failed`,
- store error summary,
- leave transcript intact for retry or inspection.

### PDF Errors

PDF generation errors are separate from extraction validity. If the saved JSON exists but rendering fails, the extraction remains valid and the report request returns a rendering failure.

This separation avoids losing good model output because of presentation issues.

### PDF Reproducibility

Because reports are rendered on demand, the system should version the report template separately from the extraction prompt. Each rendered report path should know which `ReportTemplateVersion` it is using, and the extraction document should retain the template version expected by the current renderer.

This preserves auditability when layout, grouping, or styling changes over time. The same JSON should not silently render into materially different official output without a visible version boundary.

## Testing Strategy

### Unit Tests

- prompt payload builder
- job state transitions
- extraction response validation
- JSON-to-view-model composition for PDF sections
- ID resolution for people, locations, objects, and statements

### Integration Tests

- transcript intake creates queued job
- worker processes queued job and saves extraction document
- completed extraction can be requested
- PDF endpoint handles queued, completed, and failed cases correctly

### Fixture-Based Report Tests

Use representative JSON fixtures based on the agreed schema to validate rendering behavior. Favor snapshot or golden-style checks at the HTML intermediate level if full PDF binary comparisons are too brittle.

## Why This Design Fits Better Than The Current Starter

The current starter is optimized for transcript enrichment that builds a cross-record intelligence picture with linked people, incidents, and locations. The new project is transcript-local and document-centric.

This design keeps the helpful parts:

- API/worker separation,
- durable job state,
- PostgreSQL + EF Core,
- on-demand reporting.

It drops or avoids the parts that do not fit:

- cross-transcript identity linking,
- relationship persistence as a normalized graph,
- report logic based on aggregated incident history.

## Recommended Initial Implementation Order

1. Define new entities and migrations for `Transcript`, `ExtractionJob`, and `ExtractionDocument`.
2. Add transcript intake and status endpoints.
3. Move prompt assets into versioned files/configuration.
4. Implement worker extraction flow using the fixed prompt template and saved schema text.
5. Add extraction retrieval endpoint for inspection/debugging.
6. Build JSON-to-report composition layer.
7. Implement HTML report template matching the sample infographic style.
8. Add on-demand PDF generation endpoint.
9. Add fixture-driven tests for extraction handling and PDF composition.

## Open Decisions For Later

- whether to cache generated PDFs,
- whether to support multiple prompt profiles,
- whether to expose a browser HTML preview alongside PDF,
- whether to add searchable extracted summary fields outside the JSON blob,
- whether to retain original model request/response payloads for audit.

## Recommendation

Build the new project as a lean transcript-processing pipeline centered on a canonical `ExtractionDocument` JSON payload and an infographic-style on-demand PDF renderer. Reuse the current repo's API/worker/core split, but do not carry over cross-transcript intelligence graph behavior.

This keeps v1 aligned with the provided schema, prompts, and sample output while leaving room for later expansion.
