using Orchestrator.Application.Departments;
using Orchestrator.Application.Errors;
using Orchestrator.Application.Roles;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class DepartmentServiceTests
{
    private readonly OrchestratorDbContext _db = TestDb.Create();

    private DepartmentService As(int? userId = TestDb.SuperAdminUser) => new(_db, new RoleService(_db), _db.As(userId));

    private static AssignRolesRequest Roles(params string[] names) => new() { Roles = [.. names] };

    [Fact]
    public async Task List_returns_seeded_departments_ordered_by_name()
    {
        var list = await As().ListAsync(default);
        Assert.Equal(["Accounting", "Administratives", "Customers", "Estimation", "Operations", "Quality Check", "Unassigned"], list.Select(d => d.Name));
    }

    [Fact]
    public async Task Get_returns_department_and_unknown_is_not_found()
    {
        Assert.Equal("QUALITY_CHECK", (await As().GetAsync(4, default)).Code);
        await Assert.ThrowsAsync<NotFoundException>(() => As().GetAsync(9999, default));
    }

    [Fact]
    public async Task Create_trims_name_and_is_active_not_system()
    {
        var created = await As().CreateAsync(new CreateDepartmentRequest { Name = "  Press  ", Code = "PRESS", Description = "Prints" }, default);
        Assert.Equal("Press", created.Name);
        Assert.Equal("PRESS", created.Code);
        Assert.True(created.IsActive);
        Assert.False(created.IsSystem);
    }

    [Fact]
    public async Task Update_changes_name_and_description_not_code()
    {
        var created = await As().CreateAsync(new CreateDepartmentRequest { Name = "Press", Code = "PRESS" }, default);
        var updated = await As().UpdateAsync(created.Id, new UpdateDepartmentRequest { Name = "Printing", Description = "Hot" }, default);
        Assert.Equal("Printing", updated.Name);
        Assert.Equal("Hot", updated.Description);
        Assert.Equal("PRESS", updated.Code);
    }

    [Fact]
    public async Task Update_system_department_cannot_be_renamed_but_description_can_change()
    {
        await Assert.ThrowsAsync<ConflictException>(() => As().UpdateAsync(4, new UpdateDepartmentRequest { Name = "Quality" }, default));
        var updated = await As().UpdateAsync(4, new UpdateDepartmentRequest { Name = "Quality Check", Description = "Checks" }, default);
        Assert.Equal("Checks", updated.Description);
    }

    [Fact]
    public async Task Update_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => As().UpdateAsync(9999, new UpdateDepartmentRequest { Name = "X" }, default));

    [Fact]
    public async Task Deactivate_soft_deletes_custom_department()
    {
        var created = await As().CreateAsync(new CreateDepartmentRequest { Name = "Press", Code = "PRESS" }, default);
        await As().DeactivateAsync(created.Id, default);
        Assert.False((await As().GetAsync(created.Id, default)).IsActive);
    }

    [Fact]
    public async Task Activate_restores_a_deactivated_department_and_unknown_is_not_found()
    {
        var created = await As().CreateAsync(new CreateDepartmentRequest { Name = "Cutting", Code = "CUT" }, default);
        await As().DeactivateAsync(created.Id, default);
        await As().ActivateAsync(created.Id, default);
        Assert.True((await As().GetAsync(created.Id, default)).IsActive);
        await Assert.ThrowsAsync<NotFoundException>(() => As().ActivateAsync(9999, default));
    }

    [Fact]
    public async Task Deactivate_system_department_is_conflict() =>
        await Assert.ThrowsAsync<ConflictException>(() => As().DeactivateAsync(4, default));

    [Fact]
    public async Task Deactivate_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => As().DeactivateAsync(9999, default));

    [Fact]
    public async Task Roles_lists_department_roles_ordered_and_unknown_is_not_found()
    {
        var press = _db.AddDepartment("PRESS", SystemIds.ManagerRole, SystemIds.EmployeeRole);
        Assert.Equal(["Employee", "Manager"], (await As().RolesAsync(press.Id, default)).Select(r => r.Name));
        await Assert.ThrowsAsync<NotFoundException>(() => As().RolesAsync(9999, default));
    }

    [Fact]
    public async Task AssignRoles_by_admin_replaces_roles_below_admin()
    {
        var press = _db.AddDepartment("PRESS", SystemIds.EmployeeRole);
        var roles = await As(TestDb.AdminUser).AssignRolesAsync(press.Id, Roles("Manager"), default);
        Assert.Equal(["Manager"], roles.Select(r => r.Name));
    }

    [Fact]
    public async Task AssignRoles_by_admin_cannot_hand_out_admin_role()
    {
        var press = _db.AddDepartment("PRESS");
        await Assert.ThrowsAsync<ForbiddenException>(() => As(TestDb.AdminUser).AssignRolesAsync(press.Id, Roles("Admin"), default));
    }

    [Fact]
    public async Task AssignRoles_by_admin_cannot_touch_department_with_admin_role() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(TestDb.AdminUser).AssignRolesAsync(2, Roles("Manager"), default));

    [Fact]
    public async Task AssignRoles_by_super_admin_can_hand_out_admin_role()
    {
        var press = _db.AddDepartment("PRESS");
        var roles = await As().AssignRolesAsync(press.Id, Roles("Admin"), default);
        Assert.Equal(["Admin"], roles.Select(r => r.Name));
    }

    [Fact]
    public async Task AssignRoles_with_empty_list_clears_roles()
    {
        var press = _db.AddDepartment("PRESS", SystemIds.EmployeeRole);
        Assert.Empty(await As().AssignRolesAsync(press.Id, Roles(), default));
    }

    [Fact]
    public async Task AssignRoles_unknown_role_is_bad_request()
    {
        var press = _db.AddDepartment("PRESS");
        await Assert.ThrowsAsync<BadRequestException>(() => As().AssignRolesAsync(press.Id, Roles("Nope"), default));
    }

    [Fact]
    public async Task AssignRoles_unknown_department_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => As().AssignRolesAsync(9999, Roles("Manager"), default));

    [Fact]
    public async Task AssignRoles_needs_admin_rank()
    {
        var press = _db.AddDepartment("PRESS", SystemIds.ManagerRole);
        var manager = _db.AddUser("boss", [press.Id]);
        await Assert.ThrowsAsync<ForbiddenException>(() => As(manager.Id).AssignRolesAsync(press.Id, Roles("Employee"), default));
    }

    [Fact]
    public async Task AssignRoles_without_acting_user_is_unauthorized()
    {
        var press = _db.AddDepartment("PRESS");
        await Assert.ThrowsAsync<UnauthorizedException>(() => As(null).AssignRolesAsync(press.Id, Roles("Employee"), default));
    }
}
