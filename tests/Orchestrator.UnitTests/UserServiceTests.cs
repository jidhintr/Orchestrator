using Orchestrator.Application.Errors;
using Orchestrator.Application.Roles;
using Orchestrator.Application.Users;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class UserServiceTests
{
    private readonly OrchestratorDbContext _db = TestDb.Create();
    private readonly Department _press;
    private readonly Department _bindery;
    private readonly User _manager;
    private readonly User _otherManager;
    private readonly User _employee;

    public UserServiceTests()
    {
        _press = _db.AddDepartment("PRESS", SystemIds.EmployeeRole);
        _bindery = _db.AddDepartment("BINDERY", SystemIds.EmployeeRole);
        _manager = _db.AddUser("pressboss", [_press.Id], SystemIds.ManagerRole);
        _otherManager = _db.AddUser("binderyboss", [_bindery.Id], SystemIds.ManagerRole);
        _employee = _db.AddUser("presshand", [_press.Id], SystemIds.EmployeeRole);
    }

    private UserService As(int? userId = TestDb.SuperAdminUser) => new(_db, new RoleService(_db), _db.As(userId));

    private static CreateUserRequest Create(string name = "newbie", List<int>? departments = null, List<string>? roles = null) =>
        new() { FirstName = name, LastName = "Person", Email = $"{name}@x.local", Mobile = "0123456789", DepartmentIds = departments, Roles = roles };

    private static UpdateUserRequest Update(string name = "renamed", bool active = true) =>
        new() { FirstName = name, LastName = "Person", Email = $"{name}@x.local", Mobile = "0123456789", IsActive = active };

    [Fact]
    public async Task List_is_ordered_by_name_and_get_returns_one()
    {
        var list = await As().ListAsync(default);
        Assert.Equal(list.Select(u => u.FirstName).Order(Comparer<string>.Default), list.Select(u => u.FirstName));
        var one = await As().GetAsync(_employee.Id, default);
        Assert.Equal("presshand", one.FirstName);
        Assert.Equal(["PRESS"], one.Departments.Select(d => d.Name));
        Assert.Equal(["Employee"], one.DepartmentRoles);
        Assert.Equal(["Employee"], one.Roles);
    }

    [Fact]
    public async Task Get_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => As().GetAsync(9999, default));

    [Fact]
    public async Task Create_without_departments_lands_in_unassigned_and_trims()
    {
        var created = await As().CreateAsync(Create() with { FirstName = "  newbie  " }, default);
        Assert.Equal("newbie", created.FirstName);
        Assert.Equal([SystemIds.UnassignedDepartment], created.Departments.Select(d => d.Id));
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_with_empty_departments_lands_in_unassigned()
    {
        var created = await As().CreateAsync(Create(departments: []), default);
        Assert.Equal([SystemIds.UnassignedDepartment], created.Departments.Select(d => d.Id));
    }

    [Fact]
    public async Task Create_with_departments_drops_unassigned()
    {
        var created = await As().CreateAsync(Create(departments: [_press.Id, SystemIds.UnassignedDepartment]), default);
        Assert.Equal([_press.Id], created.Departments.Select(d => d.Id));
    }

    [Fact]
    public async Task Create_with_roles_assigns_them()
    {
        var created = await As().CreateAsync(Create(roles: ["Manager"]), default);
        Assert.Equal(["Manager"], created.Roles);
    }

    [Fact]
    public async Task Create_unknown_department_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => As().CreateAsync(Create(departments: [9999]), default));

    [Fact]
    public async Task Create_deactivated_department_is_bad_request()
    {
        _press.IsActive = false;
        _db.SaveChanges();
        await Assert.ThrowsAsync<BadRequestException>(() => As().CreateAsync(Create(departments: [_press.Id]), default));
    }

    [Fact]
    public async Task Create_unknown_role_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => As().CreateAsync(Create(roles: ["Nope"]), default));

    [Fact]
    public async Task Create_by_admin_cannot_make_an_admin() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(TestDb.AdminUser).CreateAsync(Create(roles: ["Admin"]), default));

    [Fact]
    public async Task Create_by_admin_cannot_place_user_in_admin_department() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(TestDb.AdminUser).CreateAsync(Create(departments: [2]), default));

    [Fact]
    public async Task Create_by_super_admin_can_make_an_admin()
    {
        var created = await As().CreateAsync(Create(roles: ["Admin"]), default);
        Assert.Equal(["Admin"], created.Roles);
    }

    [Fact]
    public async Task Create_by_manager_defaults_to_own_departments()
    {
        var created = await As(_manager.Id).CreateAsync(Create(), default);
        Assert.Equal([_press.Id], created.Departments.Select(d => d.Id));
    }

    [Fact]
    public async Task Create_by_manager_outside_own_departments_is_forbidden() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_manager.Id).CreateAsync(Create(departments: [_bindery.Id]), default));

    [Fact]
    public async Task Create_by_manager_can_give_department_role()
    {
        var created = await As(_manager.Id).CreateAsync(Create(roles: ["Employee"]), default);
        Assert.Equal(["Employee"], created.Roles);
    }

    [Fact]
    public async Task Create_by_manager_cannot_give_role_outside_department_roles()
    {
        var plain = _db.AddDepartment("PLAIN", SystemIds.ManagerRole);
        var boss = _db.AddUser("plainboss", [plain.Id]);
        await Assert.ThrowsAsync<ForbiddenException>(() => As(boss.Id).CreateAsync(Create(roles: ["Employee"]), default));
    }

    [Fact]
    public async Task Create_by_manager_cannot_make_a_manager() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_manager.Id).CreateAsync(Create(roles: ["Manager"]), default));

    [Fact]
    public async Task Create_by_employee_is_forbidden() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_employee.Id).CreateAsync(Create(), default));

    [Fact]
    public async Task Create_without_acting_user_is_unauthorized() =>
        await Assert.ThrowsAsync<UnauthorizedException>(() => As(null).CreateAsync(Create(), default));

    [Fact]
    public async Task Update_changes_profile_and_can_deactivate()
    {
        var updated = await As().UpdateAsync(_employee.Id, Update("renamed", active: false), default);
        Assert.Equal("renamed", updated.FirstName);
        Assert.Equal("renamed@x.local", updated.Email);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task Update_with_null_email_clears_it()
    {
        var updated = await As().UpdateAsync(_employee.Id, Update() with { Email = null }, default);
        Assert.Null(updated.Email);
    }

    [Fact]
    public async Task Update_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => As().UpdateAsync(9999, Update(), default));

    [Fact]
    public async Task Update_user_at_own_rank_is_forbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => As(TestDb.AdminUser).UpdateAsync(TestDb.AdminUser, Update(), default));
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_manager.Id).UpdateAsync(_manager.Id, Update(), default));
    }

    [Fact]
    public async Task Update_user_above_rank_is_forbidden() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(TestDb.AdminUser).UpdateAsync(TestDb.SuperAdminUser, Update(), default));

    [Fact]
    public async Task Update_by_super_admin_can_edit_super_admin()
    {
        var updated = await As().UpdateAsync(TestDb.SuperAdminUser, Update(), default);
        Assert.Equal("renamed", updated.FirstName);
    }

    [Fact]
    public async Task Update_by_manager_in_same_department_is_allowed()
    {
        var updated = await As(_manager.Id).UpdateAsync(_employee.Id, Update(), default);
        Assert.Equal("renamed", updated.FirstName);
    }

    [Fact]
    public async Task Update_by_manager_in_other_department_is_forbidden() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_otherManager.Id).UpdateAsync(_employee.Id, Update(), default));

    [Fact]
    public async Task Update_by_employee_is_forbidden() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(_employee.Id).UpdateAsync(_manager.Id, Update(), default));

    [Fact]
    public async Task AssignRoles_replaces_user_roles()
    {
        var updated = await As().AssignRolesAsync(_employee.Id, new AssignRolesRequest { Roles = ["Manager"] }, default);
        Assert.Equal(["Manager"], updated.Roles);
        updated = await As().AssignRolesAsync(_employee.Id, new AssignRolesRequest { Roles = [] }, default);
        Assert.Empty(updated.Roles);
    }

    [Fact]
    public async Task AssignRoles_unknown_role_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => As().AssignRolesAsync(_employee.Id, new AssignRolesRequest { Roles = ["Nope"] }, default));

    [Fact]
    public async Task AssignRoles_unknown_user_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => As().AssignRolesAsync(9999, new AssignRolesRequest { Roles = [] }, default));

    [Fact]
    public async Task AssignRoles_by_admin_cannot_give_admin_role() =>
        await Assert.ThrowsAsync<ForbiddenException>(() => As(TestDb.AdminUser).AssignRolesAsync(_employee.Id, new AssignRolesRequest { Roles = ["Admin"] }, default));

    [Fact]
    public async Task AssignRoles_by_manager_keeps_roles_they_cannot_assign()
    {
        var target = _db.AddUser("target", [_press.Id], SystemIds.ClientRole);
        var updated = await As(_manager.Id).AssignRolesAsync(target.Id, new AssignRolesRequest { Roles = ["Employee"] }, default);
        Assert.Equal(["Client", "Employee"], updated.Roles);
        updated = await As(_manager.Id).AssignRolesAsync(target.Id, new AssignRolesRequest { Roles = [] }, default);
        Assert.Equal(["Client"], updated.Roles);
    }

    [Fact]
    public async Task AssignDepartments_replaces_departments()
    {
        var updated = await As().AssignDepartmentsAsync(_employee.Id, new AssignDepartmentsRequest { DepartmentIds = [_bindery.Id] }, default);
        Assert.Equal([_bindery.Id], updated.Departments.Select(d => d.Id));
    }

    [Fact]
    public async Task AssignDepartments_empty_list_leaves_user_in_unassigned()
    {
        var updated = await As().AssignDepartmentsAsync(_employee.Id, new AssignDepartmentsRequest { DepartmentIds = [] }, default);
        Assert.Equal([SystemIds.UnassignedDepartment], updated.Departments.Select(d => d.Id));
    }

    [Fact]
    public async Task AssignDepartments_unknown_department_is_bad_request() =>
        await Assert.ThrowsAsync<BadRequestException>(() => As().AssignDepartmentsAsync(_employee.Id, new AssignDepartmentsRequest { DepartmentIds = [9999] }, default));

    [Fact]
    public async Task AssignDepartments_unknown_user_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => As().AssignDepartmentsAsync(9999, new AssignDepartmentsRequest { DepartmentIds = [] }, default));

    [Fact]
    public async Task AssignDepartments_by_manager_keeps_departments_they_do_not_manage()
    {
        var shared = _db.AddUser("shared", [_press.Id, _bindery.Id]);
        var updated = await As(_manager.Id).AssignDepartmentsAsync(shared.Id, new AssignDepartmentsRequest { DepartmentIds = [] }, default);
        Assert.Equal([_bindery.Id], updated.Departments.Select(d => d.Id));
    }

    [Fact]
    public async Task AssignDepartments_by_manager_outside_own_departments_is_forbidden() =>
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            As(_manager.Id).AssignDepartmentsAsync(_employee.Id, new AssignDepartmentsRequest { DepartmentIds = [_bindery.Id] }, default));
}
