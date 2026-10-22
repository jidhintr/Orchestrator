namespace Orchestrator.IntegrationTests;

public class CatalogsTests(ApiFixture api)
{
    private static string Sku() => "SKU-" + ApiFixture.Unique();

    [Fact]
    public async Task Create_returns_201_with_prices_and_ordered_steps()
    {
        var cut = await api.NewJobType();
        var fold = await api.NewJobType();
        var reply = await api.Post("catalogs", ApiFixture.CatalogBody(Sku(), [fold, cut]));
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/catalogs/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.Equal(100m, reply["basePrice"].GetDecimal());
        Assert.Equal(5m, reply["catalogDiscountPercent"].GetDecimal());
        Assert.Equal(20m, reply["laborCost"].GetDecimal());
        Assert.Equal(3m, reply["commissionPercent"].GetDecimal());
        Assert.Equal(10m, reply["profitPercent"].GetDecimal());
        Assert.True(reply["isActive"].GetBoolean());
        Assert.Equal([(fold, 1), (cut, 2)], reply["steps"].EnumerateArray().Select(s => (s.GetProperty("jobTypeId").GetInt32(), s.GetProperty("sequence").GetInt32())));
    }

    [Fact]
    public async Task List_and_get_return_created_catalogs()
    {
        var id = await api.NewCatalog();
        Assert.Contains(id, (await api.Get("catalogs")).Ids);
        Assert.Equal(id, (await api.Get($"catalogs/{id}")).Id);
    }

    [Fact]
    public async Task Get_unknown_is_404() =>
        ProblemAssert.Is(await api.Get("catalogs/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);

    [Fact]
    public async Task Create_duplicate_sku_is_409()
    {
        var sku = Sku();
        await api.Post("catalogs", ApiFixture.CatalogBody(sku, [await api.NewJobType()]));
        ProblemAssert.Is(await api.Post("catalogs", ApiFixture.CatalogBody(sku, [await api.NewJobType()])), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_with_unknown_or_deactivated_job_type_is_400()
    {
        ProblemAssert.Is(await api.Post("catalogs", ApiFixture.CatalogBody(Sku(), [ApiFixture.UnknownId])), HttpStatusCode.BadRequest);
        var off = await api.NewJobType();
        await api.Post($"job-types/{off}/deactivate");
        ProblemAssert.Is(await api.Post("catalogs", ApiFixture.CatalogBody(Sku(), [off])), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_with_invalid_values_is_400()
    {
        var jobType = await api.NewJobType();
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("catalogs", ApiFixture.CatalogBody("ab", [jobType]))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("catalogs", ApiFixture.CatalogBody(Sku(), [jobType], basePrice: -1m))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("catalogs", ApiFixture.CatalogBody(Sku(), []))).Status);
    }

    [Fact]
    public async Task Update_replaces_fields_and_steps()
    {
        var id = await api.NewCatalog();
        var other = await api.NewJobType();
        var sku = Sku();
        var reply = await api.Put($"catalogs/{id}", ApiFixture.CatalogBody(sku, [other], basePrice: 250m));
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal(sku, reply.Str("sku"));
        Assert.Equal(250m, reply["basePrice"].GetDecimal());
        Assert.Equal([other], reply["steps"].EnumerateArray().Select(s => s.GetProperty("jobTypeId").GetInt32()));
    }

    [Fact]
    public async Task Update_to_a_used_sku_is_409()
    {
        var taken = await api.Get($"catalogs/{await api.NewCatalog()}");
        var id = await api.NewCatalog();
        ProblemAssert.Is(await api.Put($"catalogs/{id}", ApiFixture.CatalogBody(taken.Str("sku"), [await api.NewJobType()])), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_unknown_is_404_and_unknown_job_type_is_400()
    {
        ProblemAssert.Is(await api.Put($"catalogs/{ApiFixture.UnknownId}", ApiFixture.CatalogBody(Sku(), [await api.NewJobType()])), HttpStatusCode.NotFound);
        ProblemAssert.Is(await api.Put($"catalogs/{await api.NewCatalog()}", ApiFixture.CatalogBody(Sku(), [ApiFixture.UnknownId])), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_a_catalog_with_orders_is_409()
    {
        var id = await api.NewCatalog();
        await api.AddOrder(id);
        ProblemAssert.Is(await api.Put($"catalogs/{id}", ApiFixture.CatalogBody(Sku(), [await api.NewJobType()])), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Activate_restores_the_record_and_unknown_is_404()
    {
        var id = await api.NewCatalog();
        await api.Post($"catalogs/{id}/deactivate");
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"catalogs/{id}/activate")).Status);
        Assert.True((await api.Get($"catalogs/{id}"))["isActive"].GetBoolean());
        ProblemAssert.Is(await api.Post($"catalogs/{ApiFixture.UnknownId}/activate"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_keeps_the_record_inactive()
    {
        var id = await api.NewCatalog();
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"catalogs/{id}/deactivate")).Status);
        Assert.False((await api.Get($"catalogs/{id}"))["isActive"].GetBoolean());
    }

    [Fact]
    public async Task Deactivate_a_catalog_with_orders_is_409()
    {
        var id = await api.NewCatalog();
        await api.AddOrder(id);
        ProblemAssert.Is(await api.Post($"catalogs/{id}/deactivate"), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Deactivate_unknown_is_404() =>
        ProblemAssert.Is(await api.Post($"catalogs/{ApiFixture.UnknownId}/deactivate"), HttpStatusCode.NotFound);

    [Fact]
    public async Task Clone_copies_prices_and_steps_under_a_new_sku()
    {
        var jobType = await api.NewJobType();
        var source = await api.Post("catalogs", ApiFixture.CatalogBody(Sku(), [jobType]));
        var sku = Sku();
        var clone = await api.Post($"catalogs/{source.Id}/clone", new { sku, name = "Copy" });
        Assert.Equal(HttpStatusCode.Created, clone.Status);
        Assert.EndsWith($"/api/v1/catalogs/{clone.Id}", clone.Header("Location"), StringComparison.Ordinal);
        Assert.NotEqual(source.Id, clone.Id);
        Assert.Equal(sku, clone.Str("sku"));
        Assert.Equal("Copy", clone.Str("name"));
        Assert.Equal(source["basePrice"].GetDecimal(), clone["basePrice"].GetDecimal());
        Assert.Equal([jobType], clone["steps"].EnumerateArray().Select(s => s.GetProperty("jobTypeId").GetInt32()));
    }

    [Fact]
    public async Task Clone_is_allowed_when_the_source_has_orders()
    {
        var id = await api.NewCatalog();
        await api.AddOrder(id);
        Assert.Equal(HttpStatusCode.Created, (await api.Post($"catalogs/{id}/clone", new { sku = Sku(), name = "Copy" })).Status);
    }

    [Fact]
    public async Task Clone_rejects_duplicate_sku_unknown_source_and_bad_body()
    {
        var source = await api.Get($"catalogs/{await api.NewCatalog()}");
        ProblemAssert.Is(await api.Post($"catalogs/{source.Id}/clone", new { sku = source.Str("sku"), name = "Copy" }), HttpStatusCode.Conflict);
        ProblemAssert.Is(await api.Post($"catalogs/{ApiFixture.UnknownId}/clone", new { sku = Sku(), name = "Copy" }), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post($"catalogs/{source.Id}/clone", new { sku = "ab", name = "Copy" })).Status);
    }
}
