namespace WaslX.Infrastructure.Settings;

public class HangfireSettings
{
    public const string SectionName = "Hangfire";

    public int WorkerCount { get; set; } = 5;

    /// <summary>HTTP Basic Auth credentials gating /hangfire — empty means the dashboard denies everyone.</summary>
    public string DashboardUsername { get; set; } = string.Empty;
    public string DashboardPassword { get; set; } = string.Empty;
}