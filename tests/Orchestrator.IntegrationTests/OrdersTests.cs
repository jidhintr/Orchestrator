namespace Orchestrator.IntegrationTests;

public class OrdersTests(ApiFixture api)
{
    private static object[] Lines(params (int Catalog, int Quantity)[] lines) => [.. lines.Select(l => new { catalogId = l.Catalog, quantity = l.Quantity })];

    private async Task<int> Catalog(params int[] departments)
    {
        var id = await api.NewCatalog();
        Assert.Equal(HttpStatusCode.Created, (await api.Post("approval-chains", new { name = "Chain " + ApiFixture.Unique(), catalogId = id, departmentIds = departments.Length == 0 ? new[] { 3 } : departments })).Status);
        return id;
    }

    private async Task<Reply> NewOrder(int priorityId = 1, int? actor = ApiFixture.SuperAdmin)
    {
        var reply = await api.Post("orders", new { clientId = await api.NewClient(), items = Lines((await Catalog(), 5)), priorityId }, actor);
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        return reply;
    }

    private async Task<(int User, int Client)> ClientUser()
    {
        var user = await api.NewUser([6]);
        var linked = await api.Post("clients", ApiFixture.ClientBody($"cu{ApiFixture.Unique()}@acme.test", userId: user));
        Assert.Equal(HttpStatusCode.Created, linked.Status);
        return (user, linked.Id);
    }

