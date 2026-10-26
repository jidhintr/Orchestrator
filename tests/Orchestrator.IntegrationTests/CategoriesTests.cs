namespace Orchestrator.IntegrationTests;

public class CategoriesTests(ApiFixture api)
{
    private static string Name() => "Category " + ApiFixture.Unique();

    [Fact]
    public async Task Create_returns_201_and_the_category_is_listed_and_readable()
    {
        var reply = await api.Post("categories", new { name = Name(), description = "Paper bags" });
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/categories/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.True(reply.Id >= 1000);
        Assert.Contains(reply.Id, (await api.Get("categories")).Ids);
        Assert.Equal(reply.Str("name"), (await api.Get($"categories/{reply.Id}")).Str("name"));
    }

    [Fact]
    public async Task Duplicate_name_is_409_regardless_of_case()
    {
        var name = Name();
        await api.Post("categories", new { name });
        ProblemAssert.Is(await api.Post("categories", new { name = name.ToUpperInvariant() }), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_replaces_the_name_and_unknown_is_404()
    {
        var created = await api.Post("categories", new { name = Name() });
        var newName = Name();
        Assert.Equal(newName, (await api.Put($"categories/{created.Id}", new { name = newName })).Str("name"));
        ProblemAssert.Is(await api.Put($"categories/{ApiFixture.UnknownId}", new { name = Name() }), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("categories", new { name = "x" })).Status);
    }

    [Fact]
    public async Task Deactivate_and_activate_toggle_is_active()
    {
        var created = await api.Post("categories", new { name = Name() });
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"categories/{created.Id}/deactivate")).Status);
        Assert.False((await api.Get($"categories/{created.Id}"))["isActive"].GetBoolean());
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"categories/{created.Id}/activate")).Status);
        Assert.True((await api.Get($"categories/{created.Id}"))["isActive"].GetBoolean());
        ProblemAssert.Is(await api.Post($"categories/{ApiFixture.UnknownId}/deactivate"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Only_admins_change_categories_and_anonymous_is_401()
    {
        var manager = await api.NewManager(Orchestrator.Domain.SystemIds.UnassignedDepartment);
        ProblemAssert.Is(await api.Post("categories", new { name = Name() }, manager), HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.OK, (await api.Get("categories", manager)).Status);
        ProblemAssert.Is(await api.Get("categories", actor: null), HttpStatusCode.Unauthorized);
    }
}
