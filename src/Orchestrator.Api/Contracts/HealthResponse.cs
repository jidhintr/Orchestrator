namespace Orchestrator.Api.Contracts;

/// <summary>Aggregate health of the API and its dependencies.</summary>
/// <param name="Status">Healthy, Degraded or Unhealthy.</param>
/// <param name="Checks">Result of each registered dependency check.</param>
public sealed record HealthResponse(string Status, IReadOnlyList<HealthCheckEntry> Checks);

/// <summary>Result of a single dependency check.</summary>
/// <param name="Name">Check name, e.g. postgres.</param>
/// <param name="Status">Healthy, Degraded or Unhealthy.</param>
public sealed record HealthCheckEntry(string Name, string Status);
