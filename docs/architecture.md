# Architecture

This is a design reference, not a contributor guide — conventions, gotchas and folder layout
rules live in [AGENTS.md](../AGENTS.md). This document explains *why* the system is shaped the
way it is, with most of its depth on the Agent, since that's where the interesting design
decisions are.

## System overview

```
DevDesk.AppHost (orchestrator)
 ├─ frontend           React SPA — tickets panel + chat UI (AG-UI stream)
 ├─ DevDesk.Server     Minimal API: ticket CRUD, and hosts the support agent
 ├─ DevDesk.Mcp        MCP server — LookupTicket, SearchTickets, SearchRunbooks tools
 ├─ DevDesk.Migrations One-shot: applies SQL schema with DbUp
 ├─ DevDesk.Ingestion  One-shot: runbooks/ → Azurite → chunk → embed → doc_chunks
 ├─ Postgres (pgvector) — tickets + doc_chunks
 ├─ Azurite            — raw runbook files before ingestion
 └─ Ollama             — local chat + embedding models
```

Two independent capabilities share one Postgres instance: **ticket CRUD** (`DevDesk.Server`'s
`Features/Tickets`, a conventional FCIS slice — see AGENTS.md) and **the agent** (this document).
They meet only at the database and at `DevDesk.Mcp`, which reads both `tickets` and `doc_chunks`.
Nothing about the agent is ticket-CRUD-specific beyond the tools it's given.

`DevDesk.Mcp` never calls `DevDesk.Server`. `DevDesk.Server` is the only caller of `DevDesk.Mcp`;
it discovers the three tools over MCP rather than declaring them itself, so adding or changing a
tool never touches the agent's code.

## The Agent

Code lives in `DevDesk.Server/Features/Chat`. There is no separate agent service or project —
Agent Framework's hosting packages are all part of `DevDesk.Server` (see AGENTS.md's "Prerelease
hosting packages" gotcha for why that's still a deliberate choice, not a shortcut).

```
Features/Chat/
├── SupportChat.cs              hosting: wires the chat client, AG-UI/OpenAI endpoints, builds the workflow
├── McpConnection.cs            connects to DevDesk.Mcp once at startup, exposes the client + discovered tools
├── StableMessageIdChatClient.cs chat-client decorator (see Gotchas)
└── Agent/
    ├── SupportWorkflow.cs       wires everything below into one Microsoft.Agents.AI.Workflows graph
    ├── Question.cs              QuestionKind, RoutedQuestion, and the LatestUserText() helper
    ├── ClassifyQuestionExecutor.cs  entry point: decides which branch handles the message
    ├── HandOffExecutor.cs       plain pass-through from a route to an agent step
    ├── ResearchExecutor.cs      calls MCP tools directly (not an AIAgent)
    └── SupportAgents.cs         factory for the three LLM-backed agents and their instructions
```

The split follows one rule: **hosting** (how the agent is exposed — AG-UI, DevUI, OpenAI-compatible
endpoints, DI wiring) stays outside `Agent/`; **what the agent actually does** (the graph, the
routing decision, the agents, the tool calls) stays inside it. Nothing under `Agent/` references
`WebApplication`, `IEndpointRouteBuilder`, or any hosting type — it only depends on
`Microsoft.Agents.AI(.Workflows)` and `Microsoft.Extensions.AI`. That means the workflow itself
could be hosted a different way (a queue trigger, a console harness for testing) without touching
anything in `Agent/`.

### The workflow graph

```
              ChatMessage list + TurnToken
                        │
                        ▼
                  ┌───────────┐
                  │ classify  │  regex ticket-id check, else one LLM call (research / conversation)
                  └─────┬─────┘
        ┌───────────────┼────────────────────┐
   Research         TicketLookup          Conversation
        │                │                    │
        ▼                ▼                    ▼
  ┌───────────┐   ┌─────────────────┐   ┌─────────────────┐
  │ research  │   │ hand-off-to-    │   │ hand-off-to-    │
  │ (direct   │   │ lookup          │   │ conversation    │
  │ MCP calls)│   └────────┬────────┘   └────────┬────────┘
  └─────┬─────┘            ▼                     ▼
        │             ┌─────────┐          ┌──────────────┐
        │             │ lookup  │          │ conversation │
        │             │ (AIAgent,          │ (AIAgent,    │
        │             │  lookup_ticket     │  no tools)   │
        │             │  only)             └──────────────┘
        │             └─────────┘
        ▼
  ┌───────────┐
  │ synthesis │  (AIAgent, no tools — answers only from research's findings)
  └───────────┘
```

