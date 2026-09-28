using DevDesk.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Postgres 18 + pgvector — structured (tickets) and vector (doc_chunks) data in one instance.
// WithDataVolume() picks the container's data directory from the image tag at the time it's
// called (Postgres 18 changed layout - see https://github.com/docker-library/postgres/issues/1259).
// Our pgvector tag doesn't parse as "18+", so call it before swapping the image via WithImage().
var postgres = builder
    .AddPostgres(ResourceNames.Postgres)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithImage(ContainerImages.PgVector, ContainerImages.PgVectorTag);

var database = postgres.AddDatabase(ResourceNames.Database);

// Azure Storage emulator (Azurite) — raw runbooks before ingestion.
var storage = builder
    .AddAzureStorage(ResourceNames.Storage)
    .RunAsEmulator(azurite => azurite.WithLifetime(ContainerLifetime.Persistent).WithDataVolume());

var runbooks = storage.AddBlobs(ResourceNames.RunbooksBlobs);

// Ollama (community toolkit) — local chat + embedding models.
var ollama = builder
    .AddOllama(ResourceNames.Ollama)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var chatModel = ollama.AddModel(ModelNames.Chat);
var embeddingModel = ollama.AddModel(ModelNames.Embedding);

// grafana-lgtm — Collector + Prometheus (metrics) + Loki (logs) + Tempo (traces) + Grafana, one image.
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
        name: Topology.GrafanaEndpointName
    )
    .WithEndpoint(targetPort: Topology.OtlpGrpcPort, name: Topology.OtlpGrpcEndpointName)
    .WithEndpoint(targetPort: Topology.OtlpHttpPort, name: Topology.OtlpHttpEndpointName);

var server = builder
    .AddProject<Projects.DevDesk_Server>(ResourceNames.Server)
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck(Topology.HealthCheckPath)
    .WithExternalHttpEndpoints();

// DevDesk.Mcp groups LookupTicket (calls the server's ticket API) and SearchRunbooks
// (pgvector similarity search embedded via the Ollama embedding model) as MCP tools.
#pragma warning disable ASPIREMCP001
var mcp = builder
    .AddProject<Projects.DevDesk_Mcp>(ResourceNames.Mcp)
    .WithReference(database)
    .WaitFor(database)
    .WithReference(embeddingModel)
    .WaitFor(embeddingModel)
    .WithReference(server)
    .WaitFor(server)
    .WithHttpHealthCheck(Topology.HealthCheckPath)
    .WithMcpServer();
#pragma warning restore ASPIREMCP001

// DevDesk.Agent hosts the AIAgent (Microsoft Agent Framework), discovers LookupTicket and
// SearchRunbooks from DevDesk.Mcp as an MCP client, and streams to the frontend via AG-UI.
var agent = builder
    .AddProject<Projects.DevDesk_Agent>(ResourceNames.Agent)
    .WithReference(chatModel)
    .WaitFor(chatModel)
    .WithReference(mcp)
    .WaitFor(mcp)
    .WithHttpHealthCheck(Topology.HealthCheckPath)
    .WithExternalHttpEndpoints();

// The Server forwards /agui to the agent in a published build. Reference only, no WaitFor:
// server -> agent -> mcp -> server would be a startup cycle.
server.WithReference(agent);

// DevUI: an in-process aggregator (no container) for stepping through agent conversations and
// tool calls. It talks to the agent's OpenAI Responses/Conversations endpoints.
_ = builder
    .AddDevUI(ResourceNames.DevUI)
    .WithAgentService(agent, agents: [new(ResourceNames.SupportAgent)])
    .WaitFor(agent);

// DevDesk.Ingestion is a one-shot job: it uploads docs/*.md to blob storage, then chunks, embeds
// and writes them to doc_chunks. It waits for mcp because DevDesk.Mcp creates the doc_chunks table.
var ingestion = builder
    .AddProject<Projects.DevDesk_Ingestion>(ResourceNames.Ingestion)
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
    .WaitFor(server)
    .WithReference(agent)
    .WaitFor(agent);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
