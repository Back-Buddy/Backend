using Azure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace ServiceDefaults;

public static class HealthChecks
{
    private const string READINESSTAG = "ready";
    private const string ALIVETAG = "alive";

    public static void AddDefaultHealthChecks<TBuilder>(this TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddHealthChecksDev();
        }
        else
        {
            builder.Services.AddHealthChecks(builder.Configuration);
        }
    }

    private static void AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        IHealthChecksBuilder healthChecksBuilder = services.AddHealthChecks();

        string? keyVaultUri = configuration["KEY_VAULT_URI"];
        if (string.IsNullOrEmpty(keyVaultUri)) return;
        DefaultAzureCredential defaultAzureCredential = new();
        healthChecksBuilder.AddAzureKeyVault(new Uri(keyVaultUri), defaultAzureCredential, options => { options.AddSecret("healthcheck"); }, tags: [READINESSTAG, ALIVETAG], timeout: TimeSpan.FromSeconds(10));
    }

    public static void AddHealthChecksDev(this IServiceCollection services)
    {
        services.AddHealthChecks();
    }

    public static void MapDefaultHealthCheckEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/api/health/isalive", new HealthCheckOptions()
        {
            ResultStatusCodes =
            {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            Predicate = (check) => check.Tags.Contains(ALIVETAG),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(report));
            }
        }).WithHttpLogging(loggingFields: Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.None);

        app.MapHealthChecks("/api/health/isready", new HealthCheckOptions()
        {
            ResultStatusCodes =
            {
                    [HealthStatus.Healthy] = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status503ServiceUnavailable,
                    [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            },
            Predicate = (check) => check.Tags.Contains(READINESSTAG),
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(report));
            }
        }).WithHttpLogging(loggingFields: Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.None);
    }
}