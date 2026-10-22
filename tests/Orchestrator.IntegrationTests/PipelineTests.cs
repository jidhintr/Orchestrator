namespace Orchestrator.IntegrationTests;

public class PipelineTests(ApiFixture api)
{
    [Fact]
    public async Task Health_is_healthy_when_postgres_is_reachable()
    {
        var reply = await api.Send(HttpMethod.Get, "/api/health", actor: null);
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal("Healthy", reply.Raw);
    }

    [Fact]
    public async Task Health_is_unhealthy_when_postgres_is_unreachable()
    {
        var reply = await api.Send(HttpMethod.Get, "/api/health", actor: null, brokenDb: true);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, reply.Status);
        Assert.Equal("Unhealthy", reply.Raw);
    }

    [Fact]
    public async Task Correlation_id_is_echoed()
    {
        var reply = await api.Send(HttpMethod.Get, "/api/health", actor: null, correlationId: "test-corr-123");
        Assert.Equal("test-corr-123", reply.Header("X-Correlation-Id"));
    }

    [Fact]
    public async Task Correlation_id_is_generated_when_missing_or_unsafe()
    {
        var generated = await api.Send(HttpMethod.Get, "/api/health", actor: null);
        Assert.Matches("^[0-9a-f]{32}$", generated.Header("X-Correlation-Id")!);

        var unsafeId = await api.Send(HttpMethod.Get, "/api/health", actor: null, correlationId: "bad id!");
        Assert.NotEqual("bad id!", unsafeId.Header("X-Correlation-Id"));
        Assert.Matches("^[0-9a-f]{32}$", unsafeId.Header("X-Correlation-Id")!);
    }

    [Fact]
    public async Task Errors_are_problem_json_with_trace_and_correlation_ids()
    {
        var reply = await api.Send(HttpMethod.Get, "/api/v1/roles/" + ApiFixture.UnknownId, correlationId: "abc-1");
        ProblemAssert.Is(reply, HttpStatusCode.NotFound);
        Assert.Equal(404, reply["status"].GetInt32());
        Assert.Equal("Resource not found.", reply.Str("title"));
        Assert.Equal("abc-1", reply.Str("correlationId"));
        Assert.False(string.IsNullOrEmpty(reply.Str("traceId")));
    }

    [Fact]
    public async Task Validation_errors_are_400_with_an_errors_map()
    {
        var reply = await api.Post("roles", new { name = "A", description = "x" });
        ProblemAssert.Is(reply, HttpStatusCode.BadRequest);
        Assert.True(reply["errors"].TryGetProperty("Name", out _));
    }

    [Fact]
    public async Task Malformed_json_is_400()
    {
        var reply = await api.Post("roles", "{not json");
        Assert.Equal(HttpStatusCode.BadRequest, reply.Status);
    }

    [Fact]
    public async Task Database_outage_is_a_500_without_exception_text()
    {
        var reply = await api.Send(HttpMethod.Get, "/api/v1/roles", brokenDb: true);
        ProblemAssert.Is(reply, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred.", reply.Str("title"));
        Assert.DoesNotContain("Host=", reply.Raw, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", reply.Raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Routes_without_a_supported_api_version_are_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await api.Send(HttpMethod.Get, "/api/roles")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Send(HttpMethod.Get, "/api/v9/roles")).Status);
    }

    [Fact]
    public async Task Supported_versions_are_reported()
    {
        var reply = await api.Get("roles");
        Assert.Contains("1.0", reply.Header("api-supported-versions"), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Non_numeric_ids_do_not_match_a_route() =>
        Assert.Equal(HttpStatusCode.NotFound, (await api.Get("roles/abc")).Status);

    [Fact]
    public async Task Openapi_document_and_scalar_are_served_in_development()
    {
        var document = await api.Send(HttpMethod.Get, "/openapi/v1.json", actor: null);
        Assert.Equal(HttpStatusCode.OK, document.Status);
        Assert.Contains("/api/v1/roles", document.Raw, StringComparison.Ordinal);
        Assert.Equal(HttpStatusCode.OK, (await api.Send(HttpMethod.Get, "/scalar", actor: null)).Status);
    }

    [Fact]
    public async Task Reads_need_a_signed_in_user()
    {
        foreach (var path in new[] { "roles", "departments", "users", "job-types", "catalogs", "clients", "approval-chains" })
        {
            ProblemAssert.Is(await api.Get(path, null), HttpStatusCode.Unauthorized);
            Assert.Equal(HttpStatusCode.OK, (await api.Get(path)).Status);
        }
    }
}
