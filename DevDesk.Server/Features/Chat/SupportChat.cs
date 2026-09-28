using AGUI.Abstractions;
using DevDesk.Server.Features.Chat.Agent;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.AGUI.AspNetCore;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace DevDesk.Server.Features.Chat;

public static class SupportChat
{
    // Must match ResourceNames.SupportAgent in the AppHost: DevUI lists agents by this name.
    private const string AgentName = "devdesk-support-agent";

    public static WebApplicationBuilder AddSupportChat(
        this WebApplicationBuilder builder,
        McpConnection mcp
    )
    {
        builder.AddOllamaApiClient("ollama-llama3-2").AddChatClient();

        builder.Services.AddAGUIServer();

        // The hosting package drops the AG-UI SDK's omit-nulls rule, and the TypeScript client rejects
        // null fields. The resolver only answers for AG-UI types, so the ticket API is unaffected.
        builder.Services.ConfigureHttpJsonOptions(options =>
            options.SerializerOptions.TypeInfoResolverChain.Insert(
                0,
                AGUIJsonUtilities.DefaultTypeInfoResolver
            )
        );

        builder.AddAIAgent(
            AgentName,
            (services, _) =>
            {
                var chatClient = new StableMessageIdChatClient(
                    services.GetRequiredService<IChatClient>()
                );
                return SupportWorkflow.Build(chatClient, mcp).AsAIAgent(name: AgentName);
            }
        );

        builder.Services.AddOpenAIResponses();
        builder.Services.AddOpenAIConversations();

        return builder;
    }

    public static WebApplication MapSupportChat(this WebApplication app)
    {
        app.MapAGUIServer("/agui", app.Services.GetRequiredKeyedService<AIAgent>(AgentName));
        app.MapOpenAIResponses();
        app.MapOpenAIConversations();

        return app;
    }
}
