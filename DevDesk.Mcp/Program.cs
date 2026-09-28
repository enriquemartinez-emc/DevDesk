using DevDesk.Mcp;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

builder.AddNpgsqlDataSource("devdeskdb");
builder.AddOllamaApiClient("ollama-nomic-embed-text").AddEmbeddingGenerator();

var app = builder.Build();

await RunbooksSchema.EnsureCreatedAsync(
    app.Services.GetRequiredService<NpgsqlDataSource>(),
    CancellationToken.None
);

app.MapDefaultEndpoints();
app.MapMcp();

app.Run();
