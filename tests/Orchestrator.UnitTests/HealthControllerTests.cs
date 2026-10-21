using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orchestrator.Api.Contracts;
using Orchestrator.Api.Controllers;

namespace Orchestrator.UnitTests;

public sealed class HealthControllerTests
{
    [Fact]
    public async Task Get_WhenAllChecksHealthy_Returns200WithChecks()
    {
        var controller = CreateController(HealthStatus.Healthy);

        var result = await controller.Get(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<HealthResponse>(ok.Value);
        Assert.Equal("Healthy", body.Status);
        Assert.Equal(new HealthCheckEntry("postgres", "Healthy"), Assert.Single(body.Checks));
    }

    [Theory]
    [InlineData(HealthStatus.Unhealthy)]
    [InlineData(HealthStatus.Degraded)]
    public async Task Get_WhenAnyCheckNotHealthy_Returns503(HealthStatus status)
    {
        var controller = CreateController(status);

        var result = await controller.Get(CancellationToken.None);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        Assert.Equal(status.ToString(), Assert.IsType<HealthResponse>(objectResult.Value).Status);
    }

    private static HealthController CreateController(HealthStatus status) => new(new StubHealthCheckService(status));

    private sealed class StubHealthCheckService(HealthStatus status) : HealthCheckService
    {
        public override Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                ["postgres"] = new(status, null, TimeSpan.Zero, null, null),
            };
            return Task.FromResult(new HealthReport(entries, TimeSpan.Zero));
        }
    }
}
