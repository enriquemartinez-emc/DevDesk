using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace DevDesk.Server.Features.Chat;

// The chat is AG-UI over server-sent events from DevDesk.Agent. In dev, Vite proxies /agui straight to
// the agent; in a published build the Server hosts the frontend, so it forwards /agui to the agent.
// The agent stays a separate service - the Server only routes to it.
public static class ChatProxy
{
    private const string RoutePrefix = "/agui";
    private const string AgentClusterId = "agent";

    // Aspire service discovery for the AppHost resource named "agent" (ResourceNames.Agent); it is
    // resolved from the services:agent:* configuration that WithReference(agent) injects.
    private const string AgentAddress = "https+http://agent";

    public static IServiceCollection AddChatProxy(this IServiceCollection services)
    {
        RouteConfig[] routes =
        [
            new()
            {
                RouteId = "agui",
                ClusterId = AgentClusterId,
                Match = new RouteMatch { Path = $"{RoutePrefix}/{{**catch-all}}" },
                Transforms =
                [
                    new Dictionary<string, string> { ["PathRemovePrefix"] = RoutePrefix },
                ],
            },
        ];

        ClusterConfig[] clusters =
        [
            new()
            {
                ClusterId = AgentClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["agent"] = new() { Address = AgentAddress },
                },
                // A local model can go quiet for a while before the first token, so allow a long idle gap.
                HttpRequest = new ForwarderRequestConfig
                {
                    ActivityTimeout = TimeSpan.FromMinutes(5),
                },
            },
        ];

        services
            .AddReverseProxy()
            .LoadFromMemory(routes, clusters)
            .AddServiceDiscoveryDestinationResolver();
        return services;
    }

    public static IEndpointRouteBuilder MapChatProxy(this IEndpointRouteBuilder app)
    {
        app.MapReverseProxy();
        return app;
    }
}
