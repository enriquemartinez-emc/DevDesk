# AGENTS.md — DevDesk

## What this is

DevDesk is a support-ticket assistant. Support agents ask it things like:

- _"What's the status of ticket #4821?"_ → answered from **structured** data (Postgres `tickets` table).
- _"How do we reset a customer's 2FA?"_ → answered from **unstructured** runbooks via RAG (pgvector).
- _"Has this come up before, and what's the fix?"_ → requires combining both, which is why the agent has more than one tool.

## Tech stack

| Layer                      | Choice                                                                                                                                                                                       |
| -------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Runtime                    | .NET 10, C# 14                                                                                                                                                                               |
| Orchestration              | .NET Aspire 13.5, **Starter App (ASP.NET Core/React)** template — Minimal API + React, no Docker Compose                                                                                     |
| Cross-cutting              | `DevDesk.ServiceDefaults` — Aspire project, provides shared OpenTelemetry, health checks, and resilience for every service                                                                   |
| Agent                      | Microsoft Agent Framework (MAF), GA — `AIAgent` + `Workflows` for multi-step graphs                                                                                                          |
| Tool exposure              | Model Context Protocol (MCP) — custom `DevDesk.Mcp` server groups `LookupTicket` + `SearchRunbooks`                                                                                          |
| Agent ↔ frontend streaming | **AG-UI**, via the official .NET SDK — replaces a single-shot `/chat` POST with a real event stream (tool calls, text deltas, run lifecycle)                                                 |
| Database                   | PostgreSQL 18 + pgvector — structured (`tickets`) and vector (`doc_chunks`) data in one instance                                                                                             |
| Object storage             | Azure Storage emulator (Azurite) — raw runbooks before ingestion                                                                                                                             |
| LLM + embeddings           | Ollama (community toolkit integration) — local chat + embedding models                                                                                                                       |
| API docs                   | **Scalar** (planned, not yet added) — interactive reference UI over the Minimal API's OpenAPI document                                                                                                                |
| Typed client generation    | **swagger-typescript-api** — generates the frontend's typed API client from that same OpenAPI document                                                                                       |
| Frontend                   | React + Vite, **TanStack Query** (server state/caching), **Zod** (runtime validation), **shadcn/ui** (components)                                                                            |
| Observability              | OpenTelemetry throughout, Aspire Dashboard (dev loop), DevUI (agent conversation/tool-call debugging), `grafana-lgtm` container (Collector + Prometheus + Loki + Tempo + Grafana, one image) |

## Architecture

```
DevDesk.AppHost (orchestrator; names, images, models and ports are constants beside AppHost.cs)
 ├─ frontend           React + Vite + shadcn/ui — chat UI (AG-UI stream) + tickets panel
 ├─ DevDesk.Server     Minimal API — ticket CRUD; forwards /agui to the agent in published builds
 ├─ DevDesk.Agent      Hosts the AIAgent (Microsoft Agent Framework): AG-UI + OpenAI Responses endpoints
 ├─ DevDesk.Mcp        MCP server — groups LookupTicket + SearchRunbooks as tools
 ├─ DevDesk.Ingestion  One-shot job — docs/ → Azurite blobs → chunk → embed → doc_chunks
 ├─ DevUI              In-process aggregator (no container) — agent conversation/tool-call debugging
 ├─ Postgres 18 (pgvector image)     — tickets + doc_chunks
 ├─ Azure Storage emulator (Azurite) — raw runbook docs before ingestion
 ├─ Ollama (community toolkit)       — local chat + embedding models
 └─ grafana-lgtm container           — Collector + Prometheus + Loki + Tempo + Grafana, one image
```

Every one of these is a native Aspire resource, added in `DevDesk.AppHost/AppHost.cs`. `aspire start` is the only setup command — it starts the database, storage emulator, local models, every service, and the full observability stack together.

**Chat flow**: the browser streams AG-UI events from `/agui`. In dev, Vite proxies `/agui` straight to `DevDesk.Agent`; in a published build `DevDesk.Server` forwards it (YARP). The agent calls tools on `DevDesk.Mcp`; `LookupTicket` calls the Server's ticket API and `SearchRunbooks` embeds the query with Ollama and searches `doc_chunks` in pgvector.

**Agent design**: `DevDesk.Agent` doesn't define its tools inline. It discovers `LookupTicket` and `SearchRunbooks` from `DevDesk.Mcp` — that's what "grouping the tools on an MCP server" means here. `DevDesk.Agent` is a separate service from `DevDesk.Server` deliberately: ticket CRUD and LLM calls have different latency, cost, and scaling profiles.

**Observability**: MAF and Microsoft.Extensions.AI instrument themselves with OpenTelemetry, and `ServiceDefaults` exports every project's traces, metrics and logs to both the Aspire Dashboard and `grafana-lgtm` (the AppHost passes each service `GRAFANA_OTLP_ENDPOINT`). The `grafana-lgtm` container is a single image that bundles the Collector, Prometheus (metrics), Loki (logs), Tempo (traces), and Grafana — no hand-written collector config. Use the Aspire Dashboard for day-to-day dev-loop debugging and Grafana for a production-shaped view; they're not redundant, they're different altitudes. DevUI adds a third altitude: stepping through one agent conversation's tool calls specifically.

## Folder structure

