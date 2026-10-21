using Microsoft.EntityFrameworkCore;
using Orchestrator.Api.Catalogs;
using Orchestrator.Api.Errors;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Api.JobTypes;

public sealed class JobTypeService(OrchestratorDbContext db)
{
    public async Task<IReadOnlyList<JobTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await db.JobTypes.AsNoTracking().OrderBy(j => j.Name).ToListAsync(cancellationToken)).Select(JobTypeResponse.From)];

    public async Task<JobTypeResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        JobTypeResponse.From(await FindAsync(id, cancellationToken));

    public async Task<IReadOnlyList<CatalogResponse>> CatalogsAsync(int id, CancellationToken cancellationToken)
    {
        await FindAsync(id, cancellationToken);
        var catalogs = await db.Catalogs.AsNoTracking()
            .Include(c => c.CatalogJobSteps).ThenInclude(s => s.JobType)
            .Where(c => c.CatalogJobSteps.Any(s => s.JobTypeId == id))
            .OrderBy(c => c.Name).ToListAsync(cancellationToken);
        return [.. catalogs.Select(CatalogResponse.From)];
    }

    // Duplicate names are rejected by the unique index and mapped to 409 by GlobalExceptionHandler.
    public async Task<JobTypeResponse> CreateAsync(JobTypeRequest request, CancellationToken cancellationToken)
    {
        var jobType = new JobType { Name = request.Name.Trim() };
        Apply(jobType, request);
        db.JobTypes.Add(jobType);
        await db.SaveChangesAsync(cancellationToken);
        return JobTypeResponse.From(jobType);
    }

    public async Task<JobTypeResponse> UpdateAsync(int id, JobTypeRequest request, CancellationToken cancellationToken)
    {
        var jobType = await FindAsync(id, cancellationToken);
        jobType.Name = request.Name.Trim();
        Apply(jobType, request);
        await db.SaveChangesAsync(cancellationToken);
        return JobTypeResponse.From(jobType);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        var jobType = await FindAsync(id, cancellationToken);
        jobType.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Apply(JobType jobType, JobTypeRequest request)
    {
        jobType.Description = request.Description;
        jobType.LaborCost = request.LaborCost;
        jobType.CommissionPercent = request.CommissionPercent;
        jobType.ProfitPercent = request.ProfitPercent;
    }

    private async Task<JobType> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.JobTypes.FindAsync([id], cancellationToken) ?? throw new NotFoundException($"Job type '{id}' was not found.");
}
