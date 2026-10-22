namespace Orchestrator.IntegrationTests;

public class DepartmentsTests(ApiFixture api)
{
    private static object Body(string? name = null, string? code = null) =>
        new { name = name ?? "Dept " + ApiFixture.Unique(), code = code ?? "D" + ApiFixture.Unique().ToUpperInvariant(), description = "Does things" };

    [Fact]
    public async Task List_contains_the_five_system_departments()
    {
        var reply = await api.Get("departments");
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        foreach (var name in new[] { "Administratives", "Accounting", "Quality Check", "Customers", "Unassigned" })
        {
            Assert.Contains(name, reply.Names());
        }

        Assert.Equal(reply.Names().Order(StringComparer.OrdinalIgnoreCase), reply.Names());
    }

    [Fact]
    public async Task Get_returns_a_department()
    {
        var reply = await api.Get("departments/4");
        Assert.Equal("QUALITY_CHECK", reply.Str("code"));
        Assert.True(reply["isSystem"].GetBoolean());
    }

    [Fact]
    public async Task Get_unknown_is_404() =>
        ProblemAssert.Is(await api.Get("departments/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);

    [Fact]
    public async Task Create_returns_201_with_location()
    {
        var reply = await api.Post("departments", Body());
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/departments/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.True(reply["isActive"].GetBoolean());
        Assert.False(reply["isSystem"].GetBoolean());
    }

    [Fact]
    public async Task Create_duplicate_code_is_409()
    {
        var code = "C" + ApiFixture.Unique().ToUpperInvariant();
        await api.Post("departments", Body(code: code));
        ProblemAssert.Is(await api.Post("departments", Body(code: code)), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_duplicate_name_ignoring_case_is_409()
    {
        var name = "Same " + ApiFixture.Unique();
        await api.Post("departments", Body(name));
        ProblemAssert.Is(await api.Post("departments", Body(name.ToUpperInvariant())), HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("lower")]
    [InlineData("HAS SPACE")]
    [InlineData("X")]
    public async Task Create_with_a_bad_code_is_400(string code) =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("departments", Body(code: code))).Status);

    [Fact]
    public async Task Create_without_a_name_is_400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("departments", new { code = "OK" })).Status);

    [Fact]
    public async Task Create_needs_an_admin()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Post("departments", Body(), null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post("departments", Body(), await api.NewEmployee())).Status);
        Assert.Equal(HttpStatusCode.Created, (await api.Post("departments", Body(), ApiFixture.Admin)).Status);
    }

    [Fact]
    public async Task Update_changes_name_and_description_but_never_the_code()
    {
        var created = await api.Post("departments", Body());
        var name = "Renamed " + ApiFixture.Unique();
        var reply = await api.Put($"departments/{created.Id}", new { name, description = "New", code = "CHANGED" });
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal(name, reply.Str("name"));
        Assert.Equal("New", reply.Str("description"));
        Assert.Equal(created.Str("code"), reply.Str("code"));
    }

    [Fact]
    public async Task Update_to_an_existing_name_is_409()
    {
        var taken = await api.Post("departments", Body());
        var other = await api.Post("departments", Body());
        ProblemAssert.Is(await api.Put($"departments/{other.Id}", new { name = taken.Str("name") }), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_renaming_a_system_department_is_409() =>
        ProblemAssert.Is(await api.Put("departments/4", new { name = "Quality" }), HttpStatusCode.Conflict);

    [Fact]
    public async Task Update_unknown_is_404() =>
        ProblemAssert.Is(await api.Put($"departments/{ApiFixture.UnknownId}", new { name = "X" }), HttpStatusCode.NotFound);

    [Fact]
    public async Task Update_with_an_empty_name_is_400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"departments/{await api.NewDepartment()}", new { name = "" })).Status);

    [Fact]
    public async Task Update_needs_an_admin() =>
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Put($"departments/{await api.NewDepartment()}", new { name = "X" }, await api.NewEmployee())).Status);

    [Fact]
    public async Task Deactivate_keeps_the_record_inactive()
    {
        var id = await api.NewDepartment();
        Assert.Equal(HttpStatusCode.NoContent, (await api.Post($"departments/{id}/deactivate")).Status);
        var reply = await api.Get($"departments/{id}");
        Assert.False(reply["isActive"].GetBoolean());
    }

    [Fact]
    public async Task Deactivate_a_system_department_is_409() =>
        ProblemAssert.Is(await api.Post("departments/4/deactivate"), HttpStatusCode.Conflict);

    [Fact]
    public async Task Deactivate_unknown_is_404() =>
        ProblemAssert.Is(await api.Post($"departments/{ApiFixture.UnknownId}/deactivate"), HttpStatusCode.NotFound);

    [Fact]
    public async Task Deactivate_needs_an_admin() =>
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Post($"departments/{await api.NewDepartment()}/deactivate", actor: await api.NewEmployee())).Status);

    [Fact]
    public async Task Roles_lists_the_seeded_roles_of_a_department()
    {
        Assert.Equal(["Admin"], (await api.Get("departments/2/roles")).Names());
        Assert.Equal(["Client"], (await api.Get("departments/6/roles")).Names());
        Assert.Empty((await api.Get("departments/4/roles")).Items);
    }

    [Fact]
    public async Task Roles_of_unknown_department_is_404() =>
        ProblemAssert.Is(await api.Get($"departments/{ApiFixture.UnknownId}/roles"), HttpStatusCode.NotFound);

    [Fact]
    public async Task Assign_roles_replaces_them_and_users_inherit()
    {
        var department = await api.NewDepartment();
        var user = await api.NewUser([department]);
        var reply = await api.Put($"departments/{department}/roles", new { roles = new[] { "Manager", "Employee" } }, ApiFixture.Admin);
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal(["Employee", "Manager"], reply.Names());
        Assert.Equal(["Employee", "Manager"], (await api.Get($"users/{user}"))["departmentRoles"].EnumerateArray().Select(e => e.GetString()));

        var cleared = await api.Put($"departments/{department}/roles", new { roles = Array.Empty<string>() });
        Assert.Empty(cleared.Items);
    }

    [Fact]
    public async Task Admin_cannot_hand_out_the_admin_role() =>
        ProblemAssert.Is(await api.Put($"departments/{await api.NewDepartment()}/roles", new { roles = new[] { "Admin" } }, ApiFixture.Admin), HttpStatusCode.Forbidden);

    [Fact]
    public async Task Admin_cannot_change_the_admin_department() =>
        ProblemAssert.Is(await api.Put("departments/2/roles", new { roles = new[] { "Manager" } }, ApiFixture.Admin), HttpStatusCode.Forbidden);

    [Fact]
    public async Task Super_admin_can_hand_out_the_admin_role()
    {
        var department = await api.NewDepartment();
        var reply = await api.Put($"departments/{department}/roles", new { roles = new[] { "Admin" } });
        Assert.Equal(["Admin"], reply.Names());
    }

    [Fact]
    public async Task Assign_unknown_role_is_400() =>
        ProblemAssert.Is(await api.Put($"departments/{await api.NewDepartment()}/roles", new { roles = new[] { "Nope" + ApiFixture.Unique() } }), HttpStatusCode.BadRequest);

    [Fact]
    public async Task Assign_roles_to_unknown_department_is_404() =>
        ProblemAssert.Is(await api.Put($"departments/{ApiFixture.UnknownId}/roles", new { roles = new[] { "Manager" } }), HttpStatusCode.NotFound);

    [Fact]
    public async Task Assign_roles_without_the_roles_property_is_400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"departments/{await api.NewDepartment()}/roles", new { })).Status);

    [Fact]
    public async Task Assign_roles_needs_an_admin()
    {
        var department = await api.NewDepartment();
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Put($"departments/{department}/roles", new { roles = new[] { "Employee" } }, null)).Status);
        Assert.Equal(HttpStatusCode.Forbidden, (await api.Put($"departments/{department}/roles", new { roles = new[] { "Employee" } }, await api.NewEmployee())).Status);
    }
}