```
DevDesk/
├── DevDesk.AppHost/              # orchestrator — AppHost.cs plus ResourceNames.cs, ContainerImages.cs,
│                                  # ModelNames.cs, Topology.cs constants, no other logic
├── DevDesk.ServiceDefaults/      # shared OpenTelemetry, health checks, service discovery, resilience
├── DevDesk.Server.Core/          # domain logic — zero I/O references, ever
│   └── Features/
│       └── Tickets/
│           └── Core.cs           # Ticket (closed hierarchy: Open/InProgress/Resolved/Closed) + TicketRules
├── DevDesk.Server.Core.Tests/    # unit tests against DevDesk.Server.Core, zero mocks, zero DB
├── DevDesk.Server/               # Minimal API
│   ├── Common/                   # validation filter shared by endpoints
│   └── Features/
│       ├── Tickets/              # one file per use case: request, validator, endpoint, handler, SQL,
│       │                         # and a file-scoped Db*Row DTO; plus TicketsEndpoints.cs (route group)
│       └── Chat/ChatProxy.cs     # forwards /agui to the agent
├── DevDesk.Mcp/                  # MCP server — LookupTicket, SearchRunbooks tools
├── DevDesk.Agent/                # hosts the AIAgent, MCP client, chat model, AG-UI + DevUI endpoints
├── DevDesk.Ingestion/            # one-shot job: uploads docs/ to Azurite, chunks, embeds, writes doc_chunks
├── frontend/                     # React + Vite + shadcn/ui
│   ├── openapi/                  # committed OpenAPI document the API client is generated from
│   └── src/{api,features,components,lib}   # api/ is generated; features/ hold hooks + components
└── docs/                         # sample runbooks, ingested into pgvector via Azurite
```

New backend features put pure decisions in `Features/{FeatureName}/Core.cs` in `DevDesk.Server.Core` and everything with I/O in one use-case file per endpoint under `DevDesk.Server/Features/{FeatureName}/`, following `emc-fcis-feature-slice`. Don't add `Data` or `Mapper` classes, and don't put I/O in `Core.cs`.

## Gotchas

Things that cost time once and aren't obvious from the code:

- **Postgres 18 data volume**: `WithDataVolume()` picks the container data directory from the image tag at call time, and the pgvector tag doesn't parse as 18+. Call it *before* `WithImage(...)`.
- **Slow local models**: `ServiceDefaults` raises the standard resilience timeouts (3 min per attempt, 6 min total); the ~30s defaults cancelled Ollama chat calls mid-generation.
- **MCP endpoint**: the MCP client transport needs a concrete `http(s)` URL and rejects Aspire's `https+http://` scheme, so `DevDesk.Agent` reads `services:mcp:*` configuration.
- **AG-UI nulls**: the Agent Framework hosting package loses the AG-UI SDK's omit-nulls rule on net10, and the TypeScript client rejects `null` fields. `DevDesk.Agent` inserts `AGUIJsonUtilities.DefaultTypeInfoResolver` into the JSON options; keep it.
- **One message per model turn**: OllamaSharp streaming updates have no message id, so `StableMessageIdChatClient` stamps one; without it every token becomes its own AG-UI message.
- **Startup cycle**: `server` → `agent` → `mcp` → `server`. The Server only references the agent (no `WaitFor`); don't add one.
- **DevUI**: the AppHost needs `Microsoft.Agents.AI.DevUI` directly (it holds the web UI; without it `/devui/` is 404). The agent name in `DevDesk.Agent/Program.cs` must match `ResourceNames.SupportAgent`.
- **API client generation**: don't pass `--http-client` to `swagger-typescript-api`; any value selects Axios. Regenerate with `npm run fetch:openapi` then `npm run generate:api`.

## Available agent skills

- `emc-fcis-feature-slice` — use for any .NET endpoint business
  logic: Core/handler split, immutable domain records
- `emc-pragmatic-type-driven-domain-modeling` — use when modeling new C# domain
  types, especially status/lifecycle fields (prefer sum types over
  enums/booleans for state)
  - `emc-endpoint-style-minimal-api` - use when adding or changing endpoint routes,
    HTTP contracts, validation, OpenAPI, or resource creation responses
  - `supabase-postgres-best-practices` - use when working with postgresql
  - `vercel-react-best-practices` - use when working with Nextjs
  - `shadcn` - use when using shadcn ui components, use the latest haiku model to work with this skill.
  - `emc-react-frontend-feature` use when working with React frontend features with ShadCN UI, TanStack Query, and an OpenAPI-generated client with swagger-typescript-api.
  - all Aspire skills available in this project.

## Non-negotiable rules

1. **No magic strings or numbers in `DevDesk.AppHost/AppHost.cs`.** Names, image tags, model names, and ports live in `ResourceNames.cs`, `ContainerImages.cs`, `ModelNames.cs`, and `Topology.cs`, alongside `AppHost.cs` in the same project — no separate `AppHost.Core` project. `AppHost.cs` should read as a list of resources, nothing to decode.
2. **`DevDesk.Server.Core` has zero I/O package references** — no Npgsql, no Dapper, no ASP.NET. If a change makes `DevDesk.Server.Core` need one of those, the logic belongs in a handler in `DevDesk.Server` instead.
3. **Tools live in `DevDesk.Mcp`, not inline in `DevDesk.Agent`.** The agent discovers tools; it doesn't define them.
4. **`DevDesk.Agent` and `DevDesk.Server` stay separate services.** Don't merge them for convenience.

## Git workflow

- Commit only when explicitly asked, and only after the user has approved the specific change.
- Use [Conventional Commits](https://www.conventionalcommits.org/) for every commit message (`feat:`, `fix:`, `chore:`, `docs:`, etc.).

## Working guidelines

- **No browser end-to-end tests without approval.** Don't drive the app with `playwright-cli` (or any other browser automation) unless the user explicitly says to. Until then, make frontend changes and verify them with the type check, lint, and build only.
- **Format C# after every change.** Whenever you change a `.cs` file or a related C# file, run `dotnet csharpier format .` from the repo root before finishing.

## When something isn't covered here

Flag the gap and propose an approach rather than guessing silently — especially anything that would introduce a new container, a new orchestration mechanism, or a change to the `Core`/handler split described above.
