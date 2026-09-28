using DevDesk.ServiceDefaults;
using ModelContextProtocol.Client;

namespace DevDesk.Server.Features.Chat;

public sealed class McpConnection : IAsyncDisposable
{
    private const string ClientName = "devdesk-mcp";

    private readonly HttpClient httpClient;

    private McpConnection(HttpClient httpClient, McpClient client, IList<McpClientTool> tools)
    {
        this.httpClient = httpClient;
        Client = client;
        Tools = tools;
    }

    public McpClient Client { get; }
    public IList<McpClientTool> Tools { get; }

    public static async Task<McpConnection> ConnectAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default
    )
    {
        // The MCP transport rejects Aspire's "https+http://" pseudo-scheme, so use the concrete URL
        // that WithReference(mcp) injects.
        var url =
            configuration[$"services:{ServiceResources.Mcp}:https:0"]
            ?? configuration[$"services:{ServiceResources.Mcp}:http:0"]
            ?? throw new InvalidOperationException(
                $"Missing service discovery configuration for '{ServiceResources.Mcp}'."
            );

        var httpClient = new HttpClient();
        var client = await McpClient.CreateAsync(
            new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri(url), Name = ClientName },
                httpClient
            ),
            cancellationToken: cancellationToken
        );
        var tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

        return new McpConnection(httpClient, client, tools);
    }

    public async ValueTask DisposeAsync()
    {
        await Client.DisposeAsync();
        httpClient.Dispose();
    }
}