    [Fact]
    public async Task Priorities_are_listed_by_rank_with_their_cost()
    {
        var reply = await api.Get("priorities");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal(["Normal", "High", "Premium", "Ultra High"], reply.Names());
        Assert.Equal(0m, reply.Items[0].GetProperty("cost").GetDecimal());
        ProblemAssert.Is(await api.Get("priorities", actor: null), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Place_returns_201_with_items_job_steps_priority_cost_and_a_placed_event()
    {
        var first = await api.NewCatalog(await api.NewJobType(), await api.NewJobType());
        var second = await Catalog();
        var reply = await api.Post("orders", new { clientId = await api.NewClient(), items = Lines((first, 5), (second, 2)), priorityId = 3 });
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/orders/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.True(reply.Id >= 1000);
        Assert.Equal(("PendingApproval", "Premium"), (reply.Str("status"), reply.Str("priorityName")));
        Assert.Equal((700m, 35m, 250m, 915m), (reply["subTotal"].GetDecimal(), reply["discountTotal"].GetDecimal(), reply["priorityCost"].GetDecimal(), reply["netAmount"].GetDecimal()));
        var items = reply["items"].EnumerateArray().ToList();
        Assert.Equal([2, 1], items.Select(i => i.GetProperty("steps").GetArrayLength()));
        Assert.Equal([475m, 190m], items.Select(i => i.GetProperty("lineNet").GetDecimal()));
        Assert.Equal((100m, 5m, 0m), (items[0].GetProperty("unitPrice").GetDecimal(), items[0].GetProperty("catalogDiscountPercent").GetDecimal(), items[0].GetProperty("clientDiscountPercent").GetDecimal()));
        Assert.All(items.SelectMany(i => i.GetProperty("steps").EnumerateArray()), s => Assert.Equal("Waiting", s.GetProperty("status").GetString()));

        var events = await api.Get($"orders/{reply.Id}/events");
        Assert.Equal(["Placed"], events.Names("type"));
        Assert.Equal(ApiFixture.SuperAdmin, events.Items[0].GetProperty("actorUserId").GetInt32());
    }

    [Fact]
    public async Task Place_snapshots_the_client_tier_discount_and_the_optional_fields()
    {
        var gold = (await api.Post("clients", ApiFixture.ClientBody(tier: "Gold"))).Id;
        var due = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(7).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var reply = await api.Post("orders", new { clientId = gold, items = Lines((await Catalog(), 5)), commitmentDeadline = due, reference = " PO-1 ", notes = "Matt finish" });
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.Equal(451.25m, reply["netAmount"].GetDecimal());
        Assert.Equal(5m, reply["items"][0].GetProperty("clientDiscountPercent").GetDecimal());
        Assert.Equal((due, "PO-1", "Matt finish"), (reply.Str("commitmentDeadline"), reply.Str("reference"), reply.Str("notes")));
    }

    [Fact]
    public async Task List_and_get_return_the_order()
    {
        var id = (await NewOrder()).Id;
        Assert.Contains(id, (await api.Get("orders")).Ids);
        Assert.Equal(id, (await api.Get($"orders/{id}")).Id);
        ProblemAssert.Is(await api.Get("orders/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);
        ProblemAssert.Is(await api.Get($"orders/{ApiFixture.UnknownId}/events"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Place_rejects_bad_input()
    {
        var client = await api.NewClient();
        var catalog = await Catalog();
        var items = Lines((catalog, 5));
        ProblemAssert.Is(await api.Post("orders", new { items }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("orders", new { clientId = ApiFixture.UnknownId, items }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("orders", new { clientId = client, items = Lines((ApiFixture.UnknownId, 5)) }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("orders", new { clientId = client, items = Lines((catalog, 5), (catalog, 1)) }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("orders", new { clientId = client, items, priorityId = ApiFixture.UnknownId }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("orders", new { clientId = client }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("orders", new { clientId = client, items = Array.Empty<object>() }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("orders", new { clientId = client, items = Lines((catalog, 0)) }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("orders", new { clientId = client, items, commitmentDeadline = "2020-01-01" }), HttpStatusCode.BadRequest);
        await api.Post($"catalogs/{catalog}/deactivate");
        ProblemAssert.Is(await api.Post("orders", new { clientId = client, items }), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Place_for_a_suspended_client_is_409()
    {
        var client = await api.NewClient();
        await api.Put($"clients/{client}/status", new { status = "Suspended" });
        ProblemAssert.Is(await api.Post("orders", new { clientId = client, items = Lines((await Catalog(), 1)) }), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Orders_need_a_signed_in_user() =>
        ProblemAssert.Is(await api.Get("orders", actor: null), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Client_user_places_for_their_own_client_and_sees_only_their_orders()
    {
        var (user, client) = await ClientUser();
        var mine = await api.Post("orders", new { clientId = await api.NewClient(), items = Lines((await Catalog(), 5)) }, user);
        Assert.Equal(HttpStatusCode.Created, mine.Status);
        Assert.Equal(client, mine["clientId"].GetInt32());

        var other = await NewOrder();
        Assert.Equal([mine.Id], (await api.Get("orders", user)).Ids);
        ProblemAssert.Is(await api.Get($"orders/{other.Id}", user), HttpStatusCode.NotFound);
        ProblemAssert.Is(await api.Get($"orders/{other.Id}/events", user), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Client_user_cannot_cancel_or_request_priority_and_does_not_see_reasons()
    {
        var (user, _) = await ClientUser();
        var order = await api.Post("orders", new { items = Lines((await Catalog(), 5)) }, user);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/cancel", new { reason = "Changed my mind" }, user), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/priority-requests", new { priorityId = 2 }, user), HttpStatusCode.Forbidden);

        await api.Post($"orders/{order.Id}/cancel", new { reason = "Internal note" });
        Assert.All((await api.Get($"orders/{order.Id}/events", user)).Items, e => Assert.Equal(System.Text.Json.JsonValueKind.Null, e.GetProperty("reason").ValueKind));
        Assert.Equal("Internal note", (await api.Get($"orders/{order.Id}/events")).Items[^1].GetProperty("reason").GetString());
    }

    [Fact]
    public async Task Client_user_without_a_linked_client_is_403()
    {
        var user = await api.NewUser([6]);
        ProblemAssert.Is(await api.Post("orders", new { items = Lines((await Catalog(), 5)) }, user), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Linking_a_user_to_two_clients_is_409_and_an_unknown_user_is_400()
    {
        var (user, _) = await ClientUser();
        ProblemAssert.Is(await api.Post("clients", ApiFixture.ClientBody(userId: user)), HttpStatusCode.Conflict);
        ProblemAssert.Is(await api.Post("clients", ApiFixture.ClientBody(userId: ApiFixture.UnknownId)), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Manager_cancels_the_order_back_to_estimation()
    {
        var order = await NewOrder();
        var manager = await api.NewManager(await api.NewDepartment("Employee"));
        var cancelled = await api.Post($"orders/{order.Id}/cancel", new { reason = "Stock deficit" }, manager);
        Assert.Equal(HttpStatusCode.OK, cancelled.Status);
        Assert.Equal("Cancelled", cancelled.Str("status"));
        Assert.Equal(8, cancelled["returnedToDepartmentId"].GetInt32());
        Assert.Equal(["Placed", "Cancelled"], (await api.Get($"orders/{order.Id}/events")).Names("type"));
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/cancel", new { reason = "Again" }, manager), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_rejects_employees_missing_reason_and_unknown_orders()
    {
        var order = await NewOrder();
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/cancel", new { reason = "Quality" }, await api.NewEmployee()), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/cancel", new { reason = "" }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post($"orders/{ApiFixture.UnknownId}/cancel", new { reason = "Quality" }), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Priority_request_appends_an_event_and_keeps_the_priority()
    {
        var order = await NewOrder();
        var reply = await api.Post($"orders/{order.Id}/priority-requests", new { priorityId = 4, reason = "Rush job" });
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.Equal(("PriorityChangeRequested", "Normal", "Ultra High"), (reply.Str("type"), reply.Str("fromValue"), reply.Str("toValue")));
        Assert.Equal("Normal", (await api.Get($"orders/{order.Id}")).Str("priorityName"));
        Assert.Equal(["Placed", "PriorityChangeRequested"], (await api.Get($"orders/{order.Id}/events")).Names("type"));
    }

    [Fact]
    public async Task Priority_request_rejects_same_priority_employees_unknown_and_cancelled_orders()
    {
        var order = await NewOrder();
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/priority-requests", new { priorityId = 1 }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/priority-requests", new { priorityId = ApiFixture.UnknownId }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/priority-requests", new { priorityId = 2 }, await api.NewEmployee()), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Post($"orders/{ApiFixture.UnknownId}/priority-requests", new { priorityId = 2 }), HttpStatusCode.NotFound);
        await api.Post($"orders/{order.Id}/cancel", new { reason = "Client request" });
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/priority-requests", new { priorityId = 2 }), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Estimation_adjusts_the_quote_and_it_is_logged()
    {
        var order = await NewOrder();
        var estimator = await api.NewUser([8], ["Manager"]);
        var adjusted = await api.Post($"orders/{order.Id}/quote", new { total = 400.555m, reason = "Bulk rate" }, estimator);
        Assert.Equal(HttpStatusCode.OK, adjusted.Status);
        Assert.Equal(400.56m, adjusted["netAmount"].GetDecimal());
        var logged = (await api.Get($"orders/{order.Id}/events")).Items[^1];
        Assert.Equal(("QuoteAdjusted", "475.00", "400.56"), (logged.GetProperty("type").GetString(), logged.GetProperty("fromValue").GetString(), logged.GetProperty("toValue").GetString()));
    }

    [Fact]
    public async Task Only_estimation_or_admins_adjust_quotes_and_only_before_approval()
    {
        var order = await NewOrder();
        var body = new { total = 100, reason = "Goodwill" };
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/quote", body, await api.NewManager(await api.NewDepartment("Employee"))), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/quote", body, await api.NewEmployee()), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/quote", new { total = -1, reason = "Goodwill" }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post($"orders/{ApiFixture.UnknownId}/quote", body), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.OK, (await api.Post($"orders/{order.Id}/quote", body)).Status);
        await api.Post($"orders/{order.Id}/cancel", new { reason = "Client request" });
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/quote", body), HttpStatusCode.Conflict);
    }
}
