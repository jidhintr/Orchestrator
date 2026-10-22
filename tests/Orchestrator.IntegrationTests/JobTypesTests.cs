namespace Orchestrator.IntegrationTests;

public class JobTypesTests(ApiFixture api)
{
    private static object Body(string? name = null) => new
    {
        name = name ?? "Job " + ApiFixture.Unique(),
        description = "Does a step",
        laborCost = 5,
        commissionPercent = 2,
        profitPercent = 8,
    };

    [Fact]
    public async Task List_contains_the_seeded_job_types()
    {
        var reply = await api.Get("job-types");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        foreach (var name in new[] { "Cutting", "Laminating", "Quality Assurance", "Delivery" })
        {
            Assert.Contains(name, reply.Names());
        }
    }

    [Fact]
    public async Task Get_returns_a_job_type() =>
        Assert.Equal("Cutting", (await api.Get("job-types/1")).Str("name"));

    [Fact]
    public async Task Get_unknown_is_404() =>
        ProblemAssert.Is(await api.Get("job-types/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);

    [Fact]
    public async Task Create_returns_201_with_location_and_costs()
    {
        var reply = await api.Post("job-types", Body());
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/job-types/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.Equal(5m, reply["laborCost"].GetDecimal());
        Assert.Equal(2m, reply["commissionPercent"].GetDecimal());
        Assert.Equal(8m, reply["profitPercent"].GetDecimal());
        Assert.True(reply["isActive"].GetBoolean());
    }

    [Fact]
    public async Task Create_with_only_a_name_leaves_costs_null()
    {
        var reply = await api.Post("job-types", new { name = "Job " + ApiFixture.Unique() });
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.Equal(System.Text.Json.JsonValueKind.Null, reply["laborCost"].ValueKind);
    }

    [Fact]
    public async Task Create_duplicate_name_ignoring_case_is_409()
    {
        var name = "Dup " + ApiFixture.Unique();
        await api.Post("job-types", Body(name));
        ProblemAssert.Is(await api.Post("job-types", Body(name.ToUpperInvariant())), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_with_invalid_values_is_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("job-types", Body("A"))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("job-types", new { name = "Valid name", laborCost = -1 })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("job-types", new { name = "Valid name", commissionPercent = 101 })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("job-types", new { name = "Valid name", profitPercent = -1 })).Status);
    }

    [Fact]
    public async Task Create_needs_an_admin()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Post("job-types", Body(), null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post("job-types", Body(), await api.NewEmployee())).Status);
        Assert.Equal(HttpStatusCode.Created, (await api.Post("job-types", Body(), ApiFixture.Admin)).Status);
    }

    [Fact]
    public async Task Update_replaces_the_fields()
    {
        var id = await api.NewJobType();
        var reply = await api.Put($"job-types/{id}", new { name = "Renamed " + ApiFixture.Unique(), description = "New" });
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal("New", reply.Str("description"));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, reply["laborCost"].ValueKind);
    }

    [Fact]
    public async Task Update_to_an_existing_name_is_409()
    {
        var taken = await api.Post("job-types", Body());
        ProblemAssert.Is(await api.Put($"job-types/{await api.NewJobType()}", Body(taken.Str("name"))), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_unknown_is_404_and_bad_body_is_400()
    {
        ProblemAssert.Is(await api.Put($"job-types/{ApiFixture.UnknownId}", Body()), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"job-types/{await api.NewJobType()}", Body("A"))).Status);
    }

    [Fact]
    public async Task Update_needs_an_admin() =>
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Put($"job-types/{await api.NewJobType()}", Body(), await api.NewEmployee())).Status);

    [Fact]
    public async Task Deactivate_keeps_the_record_inactive()
    {
        var id = await api.NewJobType();
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"job-types/{id}/deactivate")).Status);
        Assert.False((await api.Get($"job-types/{id}"))["isActive"].GetBoolean());
    }

    [Fact]
    public async Task Activate_restores_the_record_and_needs_an_admin()
    {
        var id = await api.NewJobType();
        await api.Post($"job-types/{id}/deactivate");
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post($"job-types/{id}/activate", actor: await api.NewEmployee())).Status);
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"job-types/{id}/activate")).Status);
        Assert.True((await api.Get($"job-types/{id}"))["isActive"].GetBoolean());
        ProblemAssert.Is(await api.Post($"job-types/{ApiFixture.UnknownId}/activate"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_unknown_is_404_and_needs_an_admin()
    {
        ProblemAssert.Is(await api.Post($"job-types/{ApiFixture.UnknownId}/deactivate"), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post($"job-types/{await api.NewJobType()}/deactivate", actor: await api.NewEmployee())).Status);
    }

    [Fact]
    public async Task Catalogs_lists_catalogs_that_use_the_job_type()
    {
        var used = await api.NewJobType();
        var unused = await api.NewJobType();
        var catalog = await api.NewCatalog(used);

        Assert.Equal([catalog], (await api.Get($"job-types/{used}/catalogs")).Ids);
        Assert.Empty((await api.Get($"job-types/{unused}/catalogs")).Items);
    }

    [Fact]
    public async Task Catalogs_of_unknown_job_type_is_404() =>
        ProblemAssert.Is(await api.Get($"job-types/{ApiFixture.UnknownId}/catalogs"), HttpStatusCode.NotFound);
}
