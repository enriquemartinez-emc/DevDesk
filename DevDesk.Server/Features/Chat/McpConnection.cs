using ModelContextProtocol.Client;

namespace DevDesk.Server.Features.Chat;

public sealed class McpConnection : IAsyncDisposable
{
    private const string ClientName = "devdesk-mcp";
    private const string ResourceName = "mcp";

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
        // The MCP transport rejects Aspire's "https+http://" pseudo-scheme, so use the concrete URL WithReference(mcp) injects.
        var url =
            configuration[$"services:{ResourceName}:https:0"]
            ?? configuration[$"services:{ResourceName}:http:0"]
            ?? throw new InvalidOperationException(
                $"Missing service discovery configuration for '{ResourceName}'."
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
