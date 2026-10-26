using Orchestrator.Application.Errors;
using Orchestrator.Application.Orders;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class OrderFlowServiceTests
{
    private const int Accounting = 3;
    private const int QualityCheck = 4;

    private readonly OrchestratorDbContext _db = TestDb.Create();
    private readonly Client _client;
    private readonly User _ops;
    private readonly User _clerk;

    public OrderFlowServiceTests()
    {
        _client = _db.AddClient();
        _ops = _db.AddUser("ops", [SystemIds.OperationsDepartment], SystemIds.ManagerRole);
        _clerk = _db.AddUser("clerk", [SystemIds.OperationsDepartment]);
    }

    private OrderService Orders(int userId) => new(_db, _db.As(userId));

    private OrderApprovalService Approvals(int userId) => new(_db, _db.As(userId));

    private OrderStepService Steps(int userId) => new(_db, _db.As(userId));

    private Catalog NewCatalog(string sku, params int[] chain)
    {
        var catalog = _db.AddCatalog(sku, _db.AddJobType("Cut " + sku), _db.AddJobType("Fold " + sku));
        if (chain.Length > 0)
        {
            _db.AddChain(catalog, chain);
        }

        return catalog;
    }

    private Task<OrderResponse> Place(params Catalog[] catalogs) => Orders(_clerk.Id).PlaceAsync(new PlaceOrderRequest
    {
        ClientId = _client.Id,
        Items = [.. catalogs.Select(c => new OrderItemRequest { CatalogId = c.Id, Quantity = 1 })],
    }, default);

    [Fact]
    public async Task Several_catalogs_merge_their_chains_without_repeating_a_department()
    {
        var order = await Place(NewCatalog("A", Accounting, QualityCheck), NewCatalog("B", QualityCheck, SystemIds.AdministrativesDepartment), NewCatalog("C"));
        Assert.Equal(OrderStatus.PendingApproval, order.Status);
        var approvals = await Approvals(_clerk.Id).ListAsync(order.Id, default);
        Assert.Equal([Accounting, QualityCheck, SystemIds.AdministrativesDepartment], approvals.Select(a => a.DepartmentId));
    }

    [Fact]
    public async Task A_catalog_without_its_own_chain_follows_the_default_chain()
    {
        _db.ApprovalChains.Single(c => c.IsDefault).Stages.Add(new ApprovalChainStage { DepartmentId = Accounting, Sequence = 1 });
        _db.SaveChanges();
        var order = await Place(NewCatalog("A"));
        Assert.Equal([Accounting], (await Approvals(_clerk.Id).ListAsync(order.Id, default)).Select(a => a.DepartmentId));
    }

    [Fact]
    public async Task Approving_every_stage_starts_the_line_and_a_wrong_department_is_forbidden()
    {
        var order = await Place(NewCatalog("A", Accounting, QualityCheck));
        var accounting = _db.AddUser("acc", [Accounting], SystemIds.ManagerRole);
        var quality = _db.AddUser("qa", [QualityCheck], SystemIds.ManagerRole);
        await Assert.ThrowsAsync<ForbiddenException>(() => Approvals(quality.Id).ApproveAsync(order.Id, default));
        Assert.Equal(OrderStatus.PendingApproval, (await Approvals(accounting.Id).ApproveAsync(order.Id, default)).Status);
        var started = await Approvals(quality.Id).ApproveAsync(order.Id, default);
        Assert.Equal(OrderStatus.InProgress, started.Status);
        Assert.Equal([StepStatus.Ready, StepStatus.Waiting], started.Items[0].Steps.Select(s => s.Status));
        await Assert.ThrowsAsync<ConflictException>(() => Approvals(quality.Id).ApproveAsync(order.Id, default));
        await Assert.ThrowsAsync<NotFoundException>(() => Approvals(quality.Id).ApproveAsync(9999, default));
    }

    [Fact]
    public async Task Deny_cancels_to_estimation_and_records_the_reason()
    {
        var order = await Place(NewCatalog("A", Accounting));
        var accounting = _db.AddUser("acc", [Accounting], SystemIds.ManagerRole);
        var denied = await Approvals(accounting.Id).DenyAsync(order.Id, new StepReasonRequest { Reason = " Credit limit " }, default);
        Assert.Equal((OrderStatus.Cancelled, SystemIds.EstimationDepartment), (denied.Status, denied.ReturnedToDepartmentId));
        Assert.Equal(("Denied", "Credit limit"), ((await Approvals(accounting.Id).ListAsync(order.Id, default))[0].Decision.ToString(), (await Approvals(accounting.Id).ListAsync(order.Id, default))[0].Reason));
        Assert.Contains(await Orders(accounting.Id).EventsAsync(order.Id, default), e => e.Type == OrderEventType.ApprovalDenied);
    }

    [Fact]
    public async Task An_order_without_a_chain_runs_through_to_completion_with_earnings_left_at_zero()
    {
        var order = await Place(NewCatalog("A"));
        Assert.Equal(OrderStatus.InProgress, order.Status);
        var first = order.Items[0].Steps[0].Id;
        var second = order.Items[0].Steps[1].Id;

        await Steps(_ops.Id).StartAsync(first, default);
        await Assert.ThrowsAsync<ForbiddenException>(() => Steps(_clerk.Id).CompleteAsync(first, default));
        await Steps(_ops.Id).CompleteAsync(first, default);
        await Steps(_ops.Id).StartAsync(second, default);
        var last = await Steps(_ops.Id).CompleteAsync(second, default);

        Assert.Equal(StepStatus.Completed, last.Status);
        Assert.Equal(OrderStatus.Completed, (await Orders(_ops.Id).GetAsync(order.Id, default)).Status);
        Assert.Equal(0m, _db.OrderItemJobSteps.Sum(s => s.EarnedAmount));
        Assert.Contains(await Orders(_ops.Id).EventsAsync(order.Id, default), e => e.Type == OrderEventType.Completed);
        Assert.Equal([StepEntryType.Ready, StepEntryType.Started, StepEntryType.Completed], (await Steps(_ops.Id).LogsAsync(first, default)).Select(l => l.EntryType));
    }

    [Fact]
    public async Task Steps_snapshot_department_standard_minutes_and_resolved_costs()
    {
        var catalog = _db.AddCatalog("COST", _db.AddJobType("Plain"), _db.AddJobType("Priced"));
        catalog.LaborCost = 20m;
        catalog.CommissionPercent = 3m;
        catalog.ProfitPercent = 10m;
        var priced = _db.JobTypes.Single(j => j.Name == "Priced");
        priced.LaborCost = 50m;
        _db.CatalogJobSteps.Single(s => s.CatalogId == catalog.Id && s.Sequence == 1).StandardMinutes = 30;
        _db.SaveChanges();

        await Place(catalog);
        var steps = _db.OrderItemJobSteps.OrderBy(s => s.SortOrder).ToList();
        Assert.Equal([(SystemIds.OperationsDepartment, 30, 20m, false), (SystemIds.OperationsDepartment, (int?)null, 50m, true)], steps.Select(s => (s.DepartmentId, s.StandardMinutes, s.LaborCost!.Value, s.CanReject)));
        Assert.Equal([3m, 3m], steps.Select(s => s.CommissionPercent!.Value));
    }
}
