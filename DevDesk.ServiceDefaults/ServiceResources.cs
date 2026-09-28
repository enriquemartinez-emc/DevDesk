namespace DevDesk.ServiceDefaults;

// Aspire resource names as the services see them. They must match DevDesk.AppHost's ResourceNames.
public static class ServiceResources
{
    public const string Database = "devdeskdb";
    public const string Runbooks = "runbooks";
    public const string ChatModel = "chat-model";
    public const string EmbeddingModel = "embedding-model";
    public const string Mcp = "mcp";
}
