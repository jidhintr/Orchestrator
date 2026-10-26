using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.Catalogs;

public sealed class CatalogService(IAppDbContext db, CurrentUser currentUser)
{
    public async Task<IReadOnlyList<CatalogResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await WithSteps().AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken)).Select(CatalogResponse.From)];

    public async Task<CatalogResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        CatalogResponse.From(await FindAsync(id, cancellationToken));

    // Duplicate SKUs are rejected by the unique index and mapped to 409 by GlobalExceptionHandler.
    public async Task<CatalogResponse> CreateAsync(CatalogRequest request, CancellationToken cancellationToken)
    {
        var catalog = new Catalog { Sku = request.Sku.Trim(), Name = request.Name.Trim() };
        await ApplyAsync(catalog, request, cancellationToken);
        await SetStepsAsync(catalog, request.JobTypeIds, request.StandardMinutes, cancellationToken);
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
        await ApplyAsync(catalog, request, cancellationToken);
        db.CatalogJobSteps.RemoveRange(catalog.CatalogJobSteps);
        catalog.CatalogJobSteps.Clear();
        await SetStepsAsync(catalog, request.JobTypeIds, request.StandardMinutes, cancellationToken);
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

    public async Task ActivateAsync(int id, CancellationToken cancellationToken)
    {
        var catalog = await FindAsync(id, cancellationToken);
        catalog.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<CatalogResponse> ApproveAsync(int id, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Admin, cancellationToken);
        var catalog = await FindAsync(id, cancellationToken);
        if (catalog.ApprovedAt is not null)
        {
            throw new ConflictException($"Catalog '{catalog.Sku}' is already approved.");
        }

        catalog.ApprovedAt = DateTime.UtcNow;
        catalog.ApprovedByUserId = actor.Id;
        await db.SaveChangesAsync(cancellationToken);
        return CatalogResponse.From(catalog);
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
            CategoryId = source.CategoryId,
            AvailableFrom = source.AvailableFrom,
            AvailableTo = source.AvailableTo,
            BasedOnCatalogId = source.Id,
        };
        clone.CatalogJobSteps.AddRange(source.CatalogJobSteps.Select(s => new CatalogJobStep { JobTypeId = s.JobTypeId, Sequence = s.Sequence, StandardMinutes = s.StandardMinutes }));
        db.Catalogs.Add(clone);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(clone.Id, cancellationToken);
    }

    private async Task ApplyAsync(Catalog catalog, CatalogRequest request, CancellationToken cancellationToken)
    {
        if (request.AvailableFrom > request.AvailableTo)
        {
            throw new BadRequestException("availableFrom cannot be after availableTo.");
        }

        if (request.CategoryId is { } categoryId && categoryId != catalog.CategoryId
            && !await db.Categories.AnyAsync(c => c.Id == categoryId && c.IsActive, cancellationToken))
        {
            throw new BadRequestException($"Category {categoryId} does not exist or is deactivated.");
        }

        catalog.CategoryId = request.CategoryId;
        catalog.AvailableFrom = request.AvailableFrom;
        catalog.AvailableTo = request.AvailableTo;
        catalog.Description = request.Description;
        catalog.BasePrice = request.BasePrice;
        catalog.CatalogDiscountPercent = request.CatalogDiscountPercent;
        catalog.LaborCost = request.LaborCost;
        catalog.CommissionPercent = request.CommissionPercent;
        catalog.ProfitPercent = request.ProfitPercent;
    }

    private async Task SetStepsAsync(Catalog catalog, IReadOnlyList<int> jobTypeIds, IReadOnlyList<int?>? standardMinutes, CancellationToken cancellationToken)
    {
        if (standardMinutes is not null && (standardMinutes.Count != jobTypeIds.Count || standardMinutes.Any(m => m is <= 0)))
        {
            throw new BadRequestException("standardMinutes must match jobTypeIds one to one, with positive values or null.");
        }

        var distinct = jobTypeIds.Distinct().ToList();
        var found = await db.JobTypes.CountAsync(j => distinct.Contains(j.Id) && j.IsActive, cancellationToken);
        if (found != distinct.Count)
        {
            throw new BadRequestException("Every job type must exist and be active.");
        }

        catalog.CatalogJobSteps.AddRange(jobTypeIds.Select((jobTypeId, index) => new CatalogJobStep { JobTypeId = jobTypeId, Sequence = index + 1, StandardMinutes = standardMinutes?[index] }));
    }

    private async Task EnsureNoOrdersAsync(Catalog catalog, string action, CancellationToken cancellationToken)
    {
        if (await db.OrderItems.AnyAsync(i => i.CatalogId == catalog.Id, cancellationToken))
        {
            throw new ConflictException($"Catalog '{catalog.Sku}' has orders and cannot be {action}; clone it and change the clone.");
        }
    }

    private IQueryable<Catalog> WithSteps() => db.Catalogs.Include(c => c.Category).Include(c => c.CatalogJobSteps).ThenInclude(s => s.JobType);

    private async Task<Catalog> FindAsync(int id, CancellationToken cancellationToken) =>
        await WithSteps().SingleOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException($"Catalog '{id}' was not found.");
}
