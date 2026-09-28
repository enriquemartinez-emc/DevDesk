using DevDesk.Ingestion;
using DevDesk.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddValidatedOptions<IngestionOptions>(IngestionOptions.SectionName);
builder.AddNpgsqlDataSource(ServiceResources.Database);
builder.AddAzureBlobServiceClient("runbooks");
builder.AddOllamaApiClient(ServiceResources.EmbeddingModel).AddEmbeddingGenerator();

builder.Services.AddSingleton<RunbookIngestor>();

using var host = builder.Build();
await host.StartAsync();

await host.Services.GetRequiredService<RunbookIngestor>().RunAsync(CancellationToken.None);

await host.StopAsync();
