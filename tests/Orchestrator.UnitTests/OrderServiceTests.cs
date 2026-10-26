using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Application.Orders;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class OrderServiceTests
{
    private const int High = 2;
    private const int UltraHigh = 4;

    private readonly OrchestratorDbContext _db = TestDb.Create();
    private readonly Catalog _catalog;
    private readonly Client _client;
    private readonly User _manager;
    private readonly User _employee;

    public OrderServiceTests()
    {
        _catalog = _db.AddCatalog("BOOK-1", _db.AddJobType("Cut"), _db.AddJobType("Fold"));
        _db.AddChain(_catalog, 3);
        _client = _db.AddClient();
        _manager = _db.AddUser("boss", [SystemIds.UnassignedDepartment], SystemIds.ManagerRole);
        _employee = _db.AddUser("hand", [SystemIds.UnassignedDepartment]);
    }

    private OrderService As(int userId) => new(_db, _db.As(userId));

    private PlaceOrderRequest Place(int? clientId = null, int? catalogId = null, int priorityId = SystemIds.NormalPriority, int quantity = 3) => new()
    {
        ClientId = clientId ?? _client.Id,
        Items = [new OrderItemRequest { CatalogId = catalogId ?? _catalog.Id, Quantity = quantity }],
        PriorityId = priorityId,
    };

    [Fact]
    public async Task Place_snapshots_the_item_copies_job_steps_and_appends_a_placed_event()
    {
        var order = await As(_employee.Id).PlaceAsync(Place(priorityId: High), default);
        Assert.Equal((OrderStatus.PendingApproval, "High", 100m), (order.Status, order.PriorityName, order.PriorityCost));
        Assert.Equal((30m, 0m, 130m), (order.SubTotal, order.DiscountTotal, order.NetAmount));
        var item = Assert.Single(order.Items);
        Assert.Equal(("BOOK-1", 3, 10m, 30m), (item.CatalogSku, item.Quantity, item.UnitPrice, item.LineNet));
        Assert.Equal([("Cut", 1, StepStatus.Waiting), ("Fold", 2, StepStatus.Waiting)], item.Steps.Select(s => (s.JobTypeName, s.SortOrder, s.Status)));
        var placed = Assert.Single(await As(_employee.Id).EventsAsync(order.Id, default));
        Assert.Equal((OrderEventType.Placed, _employee.Id, "PendingApproval"), (placed.Type, placed.ActorUserId, placed.ToValue));
    }

    [Fact]
    public async Task Place_applies_the_catalog_then_client_discount_per_item_and_totals_the_order()
    {
        _catalog.CatalogDiscountPercent = 10m;
        _client.TierDiscountPercent = 5m;
        var second = _db.AddCatalog("BOOK-2", _db.AddJobType("Bind"));
        _db.SaveChanges();
        var due = DateOnly.FromDateTime(DateTime.UtcNow);
        var order = await As(_employee.Id).PlaceAsync(new PlaceOrderRequest
        {
            ClientId = _client.Id,
            Items = [new OrderItemRequest { CatalogId = _catalog.Id, Quantity = 3 }, new OrderItemRequest { CatalogId = second.Id, Quantity = 2 }],
            CommitmentDeadline = due,
            Reference = " PO-7 ",
            Notes = " Matt ",
        }, default);
        Assert.Equal([25.65m, 19m], order.Items.Select(i => i.LineNet));
        Assert.Equal((50m, 5.35m, 44.65m), (order.SubTotal, order.DiscountTotal, order.NetAmount));
        Assert.Equal((due, "PO-7", "Matt"), (order.CommitmentDeadline, order.Reference, order.Notes));
    }

    [Fact]
    public async Task Place_rejects_missing_unknown_duplicate_deactivated_and_past_input()
    {
        var service = As(_employee.Id);
        await Assert.ThrowsAsync<BadRequestException>(() => service.PlaceAsync(Place() with { ClientId = null }, default));
        await Assert.ThrowsAsync<BadRequestException>(() => service.PlaceAsync(Place(clientId: 9999), default));
        await Assert.ThrowsAsync<BadRequestException>(() => service.PlaceAsync(Place(catalogId: 9999), default));
        await Assert.ThrowsAsync<BadRequestException>(() => service.PlaceAsync(Place(priorityId: 9999), default));
        await Assert.ThrowsAsync<BadRequestException>(() => service.PlaceAsync(Place() with { Items = [.. Place().Items, .. Place().Items] }, default));
        await Assert.ThrowsAsync<BadRequestException>(() => service.PlaceAsync(Place() with { CommitmentDeadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1) }, default));
        _catalog.IsActive = false;
        _db.SaveChanges();
        await Assert.ThrowsAsync<BadRequestException>(() => service.PlaceAsync(Place(), default));
    }

    [Theory]
    [InlineData(ClientStatus.Suspended)]
    [InlineData(ClientStatus.Inactive)]
    public async Task Place_for_a_suspended_or_inactive_client_is_conflict(ClientStatus status)
    {
        _client.Status = status;
        _client.IsActive = status != ClientStatus.Inactive;
        _db.SaveChanges();
        await Assert.ThrowsAsync<ConflictException>(() => As(_employee.Id).PlaceAsync(Place(), default));
    }

    [Fact]
    public async Task Priorities_are_listed_by_rank()
    {
        var priorities = await As(_employee.Id).PrioritiesAsync(default);
        Assert.Equal(["Normal", "High", "Premium", "Ultra High"], priorities.Select(p => p.Name));
        Assert.Equal(0m, priorities[0].Cost);
    }

    [Fact]
    public async Task Client_user_orders_for_their_own_client_and_cannot_pick_another()
    {
        var clientUser = _db.AddUser("cli", [SystemIds.CustomersDepartment]);
        var own = _db.AddClient("own@test.local", clientUser.Id);
        var order = await As(clientUser.Id).PlaceAsync(Place(clientId: _client.Id), default);
        Assert.Equal(own.Id, order.ClientId);
    }

    [Fact]
    public async Task Client_user_without_a_linked_client_is_forbidden()
    {
        var clientUser = _db.AddUser("cli", [SystemIds.CustomersDepartment]);
        await Assert.ThrowsAsync<ForbiddenException>(() => As(clientUser.Id).PlaceAsync(Place(), default));
    }

    [Fact]
    public async Task Client_user_sees_only_their_own_orders_and_no_reasons()
    {
        var clientUser = _db.AddUser("cli", [SystemIds.CustomersDepartment]);
        var own = _db.AddClient("own@test.local", clientUser.Id);
        var mine = await As(clientUser.Id).PlaceAsync(Place(), default);
        var other = await As(_employee.Id).PlaceAsync(Place(), default);
        await As(_manager.Id).CancelAsync(mine.Id, new CancelOrderRequest { Reason = "Out of stock" }, default);

        Assert.Equal([mine.Id], (await As(clientUser.Id).ListAsync(default)).Select(o => o.Id));
        Assert.Equal(own.Id, (await As(clientUser.Id).GetAsync(mine.Id, default)).ClientId);
        await Assert.ThrowsAsync<NotFoundException>(() => As(clientUser.Id).GetAsync(other.Id, default));
        await Assert.ThrowsAsync<NotFoundException>(() => As(clientUser.Id).EventsAsync(other.Id, default));
        Assert.All(await As(clientUser.Id).EventsAsync(mine.Id, default), e => Assert.Null(e.Reason));
        Assert.Contains(await As(_manager.Id).EventsAsync(mine.Id, default), e => e.Reason == "Out of stock");
    }

    [Fact]
    public async Task Staff_list_all_newest_first_and_unknown_is_not_found()
    {
        var first = await As(_employee.Id).PlaceAsync(Place(), default);
        var second = await As(_employee.Id).PlaceAsync(Place(), default);
        Assert.Equal([second.Id, first.Id], (await As(_employee.Id).ListAsync(default)).Select(o => o.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => As(_employee.Id).GetAsync(9999, default));
    }

    [Fact]
    public async Task Cancel_returns_the_order_to_estimation_and_appends_an_event()
    {
        var order = await As(_employee.Id).PlaceAsync(Place(), default);
        var cancelled = await As(_manager.Id).CancelAsync(order.Id, new CancelOrderRequest { Reason = " Quality failure " }, default);
        Assert.Equal(OrderStatus.Cancelled, cancelled.Status);
        Assert.Equal(SystemIds.EstimationDepartment, cancelled.ReturnedToDepartmentId);
        var events = await As(_manager.Id).EventsAsync(order.Id, default);
        Assert.Equal([OrderEventType.Placed, OrderEventType.Cancelled], events.Select(e => e.Type));
        Assert.Equal(("PendingApproval", "Cancelled", "Quality failure", _manager.Id), (events[1].FromValue, events[1].ToValue, events[1].Reason, events[1].ActorUserId));
    }

    [Fact]
    public async Task Cancel_needs_a_manager_an_existing_order_and_an_open_order()
    {
        var order = await As(_employee.Id).PlaceAsync(Place(), default);
        var request = new CancelOrderRequest { Reason = "Client request" };
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_employee.Id).CancelAsync(order.Id, request, default));
        await Assert.ThrowsAsync<NotFoundException>(() => As(_manager.Id).CancelAsync(9999, request, default));
        await As(_manager.Id).CancelAsync(order.Id, request, default);
        await Assert.ThrowsAsync<ConflictException>(() => As(_manager.Id).CancelAsync(order.Id, request, default));
    }

    [Fact]
    public async Task Adjust_quote_is_for_estimation_and_admins_and_logs_the_change()
    {
        var order = await As(_employee.Id).PlaceAsync(Place(), default);
        var estimator = _db.AddUser("est", [SystemIds.EstimationDepartment], SystemIds.ManagerRole);
        var request = new AdjustQuoteRequest { Total = 20.505m, Reason = " Bulk rate " };
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_manager.Id).AdjustQuoteAsync(order.Id, request, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_employee.Id).AdjustQuoteAsync(order.Id, request, default));
        Assert.Equal(20.51m, (await As(estimator.Id).AdjustQuoteAsync(order.Id, request, default)).NetAmount);
        Assert.Equal(15m, (await As(TestDb.SuperAdminUser).AdjustQuoteAsync(order.Id, request with { Total = 15m }, default)).NetAmount);
        var logged = (await As(_manager.Id).EventsAsync(order.Id, default))[1];
        Assert.Equal((OrderEventType.QuoteAdjusted, "30.00", "20.51", "Bulk rate"), (logged.Type, logged.FromValue, logged.ToValue, logged.Reason));
    }

    [Fact]
    public async Task Adjust_quote_unknown_or_after_approval_is_rejected()
    {
        var order = await As(_employee.Id).PlaceAsync(Place(), default);
        var request = new AdjustQuoteRequest { Total = 1m, Reason = "Goodwill" };
        await Assert.ThrowsAsync<NotFoundException>(() => As(TestDb.SuperAdminUser).AdjustQuoteAsync(9999, request, default));
        await As(_manager.Id).CancelAsync(order.Id, new CancelOrderRequest { Reason = "Client request" }, default);
        await Assert.ThrowsAsync<ConflictException>(() => As(TestDb.SuperAdminUser).AdjustQuoteAsync(order.Id, request, default));
    }

    [Fact]
    public async Task Priority_request_appends_an_event_and_keeps_the_priority()
    {
        var order = await As(_employee.Id).PlaceAsync(Place(), default);
        var requested = await As(_manager.Id).RequestPriorityChangeAsync(order.Id, new PriorityChangeRequest { PriorityId = UltraHigh, Reason = "Rush" }, default);
        Assert.Equal((OrderEventType.PriorityChangeRequested, "Normal", "Ultra High", "Rush"), (requested.Type, requested.FromValue, requested.ToValue, requested.Reason));
        Assert.Equal("Normal", (await As(_manager.Id).GetAsync(order.Id, default)).PriorityName);
        Assert.Equal(2, (await As(_manager.Id).EventsAsync(order.Id, default)).Count);
    }

    [Fact]
    public async Task Priority_request_rejects_employees_same_or_unknown_priority_unknown_and_closed_orders()
    {
        var order = await As(_employee.Id).PlaceAsync(Place(), default);
        var high = new PriorityChangeRequest { PriorityId = High };
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_employee.Id).RequestPriorityChangeAsync(order.Id, high, default));
        await Assert.ThrowsAsync<BadRequestException>(() => As(_manager.Id).RequestPriorityChangeAsync(order.Id, new PriorityChangeRequest { PriorityId = SystemIds.NormalPriority }, default));
        await Assert.ThrowsAsync<BadRequestException>(() => As(_manager.Id).RequestPriorityChangeAsync(order.Id, new PriorityChangeRequest { PriorityId = 9999 }, default));
        await Assert.ThrowsAsync<NotFoundException>(() => As(_manager.Id).RequestPriorityChangeAsync(9999, high, default));
        await As(_manager.Id).CancelAsync(order.Id, new CancelOrderRequest { Reason = "Client request" }, default);
        await Assert.ThrowsAsync<ConflictException>(() => As(_manager.Id).RequestPriorityChangeAsync(order.Id, high, default));
    }

    [Fact]
    public async Task Unauthenticated_calls_are_unauthorized() =>
        await Assert.ThrowsAsync<UnauthorizedException>(() => new OrderService(_db, new CurrentUser(new FakeUserId(null), _db)).ListAsync(default));
}
