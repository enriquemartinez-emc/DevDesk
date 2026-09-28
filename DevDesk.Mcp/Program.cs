var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

builder.AddNpgsqlDataSource("devdeskdb");
builder.AddOllamaApiClient("ollama-nomic-embed-text").AddEmbeddingGenerator();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapMcp();

app.Run();
