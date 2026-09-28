namespace DevDesk.AppHost;

public static class Topology
{
    public const string HealthCheckPath = "/health";
    public const string ApiDocsPath = "/scalar";
    public const string HttpEndpointName = "http";

    public const string DocsDirectory = "../docs";

    // The environment-variable form of the Ingestion:DocsPath configuration key.
    public const string DocsPathEnvironmentVariable = "Ingestion__DocsPath";

    // Read by DevDesk.ServiceDefaults to also export telemetry to grafana-lgtm.
    public const string GrafanaOtlpEnvironmentVariable = "GRAFANA_OTLP_ENDPOINT";

    public const string GrafanaEndpointName = "grafana-ui";
    public const int GrafanaPort = 3000;

    public const string OtlpGrpcEndpointName = "otlp-grpc";
    public const int OtlpGrpcPort = 4317;

    public const string OtlpHttpEndpointName = "otlp-http";
    public const int OtlpHttpPort = 4318;
}
