namespace Orchestrator.IntegrationTests;

public class ApprovalChainsTests(ApiFixture api)
{
    private const int Accounting = 3;
    private const int Qa = 4;
    private const int Administratives = 2;

    private static object Body(int catalogId, params int[] departments) =>
        new { name = "Chain " + ApiFixture.Unique(), description = "Review steps", catalogId, departmentIds = departments.Length > 0 ? departments : [Accounting, Qa] };

    [Fact]
    public async Task List_starts_with_the_default_chain()
    {
        var reply = await api.Get("approval-chains");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.True(reply.Items[0].GetProperty("isDefault").GetBoolean());
        Assert.Equal("Default", reply.Items[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task Get_returns_the_default_chain_and_unknown_is_404()
    {
        Assert.True((await api.Get("approval-chains/1"))["isDefault"].GetBoolean());
        ProblemAssert.Is(await api.Get("approval-chains/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_returns_201_with_numbered_stages()
    {
        var catalog = await api.NewCatalog();
        var reply = await api.Post("approval-chains", Body(catalog, Administratives, Accounting));
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/approval-chains/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.False(reply["isDefault"].GetBoolean());
        Assert.Equal(catalog, reply["catalogId"].GetInt32());
        Assert.Equal([(1, "Administratives"), (2, "Accounting")], reply["stages"].EnumerateArray().Select(s => (s.GetProperty("sequence").GetInt32(), s.GetProperty("departmentName").GetString())));
    }

    [Fact]
    public async Task Second_chain_for_the_same_catalog_is_409()
    {
        var catalog = await api.NewCatalog();
        await api.Post("approval-chains", Body(catalog));
        ProblemAssert.Is(await api.Post("approval-chains", Body(catalog)), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_with_unknown_catalog_repeated_or_unknown_departments_is_400()
    {
        ProblemAssert.Is(await api.Post("approval-chains", Body(ApiFixture.UnknownId)), HttpStatusCode.BadRequest);
        var catalog = await api.NewCatalog();
        ProblemAssert.Is(await api.Post("approval-chains", Body(catalog, Accounting, Accounting)), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("approval-chains", Body(catalog, ApiFixture.UnknownId)), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_a_deactivated_department_is_400()
    {
        var department = await api.NewDepartment();
        await api.Post($"departments/{department}/deactivate");
        ProblemAssert.Is(await api.Post("approval-chains", Body(await api.NewCatalog(), department)), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_invalid_fields_is_400()
    {
        var catalog = await api.NewCatalog();
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("approval-chains", new { name = "A", catalogId = catalog, departmentIds = new[] { Accounting } })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("approval-chains", new { name = "Valid", catalogId = 0, departmentIds = new[] { Accounting } })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("approval-chains", new { name = "Valid", catalogId = catalog, departmentIds = Array.Empty<int>() })).Status);
    }

    [Fact]
    public async Task Create_needs_an_admin()
    {
        var catalog = await api.NewCatalog();
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Post("approval-chains", Body(catalog), null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post("approval-chains", Body(catalog), await api.NewEmployee())).Status);
        Assert.Equal(HttpStatusCode.Created, (await api.Post("approval-chains", Body(catalog), ApiFixture.Admin)).Status);
    }

    [Fact]
    public async Task For_catalog_returns_its_own_chain_or_falls_back_to_the_default()
    {
        var withChain = await api.NewCatalog();
        var chain = await api.Post("approval-chains", Body(withChain));
        var without = await api.NewCatalog();

        Assert.Equal(chain.Id, (await api.Get($"approval-chains/for-catalog/{withChain}")).Id);
        var fallback = await api.Get($"approval-chains/for-catalog/{without}");
        Assert.Equal(HttpStatusCode.OK, fallback.Status);
        Assert.True(fallback["isDefault"].GetBoolean());
    }

    [Fact]
    public async Task For_catalog_with_unknown_catalog_is_404() =>
        ProblemAssert.Is(await api.Get($"approval-chains/for-catalog/{ApiFixture.UnknownId}"), HttpStatusCode.NotFound);

    [Fact]
    public async Task Deactivate_and_activate_toggle_the_chain_and_need_an_admin()
    {
        var catalog = await api.NewCatalog();
        var chain = await api.Post("approval-chains", Body(catalog));
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post($"approval-chains/{chain.Id}/deactivate", actor: await api.NewEmployee())).Status);
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"approval-chains/{chain.Id}/deactivate")).Status);
        Assert.False((await api.Get($"approval-chains/{chain.Id}"))["isActive"].GetBoolean());
        Assert.True((await api.Get($"approval-chains/for-catalog/{catalog}"))["isDefault"].GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"approval-chains/{chain.Id}/activate")).Status);
        Assert.True((await api.Get($"approval-chains/{chain.Id}"))["isActive"].GetBoolean());
    }

    [Fact]
    public async Task Deactivate_default_is_409_and_unknown_is_404()
    {
        ProblemAssert.Is(await api.Post("approval-chains/1/deactivate"), HttpStatusCode.Conflict);
        ProblemAssert.Is(await api.Post($"approval-chains/{ApiFixture.UnknownId}/deactivate"), HttpStatusCode.NotFound);
        ProblemAssert.Is(await api.Post($"approval-chains/{ApiFixture.UnknownId}/activate"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_replaces_name_description_and_stages()
    {
        var chain = await api.Post("approval-chains", Body(await api.NewCatalog()));
        var reply = await api.Put($"approval-chains/{chain.Id}", new { name = "Slow lane", description = "Careful", departmentIds = new[] { Administratives } });
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal("Slow lane", reply.Str("name"));
        Assert.Equal("Careful", reply.Str("description"));
        Assert.Equal(["Administratives"], reply["stages"].EnumerateArray().Select(s => s.GetProperty("departmentName").GetString()));
    }

    [Fact]
    public async Task Update_rejects_bad_departments_unknown_chain_and_bad_body()
    {
        var chain = await api.Post("approval-chains", Body(await api.NewCatalog()));
        ProblemAssert.Is(await api.Put($"approval-chains/{chain.Id}", new { name = "Slow lane", departmentIds = new[] { Qa, Qa } }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Put($"approval-chains/{chain.Id}", new { name = "Slow lane", departmentIds = new[] { ApiFixture.UnknownId } }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Put($"approval-chains/{ApiFixture.UnknownId}", new { name = "Slow lane", departmentIds = new[] { Qa } }), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"approval-chains/{chain.Id}", new { name = "Slow lane", departmentIds = Array.Empty<int>() })).Status);
    }

    [Fact]
    public async Task Update_needs_an_admin()
    {
        var chain = await api.Post("approval-chains", Body(await api.NewCatalog()));
        var body = new { name = "Slow lane", departmentIds = new[] { Qa } };
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Put($"approval-chains/{chain.Id}", body, null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Put($"approval-chains/{chain.Id}", body, await api.NewEmployee())).Status);
    }

    [Fact]
    public async Task Delete_removes_a_custom_chain()
    {
        var chain = await api.Post("approval-chains", Body(await api.NewCatalog()));
        Assert.Equal(HttpStatusCode.NoContent, (await api.Delete($"approval-chains/{chain.Id}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Get($"approval-chains/{chain.Id}")).Status);
    }

    [Fact]
    public async Task Delete_the_default_chain_is_409() =>
        ProblemAssert.Is(await api.Delete("approval-chains/1"), HttpStatusCode.Conflict);

    [Fact]
    public async Task Delete_unknown_is_404_and_needs_an_admin()
    {
        ProblemAssert.Is(await api.Delete("approval-chains/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);
        var chain = await api.Post("approval-chains", Body(await api.NewCatalog()));
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Delete($"approval-chains/{chain.Id}", null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Delete($"approval-chains/{chain.Id}", await api.NewEmployee())).Status);
    }
}
