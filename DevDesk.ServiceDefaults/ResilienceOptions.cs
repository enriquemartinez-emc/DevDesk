namespace DevDesk.ServiceDefaults;

public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    // Local models on CPU are slow; the standard ~30s attempt timeout cancels a chat call mid-answer.
    public TimeSpan AttemptTimeout { get; init; } = TimeSpan.FromMinutes(3);
    public TimeSpan TotalTimeout { get; init; } = TimeSpan.FromMinutes(6);
}