`classify`, `research` and the two `hand-off-to-*` steps are `Executor`s: plain C# with no LLM
call of their own except where noted. `lookup`, `conversation` and `synthesis` are `AIAgent`s
bound into the graph. `SupportWorkflow.Build` is the only place that assembles this shape; reading
it top to bottom is reading the whole routing policy.

**Why `classify` isn't one LLM call for everything.** A ticket id in the message is detected with
a regex, not asked of the model — it's a `Guid`, unambiguous, and cheap to check before spending a
model call. Only the research/conversation distinction goes to the LLM, and even then with a
single-word answer at `Temperature = 0`. `granite4:micro` is small and runs on CPU; the fewer
decisions it makes per turn, the more reliably it makes them (see AGENTS.md's "Chat model"
gotcha).

**Why `research` isn't a tool-calling `AIAgent`.** A research question always needs both
`search_tickets` and `search_runbooks` — there's no case where only one answers it, so there's
nothing for a model to decide. `ResearchExecutor` calls both tools directly through the raw
`McpClient`, in parallel, and reports each call as an `AgentResponseUpdateEvent` itself (that's
the only reason it touches `FunctionCallContent`/`FunctionResultContent` — to keep the chat UI's
"Searching tickets…" indicator working). Letting a tool-calling agent choose was tried first and
was unreliable on this model: it would sometimes call only one tool, or none. Removing the choice
removed the failure mode.

**Why `lookup` and `conversation` route through a `HandOffExecutor` instead of `classify` calling
them directly.** `classify` derives from `ChatProtocolExecutor`, which only emits the message
types it's configured to send; an `AIAgent` bound into the graph needs the chat protocol
(`List<ChatMessage>` + `TurnToken`) as its input, so something has to re-wrap `RoutedQuestion`
back into that shape before an edge can reach an agent step. `HandOffExecutor` is that adapter,
parameterized by executor id so one class serves both routes.

**Why the tools aren't named constants.** `"lookup_ticket"`, `"search_tickets"` and
`"search_runbooks"` each appear as a literal exactly once in `DevDesk.Mcp` (at the tool's own
attribute) and once in `DevDesk.Server` (where the agent looks the tool up by name). A shared
constant wouldn't remove either literal — the two projects only ever agree over the network at
runtime, never at compile time — so it would just add a file to open without solving anything.

### Composition root

`DevDesk.Server/Program.cs` builds the one `McpConnection` at startup (`await McpConnection
.ConnectAsync(...)`, before `builder.Build()`), then hands it to `AddSupportChat`. There's no
DI-container indirection for this: the connection is a single instance the process needs once, so
it's created explicitly where every other top-level wiring decision is made, not resolved lazily
from a container.

`AddSupportChat` (in `SupportChat.cs`) does the rest: registers the Ollama chat client, adds the
AG-UI and OpenAI-Responses hosting packages, and registers the built workflow as a single named
`AIAgent` (`builder.AddAIAgent(AgentName, ...)`) — the same instance is what AG-UI, DevUI and the
frontend all see, because "the agent" is one workflow wearing one name, not multiple endpoints
that happen to agree.

## Extending it

- **A new tool**: add it to `DevDesk.Mcp` (flat, one file per tool); reference it by its literal
  name from whichever agent or executor needs it, the same way the three existing tools are
  referenced. No change to `SupportWorkflow` unless the new tool needs a new routing branch.
- **A new routing branch**: add a `QuestionKind`, teach `classify` to produce it, and add an edge
  in `SupportWorkflow.Build` — either straight to a new `AIAgent` from `SupportAgents`, or through
  a new `Executor` if the step needs code, not just a model call.
- **Using the agent outside chat** (e.g. triggering it from ticket creation): nothing under
  `Agent/` depends on `Features/Chat`'s hosting code, so `SupportWorkflow.Build` and
  `SupportAgents` are already callable from another feature. This hasn't been needed yet — do it
  when a concrete feature asks for it, not ahead of time.

## Design review (2026-09-28)

Reviewed the current `Features/Chat` structure while writing this document, specifically for
whether it's the most organized shape for adding more AI features later. Conclusion: no
restructuring — the hosting/`Agent` split, the executor/agent distinction, and the lack of shared
constants for cross-process tool names are all deliberate and still hold up. The one thing worth
flagging (not changing) is the "using it outside chat" note above: today everything is reachable
only through `Features/Chat`, which is correct for what exists now, but the natural seam if a
second feature needs it is to promote `Agent/` to a sibling of `Chat/` rather than reach across
feature folders.
