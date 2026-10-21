using Microsoft.EntityFrameworkCore;
using Orchestrator.Api.Errors;
using Orchestrator.Api.Users;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Api.Roles;

public sealed class RoleService(OrchestratorDbContext db)
{
    public async Task<IReadOnlyList<RoleResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await db.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken)).Select(RoleResponse.From)];

    public async Task<RoleResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        RoleResponse.From(await FindAsync(id, cancellationToken));

    public async Task<IReadOnlyList<UserResponse>> UsersAsync(int id, CancellationToken cancellationToken)
    {
        await FindAsync(id, cancellationToken);
        return await db.Users.AsNoTracking().Where(u => u.Roles.Any(r => r.Id == id))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName).Select(UserService.Project).ToListAsync(cancellationToken);
    }

    // Duplicate names are rejected by the unique index and mapped to 409 by GlobalExceptionHandler.
    public async Task<RoleResponse> CreateAsync(RoleRequest request, CancellationToken cancellationToken)
    {
        var role = new Role { Name = request.Name.Trim(), Description = request.Description.Trim() };
        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);
        return RoleResponse.From(role);
    }

    public async Task<RoleResponse> UpdateAsync(int id, RoleRequest request, CancellationToken cancellationToken)
    {
        var role = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        if (role.IsSystem && name != role.Name)
        {
            throw new ConflictException($"System role '{role.Name}' cannot be renamed.");
        }

        role.Name = name;
        role.Description = request.Description.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return RoleResponse.From(role);
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken)
    {
        var role = await FindAsync(id, cancellationToken);
        if (role.IsSystem && !isActive)
        {
            throw new ConflictException($"System role '{role.Name}' cannot be disabled.");
        }

        role.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var role = await FindAsync(id, cancellationToken);
        if (role.IsSystem)
        {
            throw new ConflictException($"System role '{role.Name}' cannot be deleted.");
        }

        if (await db.Users.AnyAsync(u => u.Roles.Any(r => r.Id == id), cancellationToken))
        {
            throw new ConflictException($"Role '{role.Name}' is assigned to users; unassign it or disable it instead.");
        }

        db.Roles.Remove(role);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Role>> FindActiveAsync(List<string> names, CancellationToken cancellationToken)
    {
        var wanted = names.Select(n => n.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var roles = await db.Roles.Where(r => r.IsActive && wanted.Contains(r.Name)).ToListAsync(cancellationToken);
        return wanted.Find(n => !roles.Exists(r => string.Equals(r.Name, n, StringComparison.OrdinalIgnoreCase))) is { } bad
            ? throw new BadRequestException($"Role '{bad}' does not exist or is disabled.")
            : roles;
    }

    private async Task<Role> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Roles.FindAsync([id], cancellationToken) ?? throw new NotFoundException($"Role '{id}' was not found.");
}
