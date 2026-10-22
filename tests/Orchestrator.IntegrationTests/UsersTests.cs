namespace Orchestrator.IntegrationTests;

public class UsersTests(ApiFixture api)
{
    private static object Body(string? email = null, string[]? roles = null, int[]? departments = null) => new
    {
        firstName = "Ann",
        lastName = "Lee",
        email = email ?? $"u{ApiFixture.Unique()}@test.local",
        mobile = "+0123456789",
        departmentIds = departments,
        roles,
    };

    private static object Profile(string? email = null, bool isActive = true) => new
    {
        firstName = "Changed",
        lastName = "Name",
        email = email ?? $"c{ApiFixture.Unique()}@test.local",
        mobile = "0987654321",
        isActive,
    };

    private async Task<(int Manager, int Employee, int Department)> Team()
    {
        var department = await api.NewDepartment("Employee");
        return (await api.NewManager(department), await api.NewUser([department]), department);
    }

    [Fact]
    public async Task List_contains_seeded_and_new_users()
    {
        var id = await api.NewEmployee();
        var reply = await api.Get("users");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Contains(ApiFixture.SuperAdmin, reply.Ids);
        Assert.Contains(id, reply.Ids);
    }

    [Fact]
    public async Task Get_returns_the_user_with_departments_and_roles()
    {
        var reply = await api.Get("users/1");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal("superadmin@orchestrator.local", reply.Str("email"));
        Assert.Equal(["Super Admin"], reply["departments"].EnumerateArray().Select(d => d.GetProperty("name").GetString()));
        Assert.Equal(["Super Admin"], reply["departmentRoles"].EnumerateArray().Select(d => d.GetString()));
        Assert.Empty(reply["roles"].EnumerateArray());
    }

