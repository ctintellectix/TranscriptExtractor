# Operator Dashboard Design

## Summary

Add a new `TranscriptExtractor.Ui` Razor Pages project that serves as an internal operator dashboard for the transcript extraction pipeline. The UI will consume the existing API for transcript intake, transcript detail, extraction retrieval, and PDF download. The API will be extended with dashboard-oriented endpoints for queue metrics, recent activity, and worker health. The worker will publish a lightweight heartbeat so operators can distinguish between an empty queue and a dead or stale worker.

The initial release is intentionally unauthenticated and intended for a trusted internal environment. Authentication can be added later without changing the basic project boundaries.

## Goals

- Provide a simple internal dashboard for operating the extraction pipeline.
- Reuse the API as the boundary between UI and application/data logic.
- Surface explicit worker health in addition to persisted job state.
- Give operators a human-readable narrative snapshot of an extraction.
- Preserve the existing PDF generation flow and expose it through the UI.

## Non-Goals

- Add authentication or authorization in this phase.
- Replace the API with server-side data access from the UI.
- Introduce a SPA frontend stack.
- Redesign the extraction schema or report pipeline.

## Current State

The solution is currently split into:

- `TranscriptExtractor.Api`: transcript intake, transcript status, extraction retrieval, and PDF endpoints
- `TranscriptExtractor.Worker`: background polling host that processes queued extraction jobs
- `TranscriptExtractor.Core`: EF Core persistence, entities, orchestration, prompt loading, extraction client, and report generation
- `TranscriptExtractor.Tests`: API, worker, and core tests

This is a workable base, but there is no operator-facing application and no explicit monitoring API for worker liveness.

## Proposed Architecture

### New Project

Add `src/TranscriptExtractor.Ui` as a standalone Razor Pages web project in the solution.

Responsibilities:

- Render operator pages
- Call the API over HTTP
- Project extraction data into a readable narrative snapshot
- Provide links and actions for raw JSON inspection and PDF download

Non-responsibilities:

- Direct database access
- Job orchestration
- Report generation
- Worker liveness calculation from logs

### Existing Project Responsibilities

`TranscriptExtractor.Api` will remain the HTTP boundary for the system. It will continue to own transcript submission, transcript retrieval, extraction retrieval, and PDF generation, and it will gain dashboard-oriented read endpoints.

`TranscriptExtractor.Worker` will continue to process queued jobs and will additionally publish heartbeat information that describes its current liveness and recent activity.

`TranscriptExtractor.Core` will remain the shared application/persistence layer and will gain the minimal model support needed for worker heartbeat persistence and API query access.

## UI Scope

### 1. Dashboard

The dashboard home page should show:

- counts for queued, processing, completed, and failed jobs
- recent transcripts or jobs with status and timestamps
- worker health status
- last heartbeat timestamp
- last successful processing timestamp
- a stale/offline warning if the worker heartbeat is too old

This page is for operators, not end users. It should optimize for fast triage.

### 2. Transcript Intake

Provide a simple form that submits transcript content and metadata through the existing `POST /transcripts` endpoint. On success, redirect to the transcript detail page.

### 3. Transcript Detail

Provide a transcript-centric page that shows:

- transcript metadata
- current extraction job status
- received/interview timestamps
- actions to refresh status
- link to curated narrative snapshot
- link to raw extraction JSON
- link to download PDF

### 4. Narrative Snapshot

Provide a curated human-readable page derived from extraction JSON. This page is the main operator-facing view of the extracted result.

Suggested sections:

- case metadata
- people involved
- locations
- timeline or sequence of events
- notable statements
- relationships or associations

The page should also expose a raw JSON tab or panel for debugging and validation.

## API Additions

The existing endpoints are already useful for transcript-specific workflows:

- `POST /transcripts`
- `GET /transcripts/{id}`
- `GET /transcripts/{id}/extraction`
- `GET /reports/transcripts/{id}/pdf`

Add dashboard-specific endpoints:

- `GET /dashboard/summary`
- `GET /dashboard/recent`
- `GET /worker/health`

### Dashboard Summary

Returns aggregate counts and timestamps needed by the home dashboard:

- queued count
- processing count
- completed count
- failed count
- last transcript received timestamp
- last completed job timestamp

### Dashboard Recent

Returns a recent list suitable for dashboard tables:

- transcript id
- case number
- interviewer
- received at
- current job status
- job timestamps
- failure message when relevant

### Worker Health

