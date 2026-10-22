namespace Orchestrator.IntegrationTests;

public class RolesTests(ApiFixture api)
{
    private static object Body(string? name = null, string description = "Does things") => new { name = name ?? "Role" + ApiFixture.Unique(), description };

    [Fact]
    public async Task List_contains_the_five_system_roles_in_name_order()
    {
        var reply = await api.Get("roles");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        var names = reply.Names();
        var system = names.Where(n => n is "Admin" or "Client" or "Employee" or "Manager" or "Super Admin").ToList();
        Assert.Equal(["Admin", "Client", "Employee", "Manager", "Super Admin"], system);
        Assert.All(reply.Items.Where(i => system.Contains(i.GetProperty("name").GetString()!)), i => Assert.True(i.GetProperty("isSystem").GetBoolean()));
    }

    [Fact]
    public async Task Get_returns_a_role()
    {
        var reply = await api.Get("roles/3");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal("Manager", reply.Str("name"));
        Assert.True(reply["isActive"].GetBoolean());
    }

    [Fact]
    public async Task Get_unknown_is_404() =>
        ProblemAssert.Is(await api.Get("roles/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);

    [Fact]
    public async Task Users_lists_holders_of_a_role()
    {
        var user = await api.NewUser(roles: ["Manager"]);
        var reply = await api.Get("roles/3/users");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Contains(user, reply.Ids);
        Assert.DoesNotContain(await api.NewEmployee(), (await api.Get("roles/3/users")).Ids);
    }

    [Fact]
    public async Task Users_includes_people_who_inherit_the_role_from_a_department()
    {
        var admin = (await api.Get("roles/2/users")).Ids;
        Assert.Contains(ApiFixture.Admin, admin);
    }

    [Fact]
    public async Task Users_of_unknown_role_is_404() =>
        ProblemAssert.Is(await api.Get($"roles/{ApiFixture.UnknownId}/users"), HttpStatusCode.NotFound);

    [Fact]
    public async Task Create_returns_201_with_location()
    {
        var reply = await api.Post("roles", Body("Packer" + ApiFixture.Unique()));
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/roles/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.True(reply["isActive"].GetBoolean());
        Assert.False(reply["isSystem"].GetBoolean());
        Assert.Equal(HttpStatusCode.OK, (await api.Get($"roles/{reply.Id}")).Status);
    }

    [Fact]
    public async Task Create_by_admin_is_allowed() =>
        Assert.Equal(HttpStatusCode.Created, (await api.Post("roles", Body(), ApiFixture.Admin)).Status);

    [Fact]
    public async Task Create_duplicate_name_ignoring_case_is_409()
    {
        var name = "Dup" + ApiFixture.Unique();
        await api.NewRole(name);
        ProblemAssert.Is(await api.Post("roles", Body(name.ToUpperInvariant())), HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("")]
    public async Task Create_with_a_bad_name_is_400(string name) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("roles", Body(name))).Status);

    [Fact]
    public async Task Create_without_acting_user_is_401() =>
        ProblemAssert.Is(await api.Post("roles", Body(), actor: null), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Create_with_unknown_acting_user_is_401() =>
        ProblemAssert.Is(await api.Post("roles", Body(), ApiFixture.UnknownId), HttpStatusCode.Unauthorized);

    [Fact]
    public async Task Create_by_employee_is_403() =>
        ProblemAssert.Is(await api.Post("roles", Body(), await api.NewEmployee()), HttpStatusCode.Forbidden);

    [Fact]
    public async Task Create_by_manager_is_403()
    {
        var manager = await api.NewManager(await api.NewDepartment());
        ProblemAssert.Is(await api.Post("roles", Body(), manager), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rights_are_checked_before_validation() =>
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post("roles", Body("A"), await api.NewEmployee())).Status);

    [Fact]
    public async Task Update_changes_name_and_description()
    {
        var id = await api.NewRole();
        var name = "New" + ApiFixture.Unique();
        var reply = await api.Put($"roles/{id}", Body(name, "Updated"));
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal(name, reply.Str("name"));
        Assert.Equal("Updated", reply.Str("description"));
    }

    [Fact]
    public async Task Update_to_an_existing_name_is_409()
    {
        var other = "Taken" + ApiFixture.Unique();
        await api.NewRole(other);
        ProblemAssert.Is(await api.Put($"roles/{await api.NewRole()}", Body(other)), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_renaming_a_system_role_is_409() =>
        ProblemAssert.Is(await api.Put("roles/4", Body("Worker")), HttpStatusCode.Conflict);

    [Fact]
    public async Task Update_unknown_is_404() =>
        ProblemAssert.Is(await api.Put($"roles/{ApiFixture.UnknownId}", Body()), HttpStatusCode.NotFound);

    [Fact]
    public async Task Update_with_bad_body_is_400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"roles/{await api.NewRole()}", Body("A"))).Status);

    [Fact]
    public async Task Update_by_employee_is_403() =>
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Put($"roles/{await api.NewRole()}", Body(), await api.NewEmployee())).Status);

    [Fact]
    public async Task Disable_then_enable_a_role()
    {
        var id = await api.NewRole();
        var disabled = await api.Post($"roles/{id}/disable");
        Assert.Equal(HttpStatusCode.NoContent, disabled.Status);
        Assert.False((await api.Get($"roles/{id}"))["isActive"].GetBoolean());

        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"roles/{id}/enable")).Status);
        Assert.True((await api.Get($"roles/{id}"))["isActive"].GetBoolean());
    }

    [Fact]
    public async Task A_disabled_role_cannot_be_assigned()
    {
        var name = "Off" + ApiFixture.Unique();
        var id = await api.NewRole(name);
        await api.Post($"roles/{id}/disable");
        var reply = await api.Post("users", new { firstName = "Ann", lastName = "Lee", mobile = "0123456789", roles = new[] { name } });
        ProblemAssert.Is(reply, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Disabling_a_system_role_is_409() =>
        ProblemAssert.Is(await api.Post("roles/4/disable"), HttpStatusCode.Conflict);

    [Fact]
    public async Task Disable_and_enable_unknown_are_404()
    {
        ProblemAssert.Is(await api.Post($"roles/{ApiFixture.UnknownId}/disable"), HttpStatusCode.NotFound);
        ProblemAssert.Is(await api.Post($"roles/{ApiFixture.UnknownId}/enable"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Disable_and_enable_need_an_admin()
    {
        var id = await api.NewRole();
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Post($"roles/{id}/disable", actor: null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post($"roles/{id}/disable", actor: await api.NewEmployee())).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post($"roles/{id}/enable", actor: await api.NewEmployee())).Status);
    }

    [Fact]
    public async Task Delete_removes_an_unused_role()
    {
        var id = await api.NewRole();
        Assert.Equal(HttpStatusCode.NoContent, (await api.Delete($"roles/{id}")).Status);
        Assert.Equal(HttpStatusCode.NotFound, (await api.Get($"roles/{id}")).Status);
    }

    [Fact]
    public async Task Delete_a_system_role_is_409() =>
        ProblemAssert.Is(await api.Delete("roles/5"), HttpStatusCode.Conflict);

    [Fact]
    public async Task Delete_a_role_held_by_a_user_is_409()
    {
        var name = "Held" + ApiFixture.Unique();
        var id = await api.NewRole(name);
        await api.NewUser(roles: [name]);
        ProblemAssert.Is(await api.Delete($"roles/{id}"), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_a_role_held_by_a_department_is_409()
    {
        var name = "Held" + ApiFixture.Unique();
        var id = await api.NewRole(name);
        await api.NewDepartment(name);
        ProblemAssert.Is(await api.Delete($"roles/{id}"), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Delete_unknown_is_404() =>
        ProblemAssert.Is(await api.Delete("roles/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);

    [Fact]
    public async Task Delete_needs_an_admin()
    {
        var id = await api.NewRole();
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Delete($"roles/{id}", null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Delete($"roles/{id}", await api.NewEmployee())).Status);
    }
}
