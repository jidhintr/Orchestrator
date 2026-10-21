using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orchestrator.Api.Contracts;

namespace Orchestrator.Api.Controllers;

[ApiController]
[Route("api/health")]
[Produces("application/json")]
public sealed class HealthController(HealthCheckService healthCheckService) : ControllerBase
{
    /// <summary>Reports API liveness and Postgres connectivity.</summary>
    /// <response code="200">All dependencies are healthy.</response>
    /// <response code="503">At least one dependency is unhealthy.</response>
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        var report = await healthCheckService.CheckHealthAsync(cancellationToken);
        var body = new HealthResponse(
            report.Status.ToString(),
            [.. report.Entries.Select(e => new HealthCheckEntry(e.Key, e.Value.Status.ToString()))]);

        return report.Status == HealthStatus.Healthy
            ? Ok(body)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, body);
    }
}
