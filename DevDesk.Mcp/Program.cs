using DevDesk.Mcp;
using DevDesk.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder
    .Services.AddMcpServer()
    .WithHttpTransport(options => options.Stateless = true)
    .WithToolsFromAssembly();

builder.Services.AddValidatedOptions<SearchOptions>(SearchOptions.SectionName);
builder.AddNpgsqlDataSource(ServiceResources.Database);
builder.AddOllamaApiClient(ServiceResources.EmbeddingModel).AddEmbeddingGenerator();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapMcp();

app.Run();
