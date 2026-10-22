using Orchestrator.Application.ApprovalChains;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class ApprovalChainServiceTests
{
    private const int Accounting = 3;
    private const int Qa = 4;
    private const int Administratives = 2;

    private readonly OrchestratorDbContext _db = TestDb.Create();
    private readonly ApprovalChainService _chains;
    private readonly Catalog _catalog;

    public ApprovalChainServiceTests()
    {
        _chains = new ApprovalChainService(_db);
        _catalog = _db.AddCatalog("BOOK-1", _db.AddJobType("Cut"));
    }

    private CreateApprovalChainRequest Create(string name = "Rush", params int[] departments) =>
        new() { Name = name, CatalogId = _catalog.Id, DepartmentIds = departments.Length == 0 ? [Accounting, Qa] : departments };

    [Fact]
    public async Task List_returns_default_chain_first()
    {
        await _chains.CreateAsync(Create("Aardvark"), default);
        var list = await _chains.ListAsync(default);
        Assert.Equal(["Default", "Aardvark"], list.Select(c => c.Name));
        Assert.True(list[0].IsDefault);
        Assert.Empty(list[0].Stages);
    }

    [Fact]
    public async Task Create_numbers_stages_in_given_order()
    {
        var created = await _chains.CreateAsync(Create("  Rush  ", Administratives, Accounting), default);
        Assert.Equal("Rush", created.Name);
        Assert.Equal(_catalog.Id, created.CatalogId);
        Assert.False(created.IsDefault);
        Assert.Equal([(1, "Administratives"), (2, "Accounting")], created.Stages.Select(s => (s.Sequence, s.DepartmentName)));
    }

    [Fact]
    public async Task Create_unknown_catalog_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => _chains.CreateAsync(Create() with { CatalogId = 9999 }, default));

    [Fact]
    public async Task Create_repeated_department_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => _chains.CreateAsync(Create("Rush", Accounting, Accounting), default));

    [Fact]
    public async Task Create_unknown_department_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => _chains.CreateAsync(Create("Rush", 9999), default));

    [Fact]
    public async Task Create_deactivated_department_is_bad_request()
    {
        var off = _db.AddDepartment("OFF");
        off.IsActive = false;
        _db.SaveChanges();
        await Assert.ThrowsAsync<BadRequestException>(() => _chains.CreateAsync(Create("Rush", off.Id), default));
    }

    [Fact]
    public async Task Get_returns_chain_and_unknown_is_not_found()
    {
        var created = await _chains.CreateAsync(Create(), default);
        Assert.Equal("Rush", (await _chains.GetAsync(created.Id, default)).Name);
        await Assert.ThrowsAsync<NotFoundException>(() => _chains.GetAsync(9999, default));
    }

    [Fact]
    public async Task ForCatalog_returns_its_own_chain_or_the_default()
    {
        var other = _db.AddCatalog("BOOK-2");
        var created = await _chains.CreateAsync(Create(), default);
        Assert.Equal(created.Id, (await _chains.ForCatalogAsync(_catalog.Id, default)).Id);
        Assert.True((await _chains.ForCatalogAsync(other.Id, default)).IsDefault);
    }

    [Fact]
    public async Task ForCatalog_unknown_catalog_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _chains.ForCatalogAsync(9999, default));

    [Fact]
    public async Task Update_replaces_name_description_and_stages()
    {
        var created = await _chains.CreateAsync(Create(), default);
        var updated = await _chains.UpdateAsync(
            created.Id, new UpdateApprovalChainRequest { Name = "Slow", Description = "Careful", DepartmentIds = [Administratives] }, default);
        Assert.Equal("Slow", updated.Name);
        Assert.Equal("Careful", updated.Description);
        Assert.Equal(["Administratives"], updated.Stages.Select(s => s.DepartmentName));
    }

    [Fact]
    public async Task Update_default_chain_can_set_its_stages()
    {
        var updated = await _chains.UpdateAsync(1, new UpdateApprovalChainRequest { Name = "Default", DepartmentIds = [Accounting, Qa, Administratives] }, default);
        Assert.True(updated.IsDefault);
        Assert.Equal(3, updated.Stages.Count);
    }

    [Fact]
    public async Task Update_with_bad_departments_is_bad_request()
    {
        var created = await _chains.CreateAsync(Create(), default);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _chains.UpdateAsync(created.Id, new UpdateApprovalChainRequest { Name = "Slow", DepartmentIds = [Qa, Qa] }, default));
    }

    [Fact]
    public async Task Update_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() =>
            _chains.UpdateAsync(9999, new UpdateApprovalChainRequest { Name = "Slow", DepartmentIds = [Qa] }, default));

    [Fact]
    public async Task Delete_removes_custom_chain()
    {
        var created = await _chains.CreateAsync(Create(), default);
        await _chains.DeleteAsync(created.Id, default);
        await Assert.ThrowsAsync<NotFoundException>(() => _chains.GetAsync(created.Id, default));
    }

    [Fact]
    public async Task Delete_default_chain_is_conflict() =>
        await Assert.ThrowsAsync<ConflictException>(() => _chains.DeleteAsync(1, default));

    [Fact]
    public async Task Delete_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _chains.DeleteAsync(9999, default));
}
