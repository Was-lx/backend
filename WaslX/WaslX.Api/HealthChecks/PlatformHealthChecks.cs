using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using WaslX.Infrastructure.Settings;
using WaslX.Persistance.Data;

namespace WaslX.Api.HealthChecks;

/// <summary>
/// System health checks surfaced to the Platform Owner (US-6.10a). Each is registered with
/// <c>AddHealthChecks()</c> and never throws — a failure/exception is reported as an Unhealthy or
/// Degraded result rather than propagating.
/// </summary>

/// <summary>SQL Server / application database reachability — a lightweight connectivity probe.</summary>
public sealed class DatabaseHealthCheck(ApplicationDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database is reachable.")
                : HealthCheckResult.Unhealthy("Cannot connect to the database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connectivity check failed.", ex);
        }
    }
}

/// <summary>AI provider configuration presence (config-only; does not call the provider).</summary>
public sealed class AiProviderConfigHealthCheck(IConfiguration configuration) : IHealthCheck
{
    // Any of these keys being populated is treated as "AI provider is configured".
    private static readonly string[] KeyCandidates =
    [
        "Groq:ApiKey", "HuggingFace:ApiKey"
    ];

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var configured = KeyCandidates.Any(k => !string.IsNullOrWhiteSpace(configuration[k]));
            return Task.FromResult(configured
                ? HealthCheckResult.Healthy("AI provider is configured.")
                : HealthCheckResult.Degraded("AI provider API key is not configured."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Degraded("AI provider config check failed.", ex));
        }
    }
}

/// <summary>WhatsApp Cloud API configuration presence (App Id + secret).</summary>
public sealed class WhatsAppConfigHealthCheck(IOptions<WhatsAppOptions> options) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var o = options.Value;
            var configured = !string.IsNullOrWhiteSpace(o.AppId)
                && !string.IsNullOrWhiteSpace(o.AppSecret)
                && !string.IsNullOrWhiteSpace(o.ApiBaseUrl);
            return Task.FromResult(configured
                ? HealthCheckResult.Healthy("WhatsApp Cloud API is configured.")
                : HealthCheckResult.Degraded("WhatsApp Cloud API configuration is incomplete."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Degraded("WhatsApp config check failed.", ex));
        }
    }
}

/// <summary>Hangfire background-job storage reachability (drives campaigns + maintenance jobs).</summary>
public sealed class HangfireHealthCheck(JobStorage jobStorage) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // A cheap round-trip to the job store; throws if the storage is unreachable.
            var stats = jobStorage.GetMonitoringApi().GetStatistics();
            return Task.FromResult(HealthCheckResult.Healthy($"Hangfire storage reachable (servers={stats.Servers})."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage is unreachable.", ex));
        }
    }
}
