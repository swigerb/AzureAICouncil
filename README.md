# AzureAICouncil

Azure AI Council is a deliberative, multi-agent decision system built with **.NET (Aspire + Minimal API)** and a **React/Vite** SPA.

It runs a 3-round “council” deliberation (parallel councilors, deterministic ordered emission), streams progress to the UI via **Server-Sent Events (SSE)**, and emits dev telemetry to the **.NET Aspire Dashboard (OpenTelemetry/OTLP)**.

## Repo layout

- `src/` – .NET projects
  - `AzureAiCouncil.AppHost` – Aspire AppHost (orchestrates local dev)
  - `AzureAiCouncil.ServiceDefaults` – shared Aspire/OpenTelemetry defaults
  - `AzureAiCouncil.Api` – Minimal API + SSE
  - `AzureAiCouncil.Core` – domain models + in-memory store/event bus + Chair orchestration
  - `AzureAiCouncil.Agents` – Speaker/Councilors/Clerk agents (Azure OpenAI + mock fallback)
- `tests/` – xUnit unit tests
- `web/azure-ai-council-spa/` – React/Vite UI

## Prerequisites

- .NET SDK (repo is pinned via `global.json`)
- Node.js + npm (for the SPA)

## Quickstart (dev)

1) Start the Aspire app host:

```bash
dotnet run --project src/AzureAiCouncil.AppHost
```

2) Start the SPA:

```bash
cd web/azure-ai-council-spa
npm install
npm run dev
```

3) Open the SPA (default Vite URL):

- `http://localhost:5173`

The SPA calls the API at `http://localhost:5139` by default.

## Azure OpenAI configuration (optional)

If Azure OpenAI isn’t configured, the system runs in **mock agent mode** (useful for UI/dev flows).

To enable real deliberation, configure these settings for the API:

- `AzureOpenAI:Endpoint` (e.g. `https://YOUR-RESOURCE-NAME.openai.azure.com/`)
- `AzureOpenAI:ApiKey`
- `AzureOpenAI:Deployment`

Recommended: use **dotnet user-secrets** so you don’t commit keys.

```bash
dotnet user-secrets init --project src/AzureAiCouncil.Api
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE-NAME.openai.azure.com/" --project src/AzureAiCouncil.Api
dotnet user-secrets set "AzureOpenAI:ApiKey" "<YOUR-KEY>" --project src/AzureAiCouncil.Api
dotnet user-secrets set "AzureOpenAI:Deployment" "YOUR-DEPLOYMENT-NAME" --project src/AzureAiCouncil.Api
```

Environment variables also work:

- `AzureOpenAI__Endpoint`
- `AzureOpenAI__ApiKey`
- `AzureOpenAI__Deployment`

## Observability (Aspire)

When running via `AzureAiCouncil.AppHost`, services are wired for OpenTelemetry and should appear in the Aspire dashboard (logs/traces/metrics) automatically.

## API

- `POST /api/motions` – create a motion and start deliberation in the background
- `GET /api/deliberations/{id}` – fetch current deliberation snapshot
- `GET /api/deliberations/{id}/stream` – SSE stream of events

SSE event names:

- `stream.ready`
- `session.started`
- `round.started`
- `councilor.status`
- `statement`
- `round.completed`
- `resolution`
- `error`

## Tests

```bash
dotnet test tests/AzureAiCouncil.Tests/AzureAiCouncil.Tests.csproj
```

## Notes

- Secrets: do not commit API keys. Prefer user-secrets or environment variables.
- Streaming: SSE is the v1 transport; SignalR can be added later if desired.
