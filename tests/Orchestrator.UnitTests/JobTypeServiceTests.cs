using Orchestrator.Application.Errors;
using Orchestrator.Application.JobTypes;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class JobTypeServiceTests
{
    private readonly OrchestratorDbContext _db = TestDb.Create();
    private readonly JobTypeService _jobTypes;

    public JobTypeServiceTests() => _jobTypes = new JobTypeService(_db);

    [Fact]
    public async Task Create_trims_name_and_stores_optional_costs()
    {
        var created = await _jobTypes.CreateAsync(
            new JobTypeRequest { Name = "  Cut  ", Description = "Trim", LaborCost = 5m, CommissionPercent = 2m, ProfitPercent = 8m }, default);
        Assert.Equal("Cut", created.Name);
        Assert.Equal((5m, 2m, 8m), (created.LaborCost, created.CommissionPercent, created.ProfitPercent));
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_without_costs_leaves_them_null()
    {
        var created = await _jobTypes.CreateAsync(new JobTypeRequest { Name = "Cut" }, default);
        Assert.Null(created.LaborCost);
        Assert.Null(created.CommissionPercent);
        Assert.Null(created.ProfitPercent);
    }

    [Fact]
    public async Task List_is_ordered_by_name_and_get_returns_one()
    {
        await _jobTypes.CreateAsync(new JobTypeRequest { Name = "Fold" }, default);
        var cut = await _jobTypes.CreateAsync(new JobTypeRequest { Name = "Cut" }, default);
        var names = (await _jobTypes.ListAsync(default)).Select(j => j.Name).ToList();
        Assert.Equal(names.Order(Comparer<string>.Default), names);
        Assert.Contains("Cut", names);
        Assert.Contains("Fold", names);
        Assert.Contains("Cutting", names);
        Assert.Equal("Cut", (await _jobTypes.GetAsync(cut.Id, default)).Name);
    }

    [Fact]
    public async Task Get_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _jobTypes.GetAsync(9999, default));

    [Fact]
    public async Task Update_replaces_fields_including_clearing_costs()
    {
        var created = await _jobTypes.CreateAsync(new JobTypeRequest { Name = "Cut", LaborCost = 5m }, default);
        var updated = await _jobTypes.UpdateAsync(created.Id, new JobTypeRequest { Name = "Slice", Description = "Thin" }, default);
        Assert.Equal("Slice", updated.Name);
        Assert.Equal("Thin", updated.Description);
        Assert.Null(updated.LaborCost);
    }

    [Fact]
    public async Task Update_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _jobTypes.UpdateAsync(9999, new JobTypeRequest { Name = "Cut" }, default));

    [Fact]
    public async Task Deactivate_soft_deletes_and_unknown_is_not_found()
    {
        var created = await _jobTypes.CreateAsync(new JobTypeRequest { Name = "Cut" }, default);
        await _jobTypes.DeactivateAsync(created.Id, default);
        Assert.False((await _jobTypes.GetAsync(created.Id, default)).IsActive);
        await Assert.ThrowsAsync<NotFoundException>(() => _jobTypes.DeactivateAsync(9999, default));
    }

    [Fact]
    public async Task Activate_restores_a_deactivated_job_type_and_unknown_is_not_found()
    {
        var created = await _jobTypes.CreateAsync(new JobTypeRequest { Name = "Cut" }, default);
        await _jobTypes.DeactivateAsync(created.Id, default);
        await _jobTypes.ActivateAsync(created.Id, default);
        Assert.True((await _jobTypes.GetAsync(created.Id, default)).IsActive);
        await Assert.ThrowsAsync<NotFoundException>(() => _jobTypes.ActivateAsync(9999, default));
    }

    [Fact]
    public async Task Catalogs_lists_only_catalogs_using_the_job_type()
    {
        var cut = _db.AddJobType("Cut");
        var fold = _db.AddJobType("Fold");
        _db.AddCatalog("B-2", cut, fold);
        _db.AddCatalog("A-1", cut);
        _db.AddCatalog("C-3", fold);

        var catalogs = await _jobTypes.CatalogsAsync(cut.Id, default);
        Assert.Equal(["A-1", "B-2"], catalogs.Select(c => c.Sku));
    }

    [Fact]
    public async Task Catalogs_of_unused_job_type_is_empty_and_unknown_is_not_found()
    {
        var unused = _db.AddJobType("Unused");
        Assert.Empty(await _jobTypes.CatalogsAsync(unused.Id, default));
        await Assert.ThrowsAsync<NotFoundException>(() => _jobTypes.CatalogsAsync(9999, default));
    }
}
