using DevDesk.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// WithDataVolume() picks the data directory from the image tag when it is called, and the pgvector
// tag doesn't parse as Postgres 18+, so call it before WithImage().
var postgres = builder
    .AddPostgres(ResourceNames.Postgres)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithImage(ContainerImages.PgVector, ContainerImages.PgVectorTag);

var database = postgres.AddDatabase(ResourceNames.Database);

var storage = builder
    .AddAzureStorage(ResourceNames.Storage)
    .RunAsEmulator(azurite => azurite.WithLifetime(ContainerLifetime.Persistent).WithDataVolume());

var runbooks = storage.AddBlobs(ResourceNames.RunbooksBlobs);

var ollama = builder
    .AddOllama(ResourceNames.Ollama)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var chatModel = ollama.AddModel(ModelNames.Chat);
var embeddingModel = ollama.AddModel(ModelNames.Embedding);

var observability = builder
    .AddContainer(
        ResourceNames.Observability,
        ContainerImages.GrafanaLgtm,
        ContainerImages.GrafanaLgtmTag
    )
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(
        port: Topology.GrafanaPort,
        targetPort: Topology.GrafanaPort,
        name: Topology.GrafanaEndpointName,
        scheme: "http"
    )
    .WithEndpoint(
        targetPort: Topology.OtlpGrpcPort,
        name: Topology.OtlpGrpcEndpointName,
        scheme: "http"
    )
    .WithEndpoint(
        targetPort: Topology.OtlpHttpPort,
        name: Topology.OtlpHttpEndpointName,
        scheme: "http"
    );

var grafanaOtlp = observability.GetEndpoint(Topology.OtlpGrpcEndpointName);

var server = builder
    .AddProject<Projects.DevDesk_Server>(ResourceNames.Server)
    .WithEnvironment(Topology.GrafanaOtlpEnvironmentVariable, grafanaOtlp)
    .WithReference(database)
    .WaitFor(database)
    .WithReference(chatModel)
    .WaitFor(chatModel)
    .WithHttpHealthCheck(Topology.HealthCheckPath)
    .WithExternalHttpEndpoints();

// No WaitFor(server): only the server-hosted agent calls mcp, after it has created the tickets table.
#pragma warning disable ASPIREMCP001
var mcp = builder
    .AddProject<Projects.DevDesk_Mcp>(ResourceNames.Mcp)
    .WithEnvironment(Topology.GrafanaOtlpEnvironmentVariable, grafanaOtlp)
    .WithReference(database)
    .WaitFor(database)
    .WithReference(embeddingModel)
    .WaitFor(embeddingModel)
    .WithHttpHealthCheck(Topology.HealthCheckPath)
    .WithMcpServer();
#pragma warning restore ASPIREMCP001

server.WithReference(mcp).WaitFor(mcp);

_ = builder
    .AddDevUI(ResourceNames.DevUI)
    .WithAgentService(server, agents: [new(ResourceNames.SupportAgent)])
    .WaitFor(server);

// Waits for mcp because DevDesk.Mcp creates the doc_chunks table.
var ingestion = builder
    .AddProject<Projects.DevDesk_Ingestion>(ResourceNames.Ingestion)
    .WithEnvironment(Topology.GrafanaOtlpEnvironmentVariable, grafanaOtlp)
    .WithReference(database)
    .WithReference(runbooks)
    .WithReference(embeddingModel)
    .WaitFor(mcp)
    .WaitFor(runbooks)
    .WaitFor(embeddingModel)
    .WithEnvironment(
        Topology.DocsPathEnvironmentVariable,
        Path.GetFullPath(Topology.DocsDirectory, builder.AppHostDirectory)
    );

var webfrontend = builder
    .AddViteApp(ResourceNames.WebFrontend, "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
