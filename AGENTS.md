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
| Tool exposure              | Model Context Protocol (MCP) — custom `DevDesk.Mcp` server groups `LookupTicket` + `SearchRunbooks` + `SearchTickets`                                                                                          |
| Agent ↔ frontend streaming | **AG-UI**, via the official .NET SDK — replaces a single-shot `/chat` POST with a real event stream (tool calls, text deltas, run lifecycle)                                                 |
| Migrations                 | **DbUp** — `DevDesk.Migrations`, a one-shot project that applies numbered SQL scripts before any service starts                                                                               |
| Database                   | PostgreSQL 18 + pgvector — structured (`tickets`) and vector (`doc_chunks`) data in one instance                                                                                             |
| Object storage             | Azure Storage emulator (Azurite) — raw runbooks before ingestion                                                                                                                             |
| LLM + embeddings           | Ollama (community toolkit integration) — local chat + embedding models                                                                                                                       |
| API docs                   | **Scalar** — interactive reference UI over the Minimal API's OpenAPI document, at `/scalar` (Development only)                                                                          |
| Typed client generation    | **swagger-typescript-api** — generates the frontend's typed API client from that same OpenAPI document                                                                                       |
| Frontend                   | React + Vite, **TanStack Query** (server state/caching), **Zod** (runtime validation), **shadcn/ui** (components)                                                                            |
| Observability              | OpenTelemetry throughout, Aspire Dashboard (dev loop), DevUI (agent conversation/tool-call debugging), `grafana-lgtm` container (Collector + Prometheus + Loki + Tempo + Grafana, one image) |

## Architecture

```
DevDesk.AppHost (orchestrator; names, images, models and ports are constants beside AppHost.cs)
 ├─ frontend           React + Vite + shadcn/ui — chat UI (AG-UI stream) + tickets panel
 ├─ DevDesk.Server     Minimal API — ticket CRUD; also hosts the support agent (Agent Framework workflow): AG-UI at /agui + OpenAI Responses endpoints
 ├─ DevDesk.Mcp        MCP server — groups LookupTicket + SearchRunbooks + SearchTickets as tools
 ├─ DevDesk.Migrations One-shot job — applies the numbered SQL scripts (tickets, doc_chunks) with DbUp
 ├─ DevDesk.Ingestion  One-shot job — docs/ → Azurite blobs → chunk → embed → doc_chunks
 ├─ DevUI              In-process aggregator (no container) — agent conversation/tool-call debugging
 ├─ Postgres 18 (pgvector image)     — tickets + doc_chunks
 ├─ Azure Storage emulator (Azurite) — raw runbook docs before ingestion
 ├─ Ollama (community toolkit)       — local chat + embedding models
 └─ grafana-lgtm container           — Collector + Prometheus + Loki + Tempo + Grafana, one image
```

Every one of these is a native Aspire resource, added in `DevDesk.AppHost/AppHost.cs`. `aspire start` is the only setup command — it starts the database, storage emulator, local models, every service, and the full observability stack together.

**Chat flow**: the browser streams AG-UI events from `/agui`. `/agui` is served by `DevDesk.Server` itself (in dev, Vite proxies `/api` and `/agui` to it). The agent it hosts calls tools on `DevDesk.Mcp`; the tools query Postgres directly with Dapper (`LookupTicket` and `SearchTickets` read `tickets`; `SearchRunbooks` embeds the query with Ollama and searches `doc_chunks` in pgvector). `DevDesk.Mcp` never calls `DevDesk.Server`.

**Agent design**: the agent is a Microsoft Agent Framework **workflow** (`SupportWorkflow`), hosted as a single `AIAgent` so AG-UI, DevUI and the frontend see one agent. `classify` routes each message: a research question ("how do we fix this", "has this come up before") always runs `research`, which calls `search_tickets` and `search_runbooks` in parallel and passes both results to the synthesis agent; a message containing a ticket id goes to an agent that has only `lookup_ticket`; anything else goes to an agent with no tools. That way a small local model never has to choose tools, and greetings can't trigger one. The agent doesn't define its tools inline. It discovers `LookupTicket`, `SearchRunbooks` and `SearchTickets` from `DevDesk.Mcp` — that's what "grouping the tools on an MCP server" means here. The chat lives in `DevDesk.Server/Features/Chat` (there is no separate agent service).

**Observability**: MAF and Microsoft.Extensions.AI instrument themselves with OpenTelemetry, and `ServiceDefaults` exports every project's traces, metrics and logs to both the Aspire Dashboard and `grafana-lgtm` (the AppHost passes each service `GRAFANA_OTLP_ENDPOINT`). The `grafana-lgtm` container is a single image that bundles the Collector, Prometheus (metrics), Loki (logs), Tempo (traces), and Grafana — no hand-written collector config. Use the Aspire Dashboard for day-to-day dev-loop debugging and Grafana for a production-shaped view; they're not redundant, they're different altitudes. DevUI adds a third altitude: stepping through one agent conversation's tool calls specifically.

