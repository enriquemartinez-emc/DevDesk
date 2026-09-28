using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Microsoft.Extensions.Hosting;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/aspire/service-defaults
public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    // Set by the AppHost (Topology.GrafanaOtlpEnvironmentVariable) to the grafana-lgtm OTLP gRPC endpoint.
    private const string GrafanaOtlpEndpointKey = "GRAFANA_OTLP_ENDPOINT";
    private const string GrafanaExporterName = "grafana";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.ConfigureOpenTelemetry();

        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();

        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on resilience by default. The default ~30s attempt timeout is too short for
            // local LLM inference (Ollama on CPU) - raise it so the support chat's calls in DevDesk.Server don't
            // get cancelled mid-generation. SamplingDuration must be >= 2x AttemptTimeout.
            http.AddStandardResilienceHandler(options =>
            {
                options.AttemptTimeout.Timeout = TimeSpan.FromMinutes(3);
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(6);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(6);
            });

            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        // Uncomment the following to restrict the allowed schemes for service discovery.
        // builder.Services.Configure<ServiceDiscoveryOptions>(options =>
        // {
        //     options.AllowedSchemes = ["https"];
        // });

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
                    // Chat-client and agent metrics (names carry an "Experimental." prefix in some versions).
                    .AddMeter("*Microsoft.Extensions.AI*", "*Microsoft.Agents.AI*");
            })
            .WithTracing(tracing =>
            {
                tracing
                    .AddSource(builder.Environment.ApplicationName)
                    // Chat-client and agent spans (names carry an "Experimental." prefix in some versions).
                    .AddSource("*Microsoft.Extensions.AI*", "*Microsoft.Agents.AI*")
                    .AddAspNetCoreInstrumentation(tracing =>
                        // Exclude health check requests from tracing
                        tracing.Filter = context =>
                            !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                            && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath)
                    )
                    // Uncomment the following line to enable gRPC instrumentation (requires the OpenTelemetry.Instrumentation.GrpcNetClient package)
                    //.AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation();
            });

        builder.AddOpenTelemetryExporters();

        return builder;
    }

    private static TBuilder AddOpenTelemetryExporters<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        var useOtlpExporter = !string.IsNullOrWhiteSpace(
            builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]
        );

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

        // Uncomment the following lines to enable the Azure Monitor exporter (requires the Azure.Monitor.OpenTelemetry.AspNetCore package)
        //if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
        //{
        //    builder.Services.AddOpenTelemetry()
        //       .UseAzureMonitor();
        //}

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder
            .Services.AddHealthChecks()
            // Add a default liveness check to ensure app is responsive
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

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
                new HealthCheckOptions { Predicate = r => r.Tags.Contains("live") }
            );
        }

        return app;
    }
}
