using DevDesk.Ingestion;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("devdeskdb");
builder.AddAzureBlobServiceClient("runbooks");
builder.AddOllamaApiClient("ollama-nomic-embed-text").AddEmbeddingGenerator();

builder.Services.AddSingleton<RunbookIngestor>();

using var host = builder.Build();
await host.StartAsync();

var docsDirectory =
    builder.Configuration["DOCS_PATH"]
    ?? throw new InvalidOperationException("DOCS_PATH is not configured.");

await host
    .Services.GetRequiredService<RunbookIngestor>()
    .RunAsync(docsDirectory, CancellationToken.None);

await host.StopAsync();