    [Fact]
    public async Task Get_unknown_is_404() =>
        ProblemAssert.Is(await api.Get("users/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);

    [Fact]
    public async Task Create_without_departments_lands_in_unassigned()
    {
        var reply = await api.Post("users", Body());
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/users/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.Equal(["Unassigned"], reply["departments"].EnumerateArray().Select(d => d.GetProperty("name").GetString()));
        Assert.True(reply["isActive"].GetBoolean());
    }

    [Fact]
    public async Task Create_with_departments_and_roles()
    {
        var department = await api.NewDepartment();
        var reply = await api.Post("users", Body(roles: ["Manager"], departments: [department]));
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.Equal([department], reply["departments"].EnumerateArray().Select(d => d.GetProperty("id").GetInt32()));
        Assert.Equal(["Manager"], reply["roles"].EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Create_without_an_email_is_allowed()
    {
        var reply = await api.Post("users", new { firstName = "No", lastName = "Mail", mobile = "0123456789" });
        Assert.Equal(HttpStatusCode.Created, reply.Status);
    }

    [Fact]
    public async Task Create_duplicate_email_ignoring_case_is_409()
    {
        var email = $"dup{ApiFixture.Unique()}@test.local";
        await api.Post("users", Body(email));
        ProblemAssert.Is(await api.Post("users", Body(email.ToUpperInvariant())), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_with_invalid_fields_is_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("users", new { firstName = "A", lastName = "Lee", mobile = "0123456789" })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("users", new { firstName = "Ann", lastName = "Lee", mobile = "abc" })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("users", new { firstName = "Ann", lastName = "Lee", mobile = "0123456789", email = "nope" })).Status);
    }

    [Fact]
    public async Task Create_with_unknown_department_or_role_is_400()
    {
        ProblemAssert.Is(await api.Post("users", Body(departments: [ApiFixture.UnknownId])), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Post("users", Body(roles: ["Nope" + ApiFixture.Unique()])), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_in_a_deactivated_department_is_400()
    {
        var department = await api.NewDepartment();
        await api.Post($"departments/{department}/deactivate");
        ProblemAssert.Is(await api.Post("users", Body(departments: [department])), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_needs_a_known_acting_user_of_manager_rank_or_above()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Post("users", Body(), null)).Status);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Post("users", Body(), ApiFixture.UnknownId)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post("users", Body(), await api.NewEmployee())).Status);
        Assert.Equal(HttpStatusCode.Created, (await api.Post("users", Body(), ApiFixture.Admin)).Status);
    }

    [Fact]
    public async Task Admin_cannot_create_an_admin_or_use_the_admin_department()
    {
        ProblemAssert.Is(await api.Post("users", Body(roles: ["Admin"]), ApiFixture.Admin), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Post("users", Body(departments: [2]), ApiFixture.Admin), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Super_admin_can_create_an_admin()
    {
        var reply = await api.Post("users", Body(roles: ["Admin"]));
        Assert.Equal(HttpStatusCode.Created, reply.Status);
    }

    [Fact]
    public async Task Manager_creates_into_own_departments_by_default()
    {
        var (manager, _, department) = await Team();
        var reply = await api.Post("users", Body(), manager);
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.Equal([department], reply["departments"].EnumerateArray().Select(d => d.GetProperty("id").GetInt32()));
    }

    [Fact]
    public async Task Manager_cannot_use_departments_they_do_not_manage()
    {
        var (manager, _, _) = await Team();
        ProblemAssert.Is(await api.Post("users", Body(departments: [await api.NewDepartment()]), manager), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_can_give_roles_of_their_department_but_not_manager()
    {
        var (manager, _, _) = await Team();
        var allowed = await api.Post("users", Body(roles: ["Employee"]), manager);
        Assert.Equal(HttpStatusCode.Created, allowed.Status);
        ProblemAssert.Is(await api.Post("users", Body(roles: ["Manager"]), manager), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_changes_the_profile()
    {
        var id = await api.NewEmployee();
        var reply = await api.Put($"users/{id}", Profile("changed@test.local" + ApiFixture.Unique()[..0]));
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal("Changed", reply.Str("firstName"));
        Assert.Equal("0987654321", reply.Str("mobile"));
    }

    [Fact]
    public async Task Update_with_is_active_false_deactivates_and_the_user_can_no_longer_act()
    {
        var id = await api.NewUser(roles: ["Manager"]);
        var reply = await api.Put($"users/{id}", Profile(isActive: false));
        Assert.False(reply["isActive"].GetBoolean());
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Post("users", Body(), id)).Status);
    }

    [Fact]
    public async Task Update_to_a_used_email_is_409()
    {
        var email = $"taken{ApiFixture.Unique()}@test.local";
        await api.Post("users", Body(email));
        ProblemAssert.Is(await api.Put($"users/{await api.NewEmployee()}", Profile(email)), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_unknown_is_404() =>
        ProblemAssert.Is(await api.Put($"users/{ApiFixture.UnknownId}", Profile()), HttpStatusCode.NotFound);

    [Fact]
    public async Task Update_with_invalid_body_is_400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"users/{await api.NewEmployee()}", new { firstName = "A" })).Status);

    [Fact]
    public async Task Nobody_edits_a_user_at_or_above_their_rank()
    {
        ProblemAssert.Is(await api.Put($"users/{ApiFixture.SuperAdmin}", Profile(), ApiFixture.Admin), HttpStatusCode.Forbidden);
        ProblemAssert.Is(await api.Put($"users/{ApiFixture.Admin}", Profile(), ApiFixture.Admin), HttpStatusCode.Forbidden);
        var (manager, _, _) = await Team();
        ProblemAssert.Is(await api.Put($"users/{manager}", Profile(), manager), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manager_edits_only_users_in_their_own_departments()
    {
        var (manager, employee, _) = await Team();
        Assert.Equal(HttpStatusCode.OK, (await api.Put($"users/{employee}", Profile(), manager)).Status);

        var (_, outsider, _) = await Team();
        ProblemAssert.Is(await api.Put($"users/{outsider}", Profile(), manager), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_needs_a_known_acting_user_of_manager_rank_or_above()
    {
        var id = await api.NewEmployee();
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Put($"users/{id}", Profile(), null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Put($"users/{id}", Profile(), await api.NewEmployee())).Status);
    }

    [Fact]
    public async Task Assign_roles_replaces_extra_roles()
    {
        var id = await api.NewEmployee();
        var set = await api.Put($"users/{id}/roles", new { roles = new[] { "Manager" } });
        Assert.Equal(["Manager"], set["roles"].EnumerateArray().Select(r => r.GetString()));
        var cleared = await api.Put($"users/{id}/roles", new { roles = Array.Empty<string>() });
        Assert.Empty(cleared["roles"].EnumerateArray());
    }

    [Fact]
    public async Task Assign_roles_rejects_unknown_roles_users_and_rights()
    {
        var id = await api.NewEmployee();
        ProblemAssert.Is(await api.Put($"users/{id}/roles", new { roles = new[] { "Nope" + ApiFixture.Unique() } }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Put($"users/{ApiFixture.UnknownId}/roles", new { roles = Array.Empty<string>() }), HttpStatusCode.NotFound);
        ProblemAssert.Is(await api.Put($"users/{id}/roles", new { roles = new[] { "Admin" } }, ApiFixture.Admin), HttpStatusCode.Forbidden);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Put($"users/{id}/roles", new { roles = Array.Empty<string>() }, null)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"users/{id}/roles", new { })).Status);
    }

    [Fact]
    public async Task Manager_assigning_roles_keeps_the_roles_they_may_not_touch()
    {
        var (manager, employee, _) = await Team();
        await api.Put($"users/{employee}/roles", new { roles = new[] { "Client" } });

        var added = await api.Put($"users/{employee}/roles", new { roles = new[] { "Employee" } }, manager);
        Assert.Equal(["Client", "Employee"], added["roles"].EnumerateArray().Select(r => r.GetString()));

        var cleared = await api.Put($"users/{employee}/roles", new { roles = Array.Empty<string>() }, manager);
        Assert.Equal(["Client"], cleared["roles"].EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Assign_departments_replaces_them_and_empty_means_unassigned()
    {
        var id = await api.NewEmployee();
        var department = await api.NewDepartment();
        var set = await api.Put($"users/{id}/departments", new { departmentIds = new[] { department } });
        Assert.Equal([department], set["departments"].EnumerateArray().Select(d => d.GetProperty("id").GetInt32()));

        var cleared = await api.Put($"users/{id}/departments", new { departmentIds = Array.Empty<int>() });
        Assert.Equal(["Unassigned"], cleared["departments"].EnumerateArray().Select(d => d.GetProperty("name").GetString()));
    }

    [Fact]
    public async Task Assign_departments_rejects_unknown_departments_users_and_rights()
    {
        var id = await api.NewEmployee();
        ProblemAssert.Is(await api.Put($"users/{id}/departments", new { departmentIds = new[] { ApiFixture.UnknownId } }), HttpStatusCode.BadRequest);
        ProblemAssert.Is(await api.Put($"users/{ApiFixture.UnknownId}/departments", new { departmentIds = Array.Empty<int>() }), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Put($"users/{id}/departments", new { departmentIds = Array.Empty<int>() }, null)).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"users/{id}/departments", new { })).Status);
    }

    [Fact]
    public async Task Manager_assigning_departments_keeps_those_they_do_not_manage()
    {
        var (manager, employee, department) = await Team();
        var other = await api.NewDepartment();
        await api.Put($"users/{employee}/departments", new { departmentIds = new[] { department, other } });

        var reply = await api.Put($"users/{employee}/departments", new { departmentIds = Array.Empty<int>() }, manager);
        Assert.Equal([other], reply["departments"].EnumerateArray().Select(d => d.GetProperty("id").GetInt32()));

        ProblemAssert.Is(await api.Put($"users/{employee}/departments", new { departmentIds = new[] { await api.NewDepartment() } }, manager), HttpStatusCode.Forbidden);
    }
}
