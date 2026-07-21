namespace WaslX.Application.Features.Platform.Dtos;

/// <summary>The health of a single monitored component (US-6.10a). <c>Status</c> ∈ Healthy | Degraded | Down.</summary>
public record HealthComponentResponse(string Name, string Status, double? LatencyMs, string? Description);

/// <summary>Aggregate system-health snapshot: an overall status plus each component's result.</summary>
public record SystemHealthResponse(string Status, DateTime CheckedAt, IReadOnlyList<HealthComponentResponse> Components);
