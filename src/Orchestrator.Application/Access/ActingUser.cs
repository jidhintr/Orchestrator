using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.Access;

public sealed record Actor(int Id, int Rank, IReadOnlySet<int> DepartmentIds, IReadOnlySet<int> DepartmentRoleIds)
{
    public bool IsManager => Rank == Domain.Rank.Manager;
}

// Temporary stand-in for authentication: the caller names itself in a header, which is trusted. Replace when auth lands.
public sealed class ActingUser(ICurrentUserId current, IAppDbContext db)
{
    public const string Header = "X-Acting-User-Id";

    private Actor? _actor;

    public async Task<Actor> RequireAsync(int minimumRank, CancellationToken cancellationToken)
    {
        _actor ??= await LoadAsync(cancellationToken);
        return _actor.Rank >= minimumRank ? _actor : throw new ForbiddenException("Your role does not allow this action.");
    }

    private async Task<Actor> LoadAsync(CancellationToken cancellationToken)
    {
        if (!int.TryParse(current.Raw, out var id))
        {
            throw new UnauthorizedException($"Send the acting user's id in the {Header} header.");
        }

        var user = await db.Users.AsNoTracking().Where(u => u.Id == id && u.IsActive).Select(u => new
        {
            DepartmentIds = u.Departments.Select(d => d.Id).ToList(),
            DepartmentRoleIds = u.Departments.SelectMany(d => d.Roles).Select(r => r.Id).ToList(),
            RoleIds = u.Roles.Select(r => r.Id).ToList(),
        }).SingleOrDefaultAsync(cancellationToken) ?? throw new UnauthorizedException("The acting user does not exist or is inactive.");

        return new Actor(id, Rank.Of(user.DepartmentRoleIds.Concat(user.RoleIds)), user.DepartmentIds.ToHashSet(), user.DepartmentRoleIds.ToHashSet());
    }
}
