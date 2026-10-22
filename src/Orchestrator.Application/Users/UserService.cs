using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Access;
using Orchestrator.Application.Auth;
using Orchestrator.Application.Errors;
using Orchestrator.Application.Roles;
using Orchestrator.Domain;

namespace Orchestrator.Application.Users;

public sealed class UserService(IAppDbContext db, RoleService roles, CurrentUser acting, AuthService auth)
{
    internal static readonly Expression<Func<User, UserResponse>> Project = u => new(
        u.Id,
        u.FirstName,
        u.LastName,
        u.Email,
        u.Mobile,
        u.Departments.OrderBy(d => d.Name).Select(d => new DepartmentRef(d.Id, d.Name)).ToList(),
        u.Departments.SelectMany(d => d.Roles).Select(r => r.Name).Distinct().OrderBy(n => n).ToList(),
        u.Roles.Select(r => r.Name).OrderBy(n => n).ToList(),
        u.IsActive);

    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().OrderBy(u => u.FirstName).ThenBy(u => u.LastName).Select(Project).ToListAsync(cancellationToken);

    public async Task<UserResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(u => u.Id == id).Select(Project).SingleOrDefaultAsync(cancellationToken)
        ?? throw NotFound(id);

    // Duplicate emails are rejected by the unique index; GlobalExceptionHandler maps it to 409.
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var actor = await acting.RequireAsync(Rank.Manager, cancellationToken);
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email?.Trim(),
            Mobile = request.Mobile,
        };
        await SetDepartmentsAsync(user, actor, request.DepartmentIds ?? (actor.IsManager ? [.. actor.DepartmentIds] : []), cancellationToken);
        await SetRolesAsync(user, actor, request.Roles ?? [], cancellationToken);
        EnsureOutranks(actor, user);
        db.Users.Add(user);
        if (request.Password is not null)
        {
            db.Credentials.Add(new UserCredential { User = user, PasswordHash = PasswordHasher.Hash(request.Password), PasswordChangedAt = DateTime.UtcNow });
        }

        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var (actor, user) = await LoadManagedAsync(id, cancellationToken);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email?.Trim();
        user.Mobile = request.Mobile;
        user.IsActive = request.IsActive;
        if (!request.IsActive)
        {
            await auth.RevokeTokensAsync(id, cancellationToken);
        }

        return await SaveAsync(actor, user, cancellationToken);
    }

    public async Task<UserResponse> AssignRolesAsync(int id, AssignRolesRequest request, CancellationToken cancellationToken)
    {
        var (actor, user) = await LoadManagedAsync(id, cancellationToken);
        await SetRolesAsync(user, actor, request.Roles, cancellationToken);
        return await SaveAsync(actor, user, cancellationToken);
    }

    public async Task<UserResponse> AssignDepartmentsAsync(int id, AssignDepartmentsRequest request, CancellationToken cancellationToken)
    {
        var (actor, user) = await LoadManagedAsync(id, cancellationToken);
        await SetDepartmentsAsync(user, actor, request.DepartmentIds, cancellationToken);
        return await SaveAsync(actor, user, cancellationToken);
    }

    public async Task SetPasswordAsync(int id, SetPasswordRequest request, CancellationToken cancellationToken)
    {
        await LoadManagedAsync(id, cancellationToken);
        await auth.SetPasswordAsync(id, request.Password, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserResponse> SaveAsync(Actor actor, User user, CancellationToken cancellationToken)
    {
        EnsureOutranks(actor, user);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(user.Id, cancellationToken);
    }

    // Managers replace only the departments they manage; the target keeps its others.
    private async Task SetDepartmentsAsync(User user, Actor actor, IEnumerable<int> requested, CancellationToken cancellationToken)
    {
        var ids = requested.ToHashSet();
        if (actor.IsManager)
        {
            if (!ids.IsSubsetOf(actor.DepartmentIds))
            {
                throw new ForbiddenException("Managers can only use their own departments.");
            }

            ids.UnionWith(user.Departments.Select(d => d.Id).Where(id => !actor.DepartmentIds.Contains(id)));
        }

        if (ids.Count > 1)
        {
            ids.Remove(SystemIds.UnassignedDepartment);
        }

        if (ids.Count == 0)
        {
            ids.Add(SystemIds.UnassignedDepartment);
        }

        if (ids.Count > 1 && ids.Contains(SystemIds.CustomersDepartment))
        {
            throw new BadRequestException("A user in the Customers department cannot be in any other department.");
        }

        var found = await db.Departments.Include(d => d.Roles).Where(d => d.IsActive && ids.Contains(d.Id)).ToListAsync(cancellationToken);
        if (found.Count != ids.Count)
        {
            throw new BadRequestException("A department does not exist or is deactivated.");
        }

        user.Departments.Clear();
        user.Departments.AddRange(found);
    }

    // Same replace-only-what-you-may-assign rule as departments.
    private async Task SetRolesAsync(User user, Actor actor, List<string> names, CancellationToken cancellationToken)
    {
        var found = await roles.FindActiveAsync(names, cancellationToken);
        if (found.Find(r => !CanAssign(actor, r)) is { } denied)
        {
            throw new ForbiddenException($"You cannot assign the role '{denied.Name}'.");
        }

        var kept = user.Roles.Where(r => !CanAssign(actor, r)).ToList();
        user.Roles.Clear();
        user.Roles.AddRange(kept.Concat(found));
    }

    private async Task<(Actor Actor, User User)> LoadManagedAsync(int id, CancellationToken cancellationToken)
    {
        var actor = await acting.RequireAsync(Rank.Manager, cancellationToken);
        var user = await db.Users.Include(u => u.Roles).Include(u => u.Departments).ThenInclude(d => d.Roles)
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken) ?? throw NotFound(id);
        if (!Rank.Outranks(actor.Rank, RankOf(user)) || (actor.IsManager && !user.Departments.Any(d => actor.DepartmentIds.Contains(d.Id))))
        {
            throw new ForbiddenException("You cannot manage this user.");
        }

        return (actor, user);
    }

    private static bool CanAssign(Actor actor, Role role) =>
        Rank.Outranks(actor.Rank, Rank.Of([role.Id])) && (!actor.IsManager || actor.DepartmentRoleIds.Contains(role.Id));

    private static void EnsureOutranks(Actor actor, User user)
    {
        if (!Rank.Outranks(actor.Rank, RankOf(user)))
        {
            throw new ForbiddenException("The user would end up at or above your rank.");
        }
    }

    private static int RankOf(User user) => Rank.Of(user.Roles.Concat(user.Departments.SelectMany(d => d.Roles)).Select(r => r.Id));

    private static NotFoundException NotFound(int id) => new($"User '{id}' was not found.");
}
