using AGUI.Abstractions;
using DevDesk.Agent;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

// Must match the name the AppHost declares for DevUI (ResourceNames.SupportAgent).
const string SupportAgentName = "devdesk-support-agent";

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddOllamaApiClient("ollama-llama3-2").AddChatClient();

builder.Services.AddAGUIServer();

// The hosting package composes the AG-UI JSON context into ASP.NET's options in a way that drops the
// SDK's omit-nulls rule, so events carry "timestamp":null etc. and spec-conformant clients (the
// TypeScript AG-UI client) reject them. AGUIJsonUtilities.DefaultTypeInfoResolver carries the rule.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        AGUIJsonUtilities.DefaultTypeInfoResolver
    )
);

// Discover LookupTicket + SearchRunbooks from DevDesk.Mcp - the agent doesn't define tools itself.
// This happens before the host is built because AddAIAgent registers the agent in DI (which DevUI
// and the OpenAI-compatible endpoints resolve by name).
// HttpClientTransportOptions.Endpoint requires a concrete http(s) URI (it rejects Aspire's
// "https+http://" service-discovery pseudo-scheme), so use the real URL Aspire injected via
// WithReference(mcp) instead of handing it the pseudo-scheme directly.
var mcpBaseUrl =
    builder.Configuration["services:mcp:https:0"]
    ?? builder.Configuration["services:mcp:http:0"]
    ?? throw new InvalidOperationException("Missing service discovery configuration for 'mcp'.");

using var mcpHttpClient = new HttpClient();
await using var mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(
        new HttpClientTransportOptions { Endpoint = new Uri(mcpBaseUrl), Name = "devdesk-mcp" },
        mcpHttpClient
    )
);

var mcpTools = await mcpClient.ListToolsAsync();

builder.AddAIAgent(
    SupportAgentName,
    (services, _) =>
        new StableMessageIdChatClient(services.GetRequiredService<IChatClient>()).AsAIAgent(
            name: SupportAgentName,
            instructions: """
            You are a support-desk assistant for DevDesk. Use lookup_ticket to answer questions
            about a specific ticket's status. Use search_runbooks to answer "how do we fix this"
            or "has this come up before" questions. Combine both when a question needs it.
            """,
            tools: [.. mcpTools.Cast<AITool>()]
        )
);

// OpenAI Responses/Conversations endpoints: what the DevUI aggregator in the AppHost talks to.
builder.Services.AddOpenAIResponses();
builder.Services.AddOpenAIConversations();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapAGUIServer("/", app.Services.GetRequiredKeyedService<AIAgent>(SupportAgentName));
app.MapOpenAIResponses();
app.MapOpenAIConversations();

app.Run();
