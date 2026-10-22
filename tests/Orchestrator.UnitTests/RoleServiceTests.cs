using Orchestrator.Application.Errors;
using Orchestrator.Application.Roles;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class RoleServiceTests
{
    private readonly OrchestratorDbContext _db = TestDb.Create();
    private readonly RoleService _roles;

    public RoleServiceTests() => _roles = new RoleService(_db);

    private static RoleRequest Request(string name = "Packer", string description = "Packs boxes") => new() { Name = name, Description = description };

    [Fact]
    public async Task List_returns_seeded_roles_ordered_by_name()
    {
        var list = await _roles.ListAsync(default);
        Assert.Equal(["Admin", "Client", "Employee", "Manager", "Super Admin"], list.Select(r => r.Name));
    }

    [Fact]
    public async Task Get_returns_role_and_unknown_is_not_found()
    {
        Assert.Equal("Manager", (await _roles.GetAsync(SystemIds.ManagerRole, default)).Name);
        await Assert.ThrowsAsync<NotFoundException>(() => _roles.GetAsync(9999, default));
    }

    [Fact]
    public async Task Create_trims_and_is_active_not_system()
    {
        var created = await _roles.CreateAsync(Request("  Packer  ", "  Packs  "), default);
        Assert.Equal("Packer", created.Name);
        Assert.Equal("Packs", created.Description);
        Assert.True(created.IsActive);
        Assert.False(created.IsSystem);
    }

    [Fact]
    public async Task Update_changes_name_and_description()
    {
        var created = await _roles.CreateAsync(Request(), default);
        var updated = await _roles.UpdateAsync(created.Id, Request("Sorter", "Sorts"), default);
        Assert.Equal("Sorter", updated.Name);
        Assert.Equal("Sorts", updated.Description);
    }

    [Fact]
    public async Task Update_system_role_cannot_be_renamed_but_description_can_change()
    {
        await Assert.ThrowsAsync<ConflictException>(() => _roles.UpdateAsync(SystemIds.ManagerRole, Request("Boss"), default));
        var updated = await _roles.UpdateAsync(SystemIds.ManagerRole, Request("Manager", "New text"), default);
        Assert.Equal("New text", updated.Description);
    }

    [Fact]
    public async Task Update_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _roles.UpdateAsync(9999, Request(), default));

    [Fact]
    public async Task Disable_and_enable_toggle_custom_role()
    {
        var created = await _roles.CreateAsync(Request(), default);
        await _roles.SetActiveAsync(created.Id, false, default);
        Assert.False((await _roles.GetAsync(created.Id, default)).IsActive);
        await _roles.SetActiveAsync(created.Id, true, default);
        Assert.True((await _roles.GetAsync(created.Id, default)).IsActive);
    }

    [Fact]
    public async Task System_role_cannot_be_disabled_but_can_be_enabled()
    {
        await Assert.ThrowsAsync<ConflictException>(() => _roles.SetActiveAsync(SystemIds.AdminRole, false, default));
        await _roles.SetActiveAsync(SystemIds.AdminRole, true, default);
    }

    [Fact]
    public async Task SetActive_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _roles.SetActiveAsync(9999, false, default));

    [Fact]
    public async Task Delete_removes_unassigned_custom_role()
    {
        var created = await _roles.CreateAsync(Request(), default);
        await _roles.DeleteAsync(created.Id, default);
        await Assert.ThrowsAsync<NotFoundException>(() => _roles.GetAsync(created.Id, default));
    }

    [Fact]
    public async Task Delete_system_role_is_conflict() =>
        await Assert.ThrowsAsync<ConflictException>(() => _roles.DeleteAsync(SystemIds.EmployeeRole, default));

    [Fact]
    public async Task Delete_role_assigned_to_user_is_conflict()
    {
        var created = await _roles.CreateAsync(Request(), default);
        _db.AddUser("holder", [SystemIds.UnassignedDepartment], created.Id);
        await Assert.ThrowsAsync<ConflictException>(() => _roles.DeleteAsync(created.Id, default));
    }

    [Fact]
    public async Task Delete_role_assigned_to_department_is_conflict()
    {
        var created = await _roles.CreateAsync(Request(), default);
        _db.AddDepartment("HOLDER", created.Id);
        await Assert.ThrowsAsync<ConflictException>(() => _roles.DeleteAsync(created.Id, default));
    }

    [Fact]
    public async Task Delete_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _roles.DeleteAsync(9999, default));

    [Fact]
    public async Task Users_lists_direct_and_department_holders_only()
    {
        var direct = _db.AddUser("direct", [SystemIds.UnassignedDepartment], SystemIds.ManagerRole);
        var press = _db.AddDepartment("PRESS", SystemIds.ManagerRole);
        var viaDepartment = _db.AddUser("viadept", [press.Id]);
        _db.AddUser("nobody", [SystemIds.UnassignedDepartment]);

        var users = await _roles.UsersAsync(SystemIds.ManagerRole, default);
        Assert.Equal(["direct", "viadept"], users.Select(u => u.FirstName));
        Assert.Equal([direct.Id, viaDepartment.Id], users.Select(u => u.Id));
    }

    [Fact]
    public async Task Users_of_unknown_role_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _roles.UsersAsync(9999, default));

    [Fact]
    public async Task FindActive_trims_dedupes_and_returns_roles()
    {
        var found = await _roles.FindActiveAsync([" Manager ", "Manager", "Employee"], default);
        Assert.Equal(["Employee", "Manager"], found.Select(r => r.Name).Order());
    }

    [Fact]
    public async Task FindActive_unknown_role_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => _roles.FindActiveAsync(["Nope"], default));

    [Fact]
    public async Task FindActive_disabled_role_is_bad_request()
    {
        var created = await _roles.CreateAsync(Request(), default);
        await _roles.SetActiveAsync(created.Id, false, default);
        await Assert.ThrowsAsync<BadRequestException>(() => _roles.FindActiveAsync(["Packer"], default));
    }

    [Fact]
    public async Task FindActive_empty_list_returns_nothing() =>
        Assert.Empty(await _roles.FindActiveAsync([], default));
}
