using Orchestrator.Domain;

namespace Orchestrator.UnitTests;

public class RankTests
{
    [Fact]
    public void No_roles_is_employee() => Assert.Equal(Rank.Employee, Rank.Of([]));

    [Theory]
    [InlineData(SystemIds.SuperAdminRole, Rank.SuperAdmin)]
    [InlineData(SystemIds.AdminRole, Rank.Admin)]
    [InlineData(SystemIds.ManagerRole, Rank.Manager)]
    [InlineData(SystemIds.EmployeeRole, Rank.Employee)]
    [InlineData(SystemIds.ClientRole, Rank.Employee)]
    [InlineData(5000, Rank.Employee)]
    public void Single_role_maps_to_rank(int roleId, int expected) => Assert.Equal(expected, Rank.Of([roleId]));

    [Fact]
    public void Highest_role_wins() =>
        Assert.Equal(Rank.Admin, Rank.Of([SystemIds.EmployeeRole, SystemIds.AdminRole, SystemIds.ManagerRole]));

    [Theory]
    [InlineData(Rank.SuperAdmin, Rank.SuperAdmin, true)]
    [InlineData(Rank.SuperAdmin, Rank.Admin, true)]
    [InlineData(Rank.Admin, Rank.Admin, false)]
    [InlineData(Rank.Admin, Rank.Manager, true)]
    [InlineData(Rank.Manager, Rank.Admin, false)]
    [InlineData(Rank.Manager, Rank.Manager, false)]
    [InlineData(Rank.Employee, Rank.Employee, false)]
    public void Outranks_is_strict_except_for_super_admin(int actor, int target, bool expected) =>
        Assert.Equal(expected, Rank.Outranks(actor, target));
}
