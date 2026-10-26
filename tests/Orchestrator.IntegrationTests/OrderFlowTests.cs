namespace Orchestrator.IntegrationTests;

public class OrderFlowTests(ApiFixture api)
{
    private const int Accounting = 3;
    private const int QualityCheck = 4;
    private const int Operations = 9;

    private async Task<Reply> Place(int[] chain, int?[]? standardMinutes = null)
    {
        var catalog = await api.Post("catalogs", new
        {
            sku = "SKU-" + ApiFixture.Unique(),
            name = "Flow catalog",
            jobTypeIds = new[] { await api.NewJobType(), await api.NewJobType() },
            standardMinutes,
        });
        Assert.Equal(HttpStatusCode.Created, catalog.Status);
        Assert.Equal(HttpStatusCode.OK, (await api.Post($"catalogs/{catalog.Id}/approve")).Status);
        if (chain.Length > 0)
        {
            Assert.Equal(HttpStatusCode.Created, (await api.Post("approval-chains", new { name = "Chain " + ApiFixture.Unique(), catalogId = catalog.Id, departmentIds = chain })).Status);
        }

        var order = await api.Post("orders", new { clientId = await api.NewClient(), items = new[] { new { catalogId = catalog.Id, quantity = 2 } } });
        Assert.Equal(HttpStatusCode.Created, order.Status);
        return order;
    }

    private static int StepId(Reply order, int index) => order["items"][0].GetProperty("steps")[index].GetProperty("id").GetInt32();

    private async Task<(int Order, int First, int Second)> Running()
    {
        var order = await Place([]);
        return (order.Id, StepId(order, 0), StepId(order, 1));
    }

    private Task<Reply> Step(int id, string action, int actor, object? body = null) => api.Post($"order-steps/{id}/{action}", body, actor);

    private async Task<string> StatusOf(int step, int actor) => (await api.Get($"order-steps/{step}", actor)).Str("status");

    [Fact]
    public async Task Without_approval_stages_the_order_starts_at_once_and_the_first_step_is_ready()
    {
        var order = await Place([]);
        Assert.Equal("InProgress", order.Str("status"));
        Assert.Equal(["Ready", "Waiting"], order["items"][0].GetProperty("steps").EnumerateArray().Select(s => s.GetProperty("status").GetString()));
        Assert.Empty((await api.Get($"orders/{order.Id}/approvals")).Items);
        Assert.Equal(["Placed", "ApprovalApproved"], (await api.Get($"orders/{order.Id}/events")).Names("type"));
    }

    [Fact]
    public async Task Approvals_run_in_order_and_the_last_one_starts_the_assembly_line()
    {
        var order = await Place([Accounting, QualityCheck]);
        Assert.Equal("PendingApproval", order.Str("status"));
        Assert.Equal(["Accounting", "Quality Check"], (await api.Get($"orders/{order.Id}/approvals")).Names("departmentName"));
        var accounting = await api.NewUser([Accounting], ["Manager"]);
        var quality = await api.NewUser([QualityCheck], ["Manager"]);

        ProblemAssert.Is(await api.Post($"orders/{order.Id}/approvals/approve", null, quality), HttpStatusCode.Forbidden);
        var first = await api.Post($"orders/{order.Id}/approvals/approve", null, accounting);
        Assert.Equal("PendingApproval", first.Str("status"));
        Assert.Equal("Waiting", first["items"][0].GetProperty("steps")[0].GetProperty("status").GetString());

        var last = await api.Post($"orders/{order.Id}/approvals/approve", null, quality);
        Assert.Equal("InProgress", last.Str("status"));
        Assert.Equal("Ready", last["items"][0].GetProperty("steps")[0].GetProperty("status").GetString());
        Assert.Equal(["Approved", "Approved"], (await api.Get($"orders/{order.Id}/approvals")).Names("decision"));
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/approvals/approve", null, quality), HttpStatusCode.Conflict);
        Assert.Equal(["Placed", "ApprovalApproved", "ApprovalApproved"], (await api.Get($"orders/{order.Id}/events")).Names("type"));
    }

