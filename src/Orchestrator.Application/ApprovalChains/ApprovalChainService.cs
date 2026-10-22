using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.ApprovalChains;

public sealed class ApprovalChainService(IAppDbContext db)
{
    public async Task<IReadOnlyList<ApprovalChainResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await WithStages().AsNoTracking().OrderByDescending(c => c.IsDefault).ThenBy(c => c.Name).ToListAsync(cancellationToken))
            .Select(ApprovalChainResponse.From)];

    public async Task<ApprovalChainResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        ApprovalChainResponse.From(await FindAsync(id, cancellationToken));

    public async Task<ApprovalChainResponse> ForCatalogAsync(int catalogId, CancellationToken cancellationToken)
    {
        if (!await db.Catalogs.AnyAsync(c => c.Id == catalogId, cancellationToken))
        {
            throw new NotFoundException($"Catalog '{catalogId}' was not found.");
        }

        var chain = await WithStages().AsNoTracking().SingleOrDefaultAsync(c => c.CatalogId == catalogId && c.IsActive, cancellationToken)
            ?? await WithStages().AsNoTracking().SingleAsync(c => c.IsDefault, cancellationToken);
        return ApprovalChainResponse.From(chain);
    }

    // A second chain for the same catalog is rejected by the unique index and mapped to 409 by GlobalExceptionHandler.
    public async Task<ApprovalChainResponse> CreateAsync(CreateApprovalChainRequest request, CancellationToken cancellationToken)
    {
        if (!await db.Catalogs.AnyAsync(c => c.Id == request.CatalogId, cancellationToken))
        {
            throw new BadRequestException($"Catalog '{request.CatalogId}' does not exist.");
        }

        var chain = new ApprovalChain { Name = request.Name.Trim(), Description = request.Description, CatalogId = request.CatalogId };
        await SetStagesAsync(chain, request.DepartmentIds, cancellationToken);
        db.ApprovalChains.Add(chain);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(chain.Id, cancellationToken);
    }

    public async Task<ApprovalChainResponse> UpdateAsync(int id, UpdateApprovalChainRequest request, CancellationToken cancellationToken)
    {
        var chain = await FindAsync(id, cancellationToken);
        chain.Name = request.Name.Trim();
        chain.Description = request.Description;
        db.ApprovalChainStages.RemoveRange(chain.Stages);
        chain.Stages.Clear();
        await SetStagesAsync(chain, request.DepartmentIds, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(id, cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var chain = await FindAsync(id, cancellationToken);
        if (chain.IsDefault)
        {
            throw new ConflictException("The default approval chain cannot be deleted.");
        }

        db.ApprovalChains.Remove(chain);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        var chain = await FindAsync(id, cancellationToken);
        if (chain.IsDefault)
        {
            throw new ConflictException("The default approval chain cannot be deactivated.");
        }

        chain.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(int id, CancellationToken cancellationToken)
    {
        var chain = await FindAsync(id, cancellationToken);
        chain.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SetStagesAsync(ApprovalChain chain, IReadOnlyList<int> departmentIds, CancellationToken cancellationToken)
    {
        if (departmentIds.Distinct().Count() != departmentIds.Count)
        {
            throw new BadRequestException("A department can appear only once in a chain.");
        }

        if (await db.Departments.CountAsync(d => departmentIds.Contains(d.Id) && d.IsActive, cancellationToken) != departmentIds.Count)
        {
            throw new BadRequestException("Every department must exist and be active.");
        }

        chain.Stages.AddRange(departmentIds.Select((departmentId, index) => new ApprovalChainStage { DepartmentId = departmentId, Sequence = index + 1 }));
    }

    private IQueryable<ApprovalChain> WithStages() => db.ApprovalChains.Include(c => c.Stages).ThenInclude(s => s.Department);

    private async Task<ApprovalChain> FindAsync(int id, CancellationToken cancellationToken) =>
        await WithStages().SingleOrDefaultAsync(c => c.Id == id, cancellationToken) ?? throw new NotFoundException($"Approval chain '{id}' was not found.");
}
