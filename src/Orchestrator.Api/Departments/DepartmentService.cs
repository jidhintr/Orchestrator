using Microsoft.EntityFrameworkCore;
using Orchestrator.Api.Errors;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Api.Departments;

public sealed class DepartmentService(OrchestratorDbContext db)
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

    private async Task<Department> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Departments.FindAsync([id], cancellationToken) ?? throw new NotFoundException($"Department '{id}' was not found.");
}
