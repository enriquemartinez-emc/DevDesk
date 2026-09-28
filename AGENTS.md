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
| API docs                   | **Scalar** — interactive reference UI over the Minimal API's OpenAPI document                                                                                                                |
| Typed client generation    | **swagger-typescript-api** — generates the frontend's typed API client from that same OpenAPI document                                                                                       |
| Frontend                   | React + Vite, **TanStack Query** (server state/caching), **Zod** (runtime validation), **shadcn/ui** (components)                                                                            |
| Observability              | OpenTelemetry throughout, Aspire Dashboard (dev loop), DevUI (agent conversation/tool-call debugging), `grafana-lgtm` container (Collector + Prometheus + Loki + Tempo + Grafana, one image) |

## Architecture

```
DevDesk.AppHost (orchestrator, config lives in DevDesk.AppHost.Core)
 ├─ frontend           React + Vite + shadcn/ui — chat UI
 ├─ DevDesk.Server    Minimal API — ticket CRUD, proxies /chat to the agent
 ├─ DevDesk.Agent         Hosts the AIAgent (Microsoft Agent Framework)
 ├─ ├─ DevUI                          — agent conversation/tool-call
 ├─ DevDesk.Mcp           MCP server — groups LookupTicket + SearchRunbooks as tools
 ├─ DevDesk.Ingestion     One-shot job — docs/ → Azurite blobs → chunk → embed → doc_chunks
 ├─ Postgres 18 (pgvector image)   — tickets + doc_chunks
 ├─ Azure Storage emulator (Azurite) — raw runbook docs before ingestion
 ├─ Ollama (community toolkit)     — local chat + embedding models
 debugging
 └─ grafana-lgtm container         — Collector + Prometheus + Loki + Tempo + Grafana, one image
```

Every one of these is a native Aspire resource, added in `DevDesk.AppHost/Program.cs`. `aspire run` is the only setup command — it starts the database, storage emulator, local models, every service, and the full observability stack together.

**Agent design**: `DevDesk.Agent` doesn't define its tools inline. It discovers `LookupTicket` and `SearchRunbooks` from `DevDesk.Mcp` — that's what "grouping the tools on an MCP server" means here. `DevDesk.Agent` is a separate service from `DevDesk.Server` deliberately: ticket CRUD and LLM calls have different latency, cost, and scaling profiles.

**Observability**: MAF instruments itself with OpenTelemetry, and Aspire's `ServiceDefaults` wires every project's OTLP export automatically. The `grafana-lgtm` container is a single image that bundles the Collector, Prometheus (metrics), Loki (logs), Tempo (traces), and Grafana — no hand-written collector config. Use the Aspire Dashboard for day-to-day dev-loop debugging and Grafana for a production-shaped view; they're not redundant, they're different altitudes. DevUI adds a third altitude: stepping through one agent conversation's tool calls specifically.

## Folder structure

```
DevDesk/
├── DevDesk.AppHost/              # orchestrator — AppHost.cs plus ResourceNames.cs, ContainerImages.cs,
│                                  # ModelNames.cs, Topology.cs constants, no other logic
├── DevDesk.Server.Core/                 # domain logic — zero I/O references, ever
│   └── Features/
│       └── Tickets/
│           └── Core.cs           # Ticket, TicketStatus, TicketRules
├── DevDesk.Server.Core.Tests/    # unit tests against DevDesk.Server.Core, zero mocks, zero DB
├── DevDesk.Server/           # Minimal API — ticket CRUD + /chat proxy
│   └── Features/
│       └── Tickets/
│           └── Data.cs           # Dapper queries + row-to-domain mapping (I/O lives here, not in Core)
├── DevDesk.Mcp/                  # MCP server — LookupTicket, SearchRunbooks tools
├── DevDesk.Agent/                # hosts the AIAgent, MCP client, chat model
├── DevDesk.Ingestion/            # one-shot job: uploads docs/ to Azurite, chunks, embeds, writes doc_chunks
├── frontend/                  # React + Vite + shadcn/ui
└── docs/                         # sample runbooks, ingested into pgvector via Azurite
```

New backend features follow the same `Core.cs` (pure) + `Data.cs` (I/O) split as `Tickets`, under `Features/{FeatureName}/`. Don't add a feature folder that mixes both in one file.

## Available agent skills

- `emc-fcis-feature-slice` — use for any .NET endpoint business
  logic: Core/Data.cs/Handler split, immutable domain records
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
2. **`DevDesk.Server.Core` has zero I/O package references** — no Npgsql, no Dapper, no ASP.NET. If a change makes `DevDesk.Server.Core` need one of those, the logic belongs in `Data.cs` or a handler instead.
3. **Tools live in `DevDesk.Mcp`, not inline in `DevDesk.Agent`.** The agent discovers tools; it doesn't define them.
4. **`DevDesk.Agent` and `DevDesk.Server` stay separate services.** Don't merge them for convenience.

## Git workflow

- Commit only when explicitly asked, and only after the user has approved the specific change.
- Use [Conventional Commits](https://www.conventionalcommits.org/) for every commit message (`feat:`, `fix:`, `chore:`, `docs:`, etc.).

## When something isn't covered here

Flag the gap and propose an approach rather than guessing silently — especially anything that would introduce a new container, a new orchestration mechanism, or a change to the `Core`/`Data` split described above.
