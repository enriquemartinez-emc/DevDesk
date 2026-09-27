using DevDesk.AppHost.Core;

var builder = DistributedApplication.CreateBuilder(args);

// Postgres 18 + pgvector — structured (tickets) and vector (doc_chunks) data in one instance.
var postgres = builder.AddPostgres(ResourceNames.Postgres)
    .WithImage(ContainerImages.PgVector, ContainerImages.PgVectorTag)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var database = postgres.AddDatabase(ResourceNames.Database);

// Azure Storage emulator (Azurite) — raw runbooks before ingestion.
var storage = builder.AddAzureStorage(ResourceNames.Storage)
    .RunAsEmulator(azurite => azurite
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume());

var runbooks = storage.AddBlobs(ResourceNames.RunbooksBlobs);

// Ollama (community toolkit) — local chat + embedding models.
var ollama = builder.AddOllama(ResourceNames.Ollama)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var chatModel = ollama.AddModel(ModelNames.Chat);
var embeddingModel = ollama.AddModel(ModelNames.Embedding);

// grafana-lgtm — Collector + Prometheus (metrics) + Loki (logs) + Tempo (traces) + Grafana, one image.
var observability = builder.AddContainer(ResourceNames.Observability, ContainerImages.GrafanaLgtm, ContainerImages.GrafanaLgtmTag)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEndpoint(port: Topology.GrafanaPort, targetPort: Topology.GrafanaPort, name: Topology.GrafanaEndpointName)
    .WithEndpoint(targetPort: Topology.OtlpGrpcPort, name: Topology.OtlpGrpcEndpointName)
    .WithEndpoint(targetPort: Topology.OtlpHttpPort, name: Topology.OtlpHttpEndpointName);

var server = builder.AddProject<Projects.DevDesk_Server>(ResourceNames.Server)
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck(Topology.HealthCheckPath)
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp(ResourceNames.WebFrontend, "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
