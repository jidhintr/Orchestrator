using System.Net;
using System.Net.Http.Json;
using Orchestrator.Api.Contracts;

namespace Orchestrator.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthEndpointTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetHealth_WhenPostgresReachable_Returns200()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Healthy", body?.Status);
    }

    [Fact]
    public async Task GetOpenApiDocument_IsServedInDevelopment()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("/api/health", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}

public sealed class HealthEndpointFailureTests : IAsyncLifetime
{
    private readonly ApiFactory _factory = new();

    public ValueTask InitializeAsync() => _factory.InitializeAsync();

    public ValueTask DisposeAsync() => _factory.DisposeAsync();

    [Fact]
    public async Task GetHealth_WhenPostgresDown_Returns503NotFalseSuccess()
    {
        using var client = _factory.CreateClient();
        await _factory.StopPostgresAsync();

        var response = await client.GetAsync("/api/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(TestContext.Current.CancellationToken);
        Assert.Equal("Unhealthy", body?.Status);
    }
}
