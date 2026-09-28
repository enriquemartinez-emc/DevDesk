# DevDesk

A support-ticket assistant. Support agents ask it things like:

- "What's the status of ticket X?" — answered from structured ticket data in Postgres.
- "How do we reset a customer's 2FA?" — answered from runbooks via vector search (pgvector).
- Questions that need both — the agent has one tool for each.

Everything runs locally: the chat model and embeddings are served by Ollama, and one command starts the whole stack.
Architecture, conventions and known gotchas live in [AGENTS.md](AGENTS.md).

## Prerequisites

- .NET 10 SDK
- [Aspire CLI](https://aspire.dev) 13.5
- Docker (Docker Desktop with WSL integration if you're on WSL)
- Node.js `^20.19.0` or `>=22.12.0`

## Run it

```bash
aspire start        # from the repo root; prints the dashboard URL (with a login token)
aspire stop         # when you're done
```

`aspire start` brings up Postgres (pgvector), Azurite, Ollama, Grafana, every service and the frontend, and runs the
database migrations (`migrations`, which runs first) and then the runbook ingestion job once the embedding model is ready.

**First run is slow.** It pulls the container images and downloads the models (`granite4:micro`, roughly 2 GB, and
`nomic-embed-text`, roughly 270 MB). Data and models persist in Docker volumes, so later starts take a minute or two.
Wait until `mcp`, `server` and `webfrontend` are healthy and `migrations` and `ingestion` show *Finished* in the dashboard
(or run `aspire wait server`).

## Where to look

| What | Where |
| --- | --- |
| Chat UI | the `webfrontend` URL in the Aspire dashboard (its port changes each run) |
| Agent debugging (DevUI) | the `devui` URL in the dashboard, ending in `/devui/` |
| Traces, metrics, logs | the Aspire dashboard, and Grafana at http://localhost:3000 |
| Ticket API | `http://localhost:5480/api/tickets`, OpenAPI at `/openapi/v1.json` |
| API docs (Scalar) | the **API docs (Scalar)** link on `server` in the dashboard, or `http://localhost:5480/scalar` |

## Try it

Click **New ticket** in the UI, or create one through the API:

```bash
curl -X POST http://localhost:5480/api/tickets \
  -H 'Content-Type: application/json' \
  -d '{"subject":"Cannot log in","description":"Locked out after failed 2FA","requesterId":"11111111-1111-1111-1111-111111111111"}'
```

Then, in the chat UI, click **Ask about this ticket** on it and send. Also try *"How do we reset a customer's 2FA?"* —
the agent should search the runbooks in `docs/`.

Answers come from a small local model on CPU, so they can take a while; you'll see a "Thinking…" state and the tool
calls the agent makes.

The ticket lifecycle is `open → in_progress → resolved → closed`, with reopen from resolved or closed. The endpoints are
`POST /api/tickets`, `GET /api/tickets[/{id}]`, and `POST /api/tickets/{id}/{assign|resolve|close|reopen}`.

## Development

```bash
dotnet build DevDesk.sln
dotnet test DevDesk.Server.Core.Tests
dotnet csharpier format .          # run after any C# change (dotnet tool restore first)

cd frontend
npm install
npm run format                     # Prettier; run after any frontend change
npx tsc -b && npm run lint && npm run build
npm run fetch:openapi              # needs SERVER_URL=http://localhost:5480
npm run generate:api               # regenerate the typed client from openapi/devdesk-server.json
```

Add runbooks by dropping Markdown files into `docs/` and restarting; the ingestion job re-embeds them on every start.