    [Fact]
    public async Task Deny_cancels_the_order_back_to_estimation_and_needs_a_reason()
    {
        var order = await Place([Accounting]);
        var accounting = await api.NewUser([Accounting], ["Manager"]);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/approvals/deny", new { reason = "" }, accounting), HttpStatusCode.BadRequest);
        var denied = await api.Post($"orders/{order.Id}/approvals/deny", new { reason = "Credit limit exceeded" }, accounting);
        Assert.Equal(("Cancelled", 8), (denied.Str("status"), denied["returnedToDepartmentId"].GetInt32()));
        Assert.Equal(["Denied"], (await api.Get($"orders/{order.Id}/approvals")).Names("decision"));
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/approvals/approve", null, accounting), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Employees_clients_and_unknown_orders_cannot_decide_approvals()
    {
        var order = await Place([Accounting]);
        ProblemAssert.Is(await api.Post($"orders/{order.Id}/approvals/approve", null, await api.NewUser([Accounting])), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Post($"orders/{ApiFixture.UnknownId}/approvals/approve"), HttpStatusCode.NotFound);
        ProblemAssert.Is(await api.Get($"orders/{ApiFixture.UnknownId}/approvals"), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.OK, (await api.Post($"orders/{order.Id}/approvals/approve")).Status);
    }

    [Fact]
    public async Task A_step_runs_from_assignment_to_completion_and_the_next_step_becomes_ready()
    {
        var (orderId, first, second) = await Running();
        var employee = await api.NewUser([Operations]);
        var queue = await api.Get("order-steps?status=Ready", employee);
        Assert.True(queue.Status == HttpStatusCode.OK, queue.Raw);
        Assert.Contains(first, queue.Ids);
        Assert.DoesNotContain(second, queue.Ids);

        Assert.Equal(employee, (await Step(first, "assign", employee, new { userId = employee }))["assignedUserId"].GetInt32());
        Assert.Equal("InProgress", (await Step(first, "start", employee)).Str("status"));
        Assert.Equal(40, (await Step(first, "progress", employee, new { progress = 40, note = "Half cut" }))["progress"].GetInt32());
        Assert.Equal("OnHold", (await Step(first, "hold", employee, new { reason = "Machine fault" })).Str("status"));
        Assert.Equal("InProgress", (await Step(first, "resume", employee)).Str("status"));
        var done = await Step(first, "complete", employee);
        Assert.Equal(("Completed", 100, employee), (done.Str("status"), done["progress"].GetInt32(), done["completedByUserId"].GetInt32()));
        Assert.True(done["workMinutes"].GetDouble() >= 0);
        Assert.Equal("Ready", await StatusOf(second, employee));
        Assert.Equal(["Ready", "Assigned", "Started", "Progress", "Held", "Resumed", "Completed"], (await api.Get($"order-steps/{first}/logs", employee)).Names("entryType"));

        await Step(second, "assign", employee, new { userId = employee });
        await Step(second, "start", employee);
        await Step(second, "complete", employee);
        var order = await api.Get($"orders/{orderId}");
        Assert.Equal("Completed", order.Str("status"));
        Assert.Equal(["Placed", "ApprovalApproved", "Completed"], (await api.Get($"orders/{orderId}/events")).Names("type"));
    }

    [Fact]
    public async Task Assignment_follows_the_department_rules()
    {
        var (_, first, _) = await Running();
        var manager = await api.NewUser([Operations], ["Manager"]);
        var employee = await api.NewUser([Operations]);
        var colleague = await api.NewUser([Operations]);
        var outsider = await api.NewUser([Accounting]);

        ProblemAssert.Is(await Step(first, "assign", employee, new { userId = colleague }), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await Step(first, "assign", manager, new { userId = outsider }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await Step(first, "assign", outsider, new { userId = outsider }), HttpStatusCode.NotFound);
        Assert.Equal(colleague, (await Step(first, "assign", manager, new { userId = colleague }))["assignedUserId"].GetInt32());
        ProblemAssert.Is(await Step(first, "start", employee), HttpStatusCode.Forbidden);
        Assert.Equal("InProgress", (await Step(first, "start", manager)).Str("status"));
        ProblemAssert.Is(await Step(first, "progress", employee, new { progress = 10 }), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await Step(first, "progress", colleague, new { progress = 100 }), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Steps_reject_wrong_states_waiting_steps_clients_and_cancelled_orders()
    {
        var (orderId, first, second) = await Running();
        var manager = await api.NewUser([Operations], ["Manager"]);
        ProblemAssert.Is(await Step(second, "start", manager), HttpStatusCode.Conflict);
        ProblemAssert.Is(await Step(first, "complete", manager), HttpStatusCode.Conflict);
        ProblemAssert.Is(await Step(first, "resume", manager), HttpStatusCode.Conflict);
        ProblemAssert.Is(await Step(first, "hold", manager, new { reason = "Why" }), HttpStatusCode.Conflict);
        ProblemAssert.Is(await Step(ApiFixture.UnknownId, "start", manager), HttpStatusCode.NotFound);
        ProblemAssert.Is(await api.Get("order-steps", await api.NewUser([6])), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Get("order-steps", actor: null), HttpStatusCode.Unauthorized);

        await api.Post($"orders/{orderId}/cancel", new { reason = "Client request" });
        ProblemAssert.Is(await Step(first, "start", manager), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reject_sends_the_work_back_and_the_first_step_cannot_reject()
    {
        var (_, first, second) = await Running();
        var manager = await api.NewUser([Operations], ["Manager"]);
        await Step(first, "start", manager);
        ProblemAssert.Is(await Step(first, "reject", manager, new { reason = "Nothing before" }), HttpStatusCode.Conflict);
        await Step(first, "complete", manager);
        await Step(second, "start", manager);

        ProblemAssert.Is(await Step(second, "reject", manager, new { reason = "" }), HttpStatusCode.BadRequest);
        var rejected = await Step(second, "reject", manager, new { reason = "Scratched surface" });
        Assert.Equal(("Waiting", true), (rejected.Str("status"), rejected["assignedUserId"].ValueKind == System.Text.Json.JsonValueKind.Null));
        Assert.Equal("Ready", await StatusOf(first, manager));
        Assert.Contains("Rejected", (await api.Get($"order-steps/{second}/logs", manager)).Names("entryType"));
        await Step(first, "start", manager);
        await Step(first, "complete", manager);
        Assert.Equal("Ready", await StatusOf(second, manager));
    }

    [Fact]
    public async Task The_queue_is_limited_to_the_callers_departments_and_filters_apply()
    {
        var (_, first, _) = await Running();
        var employee = await api.NewUser([Operations]);
        var outsider = await api.NewUser([Accounting]);
        Assert.Contains(first, (await api.Get("order-steps", employee)).Ids);
        Assert.DoesNotContain(first, (await api.Get("order-steps", outsider)).Ids);
        Assert.Contains(first, (await api.Get($"order-steps?departmentId={Operations}")).Ids);
        Assert.DoesNotContain(first, (await api.Get("order-steps?status=InProgress")).Ids);
        await Step(first, "assign", employee, new { userId = employee });
        Assert.Equal([first], (await api.Get("order-steps?assignedToMe=true", employee)).Ids);
        ProblemAssert.Is(await api.Get($"order-steps/{first}", outsider), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Steps_copy_the_job_type_department_and_the_catalog_standard_minutes()
    {
        var jobType = await api.Post("job-types", new { name = "Audit " + ApiFixture.Unique(), departmentId = Accounting });
        Assert.Equal(Accounting, jobType["departmentId"].GetInt32());
        var catalog = await api.Post("catalogs", new { sku = "SKU-" + ApiFixture.Unique(), name = "Audit", jobTypeIds = new[] { jobType.Id, await api.NewJobType() }, standardMinutes = new int?[] { 45, null } });
        Assert.Equal(45, catalog["steps"][0].GetProperty("standardMinutes").GetInt32());
        await api.Post($"catalogs/{catalog.Id}/approve");
        var order = await api.Post("orders", new { clientId = await api.NewClient(), items = new[] { new { catalogId = catalog.Id, quantity = 1 } } });
        var accounting = await api.NewUser([Accounting]);
        var step = await api.Get($"order-steps/{StepId(order, 0)}", accounting);
        Assert.Equal((Accounting, "Accounting", 45), (step["departmentId"].GetInt32(), step.Str("departmentName"), step["standardMinutes"].GetInt32()));
        Assert.Equal(System.Text.Json.JsonValueKind.Null, (await api.Get($"order-steps/{StepId(order, 1)}"))["standardMinutes"].ValueKind);
    }

    [Fact]
    public async Task Catalog_standard_minutes_must_match_the_steps()
    {
        var steps = new[] { await api.NewJobType(), await api.NewJobType() };
        ProblemAssert.Is(await api.Post("catalogs", new { sku = "SKU-" + ApiFixture.Unique(), name = "Bad", jobTypeIds = steps, standardMinutes = new int?[] { 10 } }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("catalogs", new { sku = "SKU-" + ApiFixture.Unique(), name = "Bad", jobTypeIds = steps, standardMinutes = new int?[] { 10, 0 } }), HttpStatusCode.BadRequest);
    }
}
