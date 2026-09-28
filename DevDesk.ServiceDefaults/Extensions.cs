using DevDesk.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Common Aspire services for every service project: service discovery, resilience, health checks and
// OpenTelemetry (exported to the Aspire dashboard and, when configured, to grafana-lgtm).
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    // Set by the AppHost (Topology.GrafanaOtlpEnvironmentVariable).
    private const string GrafanaOtlpEndpointKey = "GRAFANA_OTLP_ENDPOINT";
    private const string GrafanaExporterName = "grafana";
    private const string OtlpEndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";

    private const string SelfCheckName = "self";
    private const string LiveTag = "live";

    // Chat-client and agent telemetry; names carry an "Experimental." prefix in some versions.
    private static readonly string[] AiTelemetryNames =
    [
        "*Microsoft.Extensions.AI*",
        "*Microsoft.Agents.AI*",
    ];

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        var resilience =
            builder.Configuration.GetSection(ResilienceOptions.SectionName).Get<ResilienceOptions>()
            ?? new ResilienceOptions();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = resilience.AttemptTimeout;
                options.TotalRequestTimeout.Timeout = resilience.TotalTimeout;
                // The circuit breaker requires a sampling duration of at least twice the attempt timeout.
                options.CircuitBreaker.SamplingDuration = resilience.AttemptTimeout * 2;
            });

            http.AddServiceDiscovery();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        builder
            .Services.AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter(AiTelemetryNames);
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    .AddSource(AiTelemetryNames)
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(builder.Configuration[OtlpEndpointKey]);

        var grafanaEndpoint = builder.Configuration[GrafanaOtlpEndpointKey];

        if (!string.IsNullOrWhiteSpace(grafanaEndpoint))
        {
            // UseOtlpExporter() takes one destination, so register each signal's exporter explicitly:
            // the Aspire dashboard (from the standard OTEL_* settings) and grafana-lgtm.
            void ConfigureGrafana(OtlpExporterOptions options)
            {
                options.Endpoint = new Uri(grafanaEndpoint);
                options.Protocol = OtlpExportProtocol.Grpc;
                // Don't forward the Aspire dashboard's API key to a different backend.
                options.Headers = null;
            }

            builder.Logging.AddOpenTelemetry(logging =>
            {
                if (useOtlpExporter)
                {
                    logging.AddOtlpExporter();
                }

                logging.AddOtlpExporter(GrafanaExporterName, ConfigureGrafana);
            });

            builder
                .Services.AddOpenTelemetry()
                .WithMetrics(metrics =>
                {
                    if (useOtlpExporter)
                    {
                        metrics.AddOtlpExporter();
                    }

                    metrics.AddOtlpExporter(GrafanaExporterName, ConfigureGrafana);
                })
                .WithTracing(tracing =>
                {
                    if (useOtlpExporter)
                    {
                        tracing.AddOtlpExporter();
                    }

                    tracing.AddOtlpExporter(GrafanaExporterName, ConfigureGrafana);
                });
        }
        else if (useOtlpExporter)
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder
            .Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck(SelfCheckName, () => HealthCheckResult.Healthy(), [LiveTag]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // Adding health checks endpoints to applications in non-development environments has security implications.
        // See https://aka.ms/aspire/healthchecks for details before enabling these endpoints in non-development environments.
        if (app.Environment.IsDevelopment())
        {
            // All health checks must pass for app to be considered ready to accept traffic after starting
            app.MapHealthChecks(HealthEndpointPath);

            // Only health checks tagged with the "live" tag must pass for app to be considered alive
            app.MapHealthChecks(
                AlivenessEndpointPath,
                new HealthCheckOptions { Predicate = r => r.Tags.Contains(LiveTag) }
            );
        }

        return app;
    }
}
