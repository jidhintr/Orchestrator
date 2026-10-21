using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Orchestrator.Api.Errors;
using Orchestrator.Api.Roles;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Api.Users;

public sealed class UserService(OrchestratorDbContext db, RoleService roles)
{
    internal static readonly Expression<Func<User, UserResponse>> Project = u =>
        new(u.Id, u.FirstName, u.LastName, u.Email, u.Mobile, u.DepartmentId, u.Department!.Name, u.Department.Roles.Select(r => r.Name).OrderBy(n => n).ToList(), u.Roles.Select(r => r.Name).OrderBy(n => n).ToList(), u.IsActive);

    public async Task<IReadOnlyList<UserResponse>> ListAsync(CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().OrderBy(u => u.FirstName).ThenBy(u => u.LastName).Select(Project).ToListAsync(cancellationToken);

    public async Task<UserResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        await db.Users.AsNoTracking().Where(u => u.Id == id).Select(Project).SingleOrDefaultAsync(cancellationToken)
        ?? throw NotFound(id);

    // Duplicate emails are rejected by the unique index and unknown departments by the FK; GlobalExceptionHandler maps both.
    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email?.Trim(),
            Mobile = request.Mobile,
            DepartmentId = request.DepartmentId,
        };
        user.Roles.AddRange(await roles.FindActiveAsync(request.Roles, cancellationToken));
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(user.Id, cancellationToken);
    }

    public async Task<UserResponse> UpdateAsync(int id, UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await FindWithRolesAsync(id, cancellationToken);
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = request.Email?.Trim();
        user.Mobile = request.Mobile;
        user.DepartmentId = request.DepartmentId;
        user.IsActive = request.IsActive;
        await ReplaceRolesAsync(user, request.Roles, cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task<UserResponse> AssignRolesAsync(int id, AssignRolesRequest request, CancellationToken cancellationToken)
    {
        await ReplaceRolesAsync(await FindWithRolesAsync(id, cancellationToken), request.Roles, cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    private async Task ReplaceRolesAsync(User user, List<string> names, CancellationToken cancellationToken)
    {
        var found = await roles.FindActiveAsync(names, cancellationToken);
        user.Roles.Clear();
        user.Roles.AddRange(found);
        await db.SaveChangesAsync(cancellationToken);
    }


    private async Task<User> FindWithRolesAsync(int id, CancellationToken cancellationToken) =>
        await db.Users.Include(u => u.Roles).SingleOrDefaultAsync(u => u.Id == id, cancellationToken) ?? throw NotFound(id);

    private static NotFoundException NotFound(int id) => new($"User '{id}' was not found.");
}
