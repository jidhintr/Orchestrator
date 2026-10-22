using Orchestrator.Application.Catalogs;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class CatalogServiceTests
{
    private readonly OrchestratorDbContext _db = TestDb.Create();
    private readonly CatalogService _catalogs;
    private readonly JobType _cut;
    private readonly JobType _fold;

    public CatalogServiceTests()
    {
        _catalogs = new CatalogService(_db);
        _cut = _db.AddJobType("Cut");
        _fold = _db.AddJobType("Fold");
    }

    private CatalogRequest Request(string sku = "BOOK-1", params int[] jobTypeIds) => new()
    {
        Sku = sku,
        Name = $"Book {sku}",
        Description = "Hardcover",
        BasePrice = 100m,
        CatalogDiscountPercent = 5m,
        LaborCost = 20m,
        CommissionPercent = 3m,
        ProfitPercent = 10m,
        JobTypeIds = jobTypeIds.Length == 0 ? [_cut.Id, _fold.Id] : jobTypeIds,
    };

    [Fact]
    public async Task Create_stores_prices_and_ordered_steps()
    {
        var created = await _catalogs.CreateAsync(Request("  BOOK-1  ", _fold.Id, _cut.Id), default);
        Assert.Equal("BOOK-1", created.Sku);
        Assert.Equal(100m, created.BasePrice);
        Assert.Equal(5m, created.CatalogDiscountPercent);
        Assert.Equal(20m, created.LaborCost);
        Assert.Equal(3m, created.CommissionPercent);
        Assert.Equal(10m, created.ProfitPercent);
        Assert.True(created.IsActive);
        Assert.Equal([("Fold", 1), ("Cut", 2)], created.Steps.Select(s => (s.JobTypeName, s.Sequence)));
    }

    [Fact]
    public async Task Create_unknown_job_type_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => _catalogs.CreateAsync(Request("X-1", 9999), default));

    [Fact]
    public async Task Create_deactivated_job_type_is_bad_request()
    {
        var off = _db.AddJobType("Off", active: false);
        await Assert.ThrowsAsync<BadRequestException>(() => _catalogs.CreateAsync(Request("X-1", off.Id), default));
    }

    [Fact]
    public async Task List_is_ordered_by_name_and_get_returns_one()
    {
        await _catalogs.CreateAsync(Request("B-2"), default);
        var first = await _catalogs.CreateAsync(Request("A-1"), default);
        Assert.Equal(["A-1", "B-2"], (await _catalogs.ListAsync(default)).Select(c => c.Sku));
        Assert.Equal("A-1", (await _catalogs.GetAsync(first.Id, default)).Sku);
    }

    [Fact]
    public async Task Get_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _catalogs.GetAsync(9999, default));

    [Fact]
    public async Task Update_replaces_fields_and_steps()
    {
        var created = await _catalogs.CreateAsync(Request(), default);
        var updated = await _catalogs.UpdateAsync(created.Id, Request("BOOK-2", _fold.Id) with { BasePrice = 250m }, default);
        Assert.Equal("BOOK-2", updated.Sku);
        Assert.Equal(250m, updated.BasePrice);
        Assert.Equal(["Fold"], updated.Steps.Select(s => s.JobTypeName));
    }

    [Fact]
    public async Task Update_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _catalogs.UpdateAsync(9999, Request(), default));

    [Fact]
    public async Task Update_catalog_with_orders_is_conflict()
    {
        var created = await _catalogs.CreateAsync(Request(), default);
        _db.Orders.Add(new Order { CatalogId = created.Id });
        _db.SaveChanges();
        await Assert.ThrowsAsync<ConflictException>(() => _catalogs.UpdateAsync(created.Id, Request("BOOK-2"), default));
    }

    [Fact]
    public async Task Update_with_unknown_job_type_is_bad_request()
    {
        var created = await _catalogs.CreateAsync(Request(), default);
        await Assert.ThrowsAsync<BadRequestException>(() => _catalogs.UpdateAsync(created.Id, Request("BOOK-1", 9999), default));
    }

    [Fact]
    public async Task Deactivate_soft_deletes()
    {
        var created = await _catalogs.CreateAsync(Request(), default);
        await _catalogs.DeactivateAsync(created.Id, default);
        Assert.False((await _catalogs.GetAsync(created.Id, default)).IsActive);
    }

    [Fact]
    public async Task Activate_restores_a_deactivated_catalog_and_unknown_is_not_found()
    {
        var created = await _catalogs.CreateAsync(Request(), default);
        await _catalogs.DeactivateAsync(created.Id, default);
        await _catalogs.ActivateAsync(created.Id, default);
        Assert.True((await _catalogs.GetAsync(created.Id, default)).IsActive);
        await Assert.ThrowsAsync<NotFoundException>(() => _catalogs.ActivateAsync(9999, default));
    }

    [Fact]
    public async Task Deactivate_catalog_with_orders_is_conflict()
    {
        var created = await _catalogs.CreateAsync(Request(), default);
        _db.Orders.Add(new Order { CatalogId = created.Id });
        _db.SaveChanges();
        await Assert.ThrowsAsync<ConflictException>(() => _catalogs.DeactivateAsync(created.Id, default));
    }

    [Fact]
    public async Task Deactivate_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _catalogs.DeactivateAsync(9999, default));

    [Fact]
    public async Task Clone_copies_prices_and_steps_under_new_sku()
    {
        var source = await _catalogs.CreateAsync(Request(), default);
        var clone = await _catalogs.CloneAsync(source.Id, new CloneCatalogRequest { Sku = " BOOK-C ", Name = " Copy " }, default);
        Assert.NotEqual(source.Id, clone.Id);
        Assert.Equal("BOOK-C", clone.Sku);
        Assert.Equal("Copy", clone.Name);
        Assert.Equal(source.BasePrice, clone.BasePrice);
        Assert.Equal(source.Description, clone.Description);
        Assert.Equal(source.Steps, clone.Steps);
    }

    [Fact]
    public async Task Clone_is_allowed_when_source_has_orders()
    {
        var source = await _catalogs.CreateAsync(Request(), default);
        _db.Orders.Add(new Order { CatalogId = source.Id });
        _db.SaveChanges();
        var clone = await _catalogs.CloneAsync(source.Id, new CloneCatalogRequest { Sku = "BOOK-C", Name = "Copy" }, default);
        Assert.Equal("BOOK-C", clone.Sku);
    }

    [Fact]
    public async Task Clone_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _catalogs.CloneAsync(9999, new CloneCatalogRequest { Sku = "BOOK-C", Name = "Copy" }, default));
}
