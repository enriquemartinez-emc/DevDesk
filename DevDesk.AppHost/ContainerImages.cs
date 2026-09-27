namespace DevDesk.AppHost.Core;

public static class ContainerImages
{
    // https://hub.docker.com/r/pgvector/pgvector - Postgres 18 built with the pgvector extension.
    public const string PgVector = "pgvector/pgvector";
    public const string PgVectorTag = "0.8.6-pg18";

    // https://hub.docker.com/r/grafana/otel-lgtm - Collector + Prometheus + Loki + Tempo + Grafana in one image.
    public const string GrafanaLgtm = "grafana/otel-lgtm";
    public const string GrafanaLgtmTag = "0.9.1";
}
