using Microsoft.EntityFrameworkCore;
using Orchestrator.Api.Errors;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Api.Catalogs;

public sealed class CatalogService(OrchestratorDbContext db)
{
    public async Task<IReadOnlyList<CatalogResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await WithSteps().AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken)).Select(CatalogResponse.From)];

    public async Task<CatalogResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        CatalogResponse.From(await FindAsync(id, cancellationToken));

    // Duplicate SKUs are rejected by the unique index and mapped to 409 by GlobalExceptionHandler.
    public async Task<CatalogResponse> CreateAsync(CatalogRequest request, CancellationToken cancellationToken)
    {
        var catalog = new Catalog { Sku = request.Sku.Trim(), Name = request.Name.Trim() };
        Apply(catalog, request);
        await SetStepsAsync(catalog, request.JobTypeIds, cancellationToken);
        db.Catalogs.Add(catalog);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(catalog.Id, cancellationToken);
    }

    public async Task<CatalogResponse> UpdateAsync(int id, CatalogRequest request, CancellationToken cancellationToken)
    {
        var catalog = await FindAsync(id, cancellationToken);
        await EnsureNoOrdersAsync(catalog, "edited", cancellationToken);
        catalog.Sku = request.Sku.Trim();
        catalog.Name = request.Name.Trim();
        Apply(catalog, request);
        db.CatalogJobSteps.RemoveRange(catalog.CatalogJobSteps);
        catalog.CatalogJobSteps.Clear();
        await SetStepsAsync(catalog, request.JobTypeIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        var catalog = await FindAsync(id, cancellationToken);
        await EnsureNoOrdersAsync(catalog, "deleted", cancellationToken);
        catalog.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CatalogResponse> CloneAsync(int id, CloneCatalogRequest request, CancellationToken cancellationToken)
    {
        var source = await FindAsync(id, cancellationToken);
        var clone = new Catalog
        {
            Sku = request.Sku.Trim(),
            Name = request.Name.Trim(),
            Description = source.Description,
            BasePrice = source.BasePrice,
            CatalogDiscountPercent = source.CatalogDiscountPercent,
            LaborCost = source.LaborCost,
            CommissionPercent = source.CommissionPercent,
            ProfitPercent = source.ProfitPercent,
        };
        clone.CatalogJobSteps.AddRange(source.CatalogJobSteps.Select(s => new CatalogJobStep { JobTypeId = s.JobTypeId, Sequence = s.Sequence }));
        db.Catalogs.Add(clone);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(clone.Id, cancellationToken);
    }

    private static void Apply(Catalog catalog, CatalogRequest request)
    {
        catalog.Description = request.Description;
        catalog.BasePrice = request.BasePrice;
        catalog.CatalogDiscountPercent = request.CatalogDiscountPercent;
        catalog.LaborCost = request.LaborCost;
        catalog.CommissionPercent = request.CommissionPercent;
        catalog.ProfitPercent = request.ProfitPercent;
    }

    private async Task SetStepsAsync(Catalog catalog, IReadOnlyList<int> jobTypeIds, CancellationToken cancellationToken)
    {
        var distinct = jobTypeIds.Distinct().ToList();
        var found = await db.JobTypes.CountAsync(j => distinct.Contains(j.Id) && j.IsActive, cancellationToken);
        if (found != distinct.Count)
        {
            throw new BadRequestException("Every job type must exist and be active.");
        }

        catalog.CatalogJobSteps.AddRange(jobTypeIds.Select((jobTypeId, index) => new CatalogJobStep { JobTypeId = jobTypeId, Sequence = index + 1 }));
    }

    private async Task EnsureNoOrdersAsync(Catalog catalog, string action, CancellationToken cancellationToken)
    {
        if (await db.Orders.AnyAsync(o => o.CatalogId == catalog.Id, cancellationToken))
        {
            throw new ConflictException($"Catalog '{catalog.Sku}' has orders and cannot be {action}; clone it and change the clone.");
        }
    }

    private IQueryable<Catalog> WithSteps() => db.Catalogs.Include(c => c.CatalogJobSteps).ThenInclude(s => s.JobType);

    private async Task<Catalog> FindAsync(int id, CancellationToken cancellationToken) =>
        await WithSteps().SingleOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException($"Catalog '{id}' was not found.");
}
