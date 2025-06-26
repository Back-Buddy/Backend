using Azure.Monitor.OpenTelemetry.AspNetCore;
using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

using ServiceDefaults;

namespace ServiceDefaults;

public static class OpenTelemetry
{
    public static void AddOpenTelemetry<TBuilder>(this TBuilder builder, string serviceName) where TBuilder : IHostApplicationBuilder
    {
        // No OpenTelemetry in development mode
        if (builder.Environment.IsDevelopment()) return;

        builder.Services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSource(serviceName)
                    .AddAzureMonitorTraceExporter();

            })
            .WithMetrics(builder =>
            {
                builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddAzureMonitorMetricExporter();
            })
            .UseAzureMonitor();

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeScopes = true;
            logging.ParseStateValues = true;
            logging.IncludeFormattedMessage = true;
            logging.SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService(serviceName))
                .AddAzureMonitorLogExporter();
        });
    }
}