## Folder structure

```
DevDesk/
├── Directory.Build.props         # shared project settings (net10.0, nullable, implicit usings, analyzers)
├── Directory.Packages.props      # every NuGet version, centrally; project files carry no versions
├── .editorconfig                 # formatting basics + dead-code analyzers (unused usings/members) as warnings
├── DevDesk.AppHost/              # orchestrator — AppHost.cs plus ResourceNames.cs, ContainerImages.cs,
│                                  # ModelNames.cs, Topology.cs constants, no other logic
├── DevDesk.ServiceDefaults/      # shared OpenTelemetry, health checks, service discovery, resilience
├── DevDesk.Server.Core/          # domain logic — zero I/O references, ever
│   └── Features/
│       └── Tickets/
│           └── Core.cs           # Ticket (closed hierarchy: Open/InProgress/Resolved/Closed) + TicketRules:
│                                  # each transition takes exactly the state it applies to; outcome types only
│                                  # where a rule needs more than the state (AgentIsRequester, ReopenWindowElapsed)
├── DevDesk.Server.Core.Tests/    # unit tests against DevDesk.Server.Core, zero mocks, zero DB
├── DevDesk.Server/               # Minimal API
│   ├── Common/                   # validation filter shared by endpoints
│   └── Features/
│       ├── Tickets/              # one file per use case: request, validator, endpoint, handler, SQL,
│       │                         # and a file-scoped Db*Row DTO; plus TicketsEndpoints.cs (route group)
│       └── Chat/                 # hosting: SupportChat (AG-UI + DevUI endpoints), McpConnection, StableMessageIdChatClient
│           └── Agent/            # what the agent is: SupportWorkflow (the graph), SupportAgents (the LLM agents and their
│                                 # instructions), the executors, SupportTools (MCP tool names), Question (routing types)
├── DevDesk.Mcp/                  # MCP server, flat (no Features/ folders): one file per tool, TicketRow.cs,
│                                  # tools query Postgres directly with Dapper
├── DevDesk.Migrations/           # one-shot job: DbUp applies Scripts/NNNN_*.sql (embedded resources), in name order
├── DevDesk.Ingestion/            # one-shot job: uploads docs/ to Azurite, chunks, embeds, writes doc_chunks
├── frontend/                     # React + Vite + shadcn/ui
│   ├── openapi/                  # committed OpenAPI document the API client is generated from
│   └── src/{api,features,components,lib}   # api/ is generated; features/ hold hooks + components
└── docs/                         # sample runbooks, ingested into pgvector via Azurite
```

`Core.cs` holds domain rules only; parsing or shaping request input for a query is shell work, not Core. Transitions take the exact source state (`Resolve(InProgressTicket)`), so an illegal call doesn't compile; the handler narrows the loaded row (`is InProgressTicket`) and answers 409 otherwise. Don't add a runtime state guard inside Core.

New backend features put pure decisions in `Features/{FeatureName}/Core.cs` in `DevDesk.Server.Core` and everything with I/O in one use-case file per endpoint under `DevDesk.Server/Features/{FeatureName}/`, following `emc-fcis-feature-slice`. Don't add `Data` or `Mapper` classes, and don't put I/O in `Core.cs`.

## Gotchas

Things that cost time once and aren't obvious from the code:

