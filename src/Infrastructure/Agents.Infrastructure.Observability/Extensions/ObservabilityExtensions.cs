using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Agents.Infrastructure.Observability.Extensions;

public static class ObservabilityExtensions
{
    /// <summary>
    /// Adds comprehensive observability (metrics, tracing, logging) to the application
    /// </summary>
    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        // Add Prometheus metrics
        services.AddSingleton(_ => Prometheus.Metrics.DefaultRegistry);

        // Add OpenTelemetry tracing
        services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(serviceName)
                        .AddTelemetrySdk()
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = configuration["ASPNETCORE_ENVIRONMENT"] ?? "Production"
                        }))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = context =>
                        {
                            // Don't trace health check and metrics endpoints
                            var path = context.Request.Path.ToString();
                            return !path.Contains("/health") && !path.Contains("/metrics");
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddSource(serviceName);

                // Add console exporter for development
                var environment = configuration["ASPNETCORE_ENVIRONMENT"];
                if (environment == "Development")
                {
                    tracerProviderBuilder.AddConsoleExporter();
                }

                // Add Azure Monitor/Application Insights exporter in production
                var appInsightsConnectionString = configuration["ApplicationInsights:ConnectionString"];
                if (!string.IsNullOrEmpty(appInsightsConnectionString))
                {
                    // Note: You would add Azure Monitor exporter here
                    // tracerProviderBuilder.AddAzureMonitorTraceExporter(options =>
                    // {
                    //     options.ConnectionString = appInsightsConnectionString;
                    // });
                }
            });

        return services;
    }

    /// <summary>
    /// Configures Serilog structured logging
    /// </summary>
    public static IHostBuilder AddStructuredLogging(
        this IHostBuilder hostBuilder,
        string serviceName)
    {
        return hostBuilder.UseSerilog((context, services, configuration) =>
        {
            var logLevel = context.Configuration["Logging:LogLevel:Default"];
            var minimumLevel = Enum.TryParse<LogEventLevel>(logLevel, out var level)
                ? level
                : LogEventLevel.Information;

            configuration
                .MinimumLevel.Is(minimumLevel)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", serviceName)
                .Enrich.WithProperty("Environment", context.HostingEnvironment.EnvironmentName)
                .Enrich.WithMachineName()
                .Enrich.WithThreadId();

            // Console sink with JSON formatting
            if (context.HostingEnvironment.IsDevelopment())
            {
                configuration.WriteTo.Console();
            }
            else
            {
                configuration.WriteTo.Console(new CompactJsonFormatter());
            }

            // Application Insights sink
            var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
            if (!string.IsNullOrEmpty(appInsightsConnectionString))
            {
                configuration.WriteTo.ApplicationInsights(
                    appInsightsConnectionString,
                    TelemetryConverter.Traces,
                    minimumLevel);
            }

            // File sink for persistent logs
            configuration.WriteTo.File(
                new CompactJsonFormatter(),
                path: $"logs/{serviceName}-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                restrictedToMinimumLevel: LogEventLevel.Information);
        });
    }

    /// <summary>
    /// Configures observability middleware in the request pipeline
    /// </summary>
    public static IApplicationBuilder UseObservability(
        this IApplicationBuilder app)
    {
        // Add request logging middleware
        app.UseSerilogRequestLogging(options =>
        {
            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
                diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
                diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
                
                if (httpContext.User.Identity?.IsAuthenticated == true)
                {
                    diagnosticContext.Set("UserName", httpContext.User.Identity.Name);
                }
            };

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
                if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
                if (elapsed > 1000) return LogEventLevel.Warning;
                return LogEventLevel.Information;
            };
        });

        // Add Prometheus metrics endpoint
        app.UseRouting();
        app.UseHttpMetrics(); // Track HTTP metrics
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapMetrics(); // Expose /metrics endpoint
        });

        return app;
    }
}
