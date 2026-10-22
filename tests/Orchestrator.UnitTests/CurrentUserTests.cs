using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.UnitTests;

public class CurrentUserTests
{
    [Fact]
    public async Task Missing_header_is_unauthorized()
    {
        using var db = TestDb.Create();
        await Assert.ThrowsAsync<UnauthorizedException>(() => db.As(null).RequireAsync(Rank.Employee, default));
    }

    [Fact]
    public async Task Unknown_user_is_unauthorized()
    {
        using var db = TestDb.Create();
        await Assert.ThrowsAsync<UnauthorizedException>(() => db.As(9999).RequireAsync(Rank.Employee, default));
    }

    [Fact]
    public async Task Inactive_user_is_unauthorized()
    {
        using var db = TestDb.Create();
        var user = db.AddUser("gone", [SystemIds.UnassignedDepartment]);
        user.IsActive = false;
        db.SaveChanges();
        await Assert.ThrowsAsync<UnauthorizedException>(() => db.As(user.Id).RequireAsync(Rank.Employee, default));
    }

    [Fact]
    public async Task Super_admin_has_rank_four_and_own_department()
    {
        using var db = TestDb.Create();
        var actor = await db.As(TestDb.SuperAdminUser).RequireAsync(Rank.SuperAdmin, default);
        Assert.Equal(Rank.SuperAdmin, actor.Rank);
        Assert.Equal([SystemIds.AdminRole], actor.DepartmentRoleIds);
        Assert.Contains(SystemIds.AdministrativesDepartment, actor.DepartmentIds);
        Assert.False(actor.IsManager);
    }

    [Fact]
    public async Task Admin_is_forbidden_from_super_admin_actions()
    {
        using var db = TestDb.Create();
        await Assert.ThrowsAsync<ForbiddenException>(() => db.As(TestDb.AdminUser).RequireAsync(Rank.SuperAdmin, default));
        Assert.Equal(Rank.Admin, (await db.As(TestDb.AdminUser).RequireAsync(Rank.Admin, default)).Rank);
    }

    [Fact]
    public async Task User_without_roles_is_an_employee()
    {
        using var db = TestDb.Create();
        var user = db.AddUser("plain", [SystemIds.UnassignedDepartment]);
        var acting = db.As(user.Id);
        Assert.Equal(Rank.Employee, (await acting.RequireAsync(Rank.Employee, default)).Rank);
        await Assert.ThrowsAsync<ForbiddenException>(() => acting.RequireAsync(Rank.Manager, default));
    }

    [Fact]
    public async Task Manager_rank_comes_from_department_role()
    {
        using var db = TestDb.Create();
        var press = db.AddDepartment("PRESS", SystemIds.ManagerRole);
        var manager = db.AddUser("boss", [press.Id]);
        var actor = await db.As(manager.Id).RequireAsync(Rank.Manager, default);
        Assert.True(actor.IsManager);
        Assert.Equal([press.Id], actor.DepartmentIds);
    }

    [Fact]
    public async Task Manager_rank_comes_from_own_role()
    {
        using var db = TestDb.Create();
        var user = db.AddUser("lead", [SystemIds.UnassignedDepartment], SystemIds.ManagerRole);
        Assert.True((await db.As(user.Id).RequireAsync(Rank.Manager, default)).IsManager);
    }
}