Returns a health payload derived from the latest heartbeat record:

- current status: `healthy`, `idle`, `stale`, or `offline`
- last poll time
- last successful job time
- current worker instance id if available
- optional status message

## Worker Heartbeat Design

The current worker logs when no work is available but does not publish operational state. To support explicit monitoring, add a persisted heartbeat record updated by the worker.

The heartbeat should capture:

- worker instance identifier
- last poll timestamp
- last successful job timestamp
- optional last error timestamp/message

Behavior:

- update heartbeat each poll cycle
- update last successful job timestamp when a job completes
- optionally store last error when processing fails unexpectedly

The API will interpret heartbeat freshness using a simple threshold. For example:

- `healthy`: recent heartbeat and recent successful work
- `idle`: recent heartbeat but no recent completed work
- `stale`: heartbeat older than threshold
- `offline`: no heartbeat record

Thresholds should be configuration-driven or at least centralized in one place.

## Data and Domain Changes

Add a lightweight persistence model in `Core` for worker status, likely a `WorkerHeartbeat` entity/table. This should be intentionally small and purpose-specific.

Avoid introducing UI-specific projection types into `Core` unless they are needed by multiple application layers. The UI can own most view models, while the API can own response DTOs for dashboard endpoints.

## UI Integration Model

The UI project should use a typed HTTP client for API access. This client should encapsulate endpoint URLs and response deserialization so page models remain focused on display flow.

Suggested UI internal structure:

- `Api/TranscriptExtractorApiClient.cs`
- `Pages/Dashboard/Index.cshtml`
- `Pages/Transcripts/New.cshtml`
- `Pages/Transcripts/Details.cshtml`
- `Pages/Transcripts/Snapshot.cshtml`
- `Models/` or `ViewModels/` for page-facing DTOs

The PDF action should redirect or link directly to the API PDF endpoint, depending on deployment topology.

## Narrative Snapshot Strategy

The extraction endpoint currently returns raw JSON. The UI should parse that payload and project it into a readable narrative shape for operators.

Initial strategy:

- keep the extraction JSON as the source of truth
- add a focused parser/projection layer in the UI
- render missing sections defensively when data is absent
- preserve raw JSON visibility for troubleshooting

This avoids changing the extraction storage model while still giving operators a practical result view.

## Error Handling

### UI

- show clear empty states when extraction is not ready
- show failure details when a job has failed
- distinguish API errors from missing data
- handle stale worker health as an operational warning

### API

- keep transcript-specific behavior unchanged where possible
- return stable dashboard payloads even when there is no heartbeat yet
- avoid leaking low-level exceptions in monitoring endpoints

## Testing Strategy

Add tests in these areas:

### API

- dashboard summary endpoint returns expected counts
- dashboard recent endpoint returns expected ordering and status data
- worker health endpoint maps heartbeat freshness into the correct status

### Worker

- heartbeat is updated on idle poll
- heartbeat last-success timestamp updates after successful processing

### UI

- API client deserializes dashboard/transcript payloads correctly
- snapshot projection logic converts extraction JSON into narrative sections
- page models handle missing extraction, failed jobs, and stale worker status

The initial UI phase does not require browser automation. Page-model and projection tests are sufficient unless the UI becomes more interactive.

## Deployment and Configuration

The UI project will need:

- API base URL
- environment-appropriate settings for local development and deployment

The API and worker continue to require the existing database and OpenAI configuration. The heartbeat feature may optionally add a monitoring threshold configuration value.

## Tradeoffs

### Why a Separate Razor Pages Project

This keeps the UI isolated from the API host while preserving a simple .NET deployment story. It also keeps the API reusable for future clients.

### Why Not Direct DB Access from UI

Direct database access would speed up initial implementation but would weaken boundaries, duplicate query logic, and complicate future authentication and deployment.

### Why Not Add UI to the API Project

Combining them would reduce project count but blur responsibilities and make the operator dashboard harder to evolve independently.

## Open Questions Deferred

- Authentication and authorization strategy
- Whether worker health should eventually use a more formal health-check or push model
- Whether the narrative snapshot should later gain editable/operator-annotated fields

## Recommended Next Step

Write an implementation plan that phases the work in this order:

1. Add shared monitoring persistence and worker heartbeat updates
2. Add API dashboard and worker-health endpoints
3. Add the Razor Pages UI project and wire API client/configuration
4. Add transcript workflow pages and narrative snapshot
5. Add tests and solution wiring cleanup
