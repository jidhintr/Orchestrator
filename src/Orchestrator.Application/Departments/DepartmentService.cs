using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Application.Roles;
using Orchestrator.Domain;

namespace Orchestrator.Application.Departments;

public sealed class DepartmentService(IAppDbContext db, RoleService roles, CurrentUser acting)
{
    public async Task<IReadOnlyList<DepartmentResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync(cancellationToken)).Select(DepartmentResponse.From)];

    public async Task<DepartmentResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        DepartmentResponse.From(await FindAsync(id, cancellationToken));

    // Duplicate names/codes are rejected by the unique indexes and mapped to 409 by GlobalExceptionHandler.
    public async Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = new Department { Name = request.Name.Trim(), Code = request.Code, Description = request.Description };
        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);
        return DepartmentResponse.From(department);
    }

    public async Task<DepartmentResponse> UpdateAsync(int id, UpdateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var department = await FindAsync(id, cancellationToken);
        var name = request.Name.Trim();
        if (department.IsSystem && name != department.Name)
        {
            throw new ConflictException($"System department '{department.Code}' cannot be renamed.");
        }

        department.Name = name;
        department.Description = request.Description;
        await db.SaveChangesAsync(cancellationToken);
        return DepartmentResponse.From(department);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        var department = await FindAsync(id, cancellationToken);
        if (department.IsSystem)
        {
            throw new ConflictException($"System department '{department.Code}' cannot be deactivated.");
        }

        department.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RoleResponse>> RolesAsync(int id, CancellationToken cancellationToken) =>
        [.. (await FindWithRolesAsync(id, cancellationToken)).Roles.OrderBy(r => r.Name).Select(RoleResponse.From)];

    public async Task<IReadOnlyList<RoleResponse>> AssignRolesAsync(int id, AssignRolesRequest request, CancellationToken cancellationToken)
    {
        var actor = await acting.RequireAsync(Rank.Admin, cancellationToken);
        var department = await FindWithRolesAsync(id, cancellationToken);
        var found = await roles.FindActiveAsync(request.Roles, cancellationToken);
        if (!Rank.Outranks(actor.Rank, Rank.Of(department.Roles.Concat(found).Select(r => r.Id))))
        {
            throw new ForbiddenException("You cannot change a department whose roles, old or new, are at or above your own rank.");
        }

        department.Roles.Clear();
        department.Roles.AddRange(found);
        await db.SaveChangesAsync(cancellationToken);
        return await RolesAsync(id, cancellationToken);
    }

    private async Task<Department> FindWithRolesAsync(int id, CancellationToken cancellationToken) =>
        await db.Departments.Include(d => d.Roles).SingleOrDefaultAsync(d => d.Id == id, cancellationToken)
        ?? throw new NotFoundException($"Department '{id}' was not found.");

    private async Task<Department> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Departments.FindAsync([id], cancellationToken) ?? throw new NotFoundException($"Department '{id}' was not found.");
}
