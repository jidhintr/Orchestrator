using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Api.Access;

public static class Rank
{
    public const int Employee = 1;
    public const int Manager = 2;
    public const int Admin = 3;
    public const int SuperAdmin = 4;

    public static int Of(IEnumerable<int> roleIds) =>
        roleIds.Select(id => id switch
        {
            SystemIds.SuperAdminRole => SuperAdmin,
            SystemIds.AdminRole => Admin,
            SystemIds.ManagerRole => Manager,
            _ => Employee,
        }).DefaultIfEmpty(Employee).Max();

    public static bool Outranks(int actor, int target) => actor == SuperAdmin || actor > target;
}
