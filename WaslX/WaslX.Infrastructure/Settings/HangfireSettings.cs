namespace WaslX.Infrastructure.Settings;

public class HangfireSettings
{
    public const string SectionName = "Hangfire";

    public int WorkerCount { get; set; } = 5;
}