- **Postgres 18 data volume**: `WithDataVolume()` picks the container data directory from the image tag at call time, and the pgvector tag doesn't parse as 18+. Call it *before* `WithImage(...)`.
- **Slow local models**: `ServiceDefaults` raises the standard resilience timeouts (3 min per attempt, 6 min total); the ~30s defaults cancelled Ollama chat calls mid-generation.
- **Chat model**: `ModelNames.Chat` (`granite4:micro`, Apache-2.0, ~2 GB) replaced `llama3.2`, which answered from the search results worse and routed some research questions wrongly. Skip reasoning models such as `qwen3`: their "thinking" tokens stream into the chat and are slow on CPU. After changing the model or the classifier prompt, ask a research, a lookup and a greeting question.
- **MCP endpoint**: the MCP client transport needs a concrete `http(s)` URL and rejects Aspire's `https+http://` scheme, so `McpConnection` in `DevDesk.Server` reads `services:mcp:*` configuration.
- **AG-UI nulls**: the Agent Framework hosting package loses the AG-UI SDK's omit-nulls rule on net10, and the TypeScript client rejects `null` fields. `SupportChat` inserts `AGUIJsonUtilities.DefaultTypeInfoResolver` into the JSON options; keep it (it only answers for AG-UI types, so the ticket API's JSON is unaffected).
- **One message per model turn**: OllamaSharp streaming updates have no message id, so `StableMessageIdChatClient` stamps one; without it every token becomes its own AG-UI message.
- **Startup order**: `migrations` runs first and `server`, `mcp` and `ingestion` use `WaitForCompletion(migrations)`, so no service creates or alters tables itself. `server` also waits for `mcp` (it discovers the tools at startup); `mcp` must not wait for `server`, which would deadlock.
- **Migrations**: change the schema only by adding a new numbered script under `DevDesk.Migrations/Scripts` (never edit one that has been applied). The two baseline scripts use `if not exists` so databases created before migrations existed adopt them.
- **Prerelease hosting packages**: the Agent Framework core (`Microsoft.Agents.AI`, Workflows) is GA, but `Microsoft.Agents.AI.Hosting`, `.Hosting.AGUI.AspNetCore`, `.Hosting.OpenAI` (alpha) and `.DevUI` (AppHost) only ship prerelease versions. They now live in `DevDesk.Server`, so bump them deliberately and re-check the chat (a TypeScript AG-UI client run against `/agui`) and the ticket API.
- **OpenAPI stays ticket-only**: the Server's OpenAPI document is filtered to `api/*`, otherwise `/agui` and DevUI's `/v1/*` endpoints leak into the generated frontend client.
- **DevUI**: the AppHost needs `Microsoft.Agents.AI.DevUI` directly (it holds the web UI; without it `/devui/` is 404). The agent name in `DevDesk.Server/Features/Chat/SupportChat.cs` must match `ResourceNames.SupportAgent`.
- **No `"use client"` in the frontend**: this is a Vite SPA, so the Next.js directive does nothing. The shadcn and AI Elements registries still ship it (even with `"rsc": false`), so strip the first line from any file `shadcn add` brings in.
- **Workflow hosted as an agent**: `Workflow.AsAIAgent` requires the start executor to accept the chat protocol (`List<ChatMessage>` + `TurnToken`), so `classify` derives from `ChatProtocolExecutor`. Every executor must declare the message types it sends (`[SendsMessage]` on `Executor<T>`; `ConfigureProtocol(...).SendsMessage<T>()` on `ChatProtocolExecutor`, where the attribute is ignored), otherwise the run fails with "cannot send messages of type …"; an agent step only runs after it receives a `TurnToken`. Errors inside the workflow reach the client only as a generic `RUN_ERROR`; pass `includeExceptionDetails: true` to `AsAIAgent` temporarily to see the cause.
- **Tool calls from the research step**: it calls MCP itself, not through a chat client, so no tool-call events exist unless `ResearchExecutor` reports them (`AgentResponseUpdateEvent` with `FunctionCallContent`/`FunctionResultContent`); that is what feeds the chat UI's "Searching tickets…" labels.
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
3. **Tools live in `DevDesk.Mcp`, not inline in the agent.** The agent (in `DevDesk.Server/Features/Chat`) discovers tools; it doesn't define them.
4. **Schema changes only through `DevDesk.Migrations`.** Services never run `create`/`alter` statements themselves.

## Git workflow

- Commit only when explicitly asked, and only after the user has approved the specific change.
- Use [Conventional Commits](https://www.conventionalcommits.org/) for every commit message (`feat:`, `fix:`, `chore:`, `docs:`, etc.).

## Working guidelines

- **No browser end-to-end tests without approval.** Don't drive the app with `playwright-cli` (or any other browser automation) unless the user explicitly says to. Until then, make frontend changes and verify them with the type check, lint, and build only.
- **SQL: one column per line.** In every query, put each `select` column, each `insert` column and value, and each `update … set` assignment on its own line.
- **Comments explain why, not what.** Keep a comment only when the code can't show the reason (a workaround, a gotcha, a business rule); the code should be enough to follow the logic.
- **Format C# after every change.** Whenever you change a `.cs` file or a related C# file, run `dotnet csharpier format .` from the repo root before finishing.
- **Format React after every change.** Whenever you change frontend code, run `npm run format` (Prettier) in `frontend/` before finishing. Generated and vendored files (`src/api`, `src/components/ui`, `src/components/ai-elements`, `src/index.css`, `src/lib/utils.ts`) are excluded in `frontend/.prettierignore`; don't reformat them by hand.

## When something isn't covered here

Flag the gap and propose an approach rather than guessing silently — especially anything that would introduce a new container, a new orchestration mechanism, or a change to the `Core`/handler split described above.
