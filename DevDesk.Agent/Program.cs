using AGUI.Abstractions;
using DevDesk.Agent;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddOllamaApiClient("ollama-llama3-2").AddChatClient();

builder.Services.AddAGUIServer();

// The hosting package composes the AG-UI JSON context into ASP.NET's options in a way that drops the
// SDK's omit-nulls rule, so events carry "timestamp":null etc. and spec-conformant clients (the
// TypeScript AG-UI client) reject them. AGUIJsonUtilities.DefaultTypeInfoResolver carries the rule.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AGUIJsonUtilities.DefaultTypeInfoResolver));

var app = builder.Build();

// Discover LookupTicket + SearchRunbooks from DevDesk.Mcp - the agent doesn't define tools itself.
// HttpClientTransportOptions.Endpoint requires a concrete http(s) URI (it rejects Aspire's
// "https+http://" service-discovery pseudo-scheme), so resolve the real URL Aspire already
// injected via WithReference(mcp) instead of handing it the pseudo-scheme directly.
var mcpBaseUrl = app.Configuration["services:mcp:https:0"]
    ?? app.Configuration["services:mcp:http:0"]
    ?? throw new InvalidOperationException("Missing service discovery configuration for 'mcp'.");

var mcpHttpClient = app.Services.GetRequiredService<IHttpClientFactory>().CreateClient();
await using var mcpClient = await McpClient.CreateAsync(new HttpClientTransport(
    new HttpClientTransportOptions
    {
        Endpoint = new Uri(mcpBaseUrl),
        Name = "devdesk-mcp",
    },
    mcpHttpClient));

var mcpTools = await mcpClient.ListToolsAsync();

var chatClient = new StableMessageIdChatClient(app.Services.GetRequiredService<IChatClient>());
AIAgent agent = chatClient.AsAIAgent(
    name: "devdesk-support-agent",
    instructions: """
        You are a support-desk assistant for DevDesk. Use lookup_ticket to answer questions
        about a specific ticket's status. Use search_runbooks to answer "how do we fix this"
        or "has this come up before" questions. Combine both when a question needs it.
        """,
    tools: [.. mcpTools.Cast<AITool>()]);

app.MapDefaultEndpoints();
app.MapAGUIServer("/", agent);

app.Run();
