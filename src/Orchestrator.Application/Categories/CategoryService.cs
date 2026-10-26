using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.Categories;

public sealed class CategoryService(IAppDbContext db)
{
    public async Task<IReadOnlyList<CategoryResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(cancellationToken)).Select(CategoryResponse.From)];

    public async Task<CategoryResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        CategoryResponse.From(await FindAsync(id, cancellationToken));

    // Duplicate names are rejected by the unique index and mapped to 409 by GlobalExceptionHandler.
    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = new Category { Name = request.Name.Trim(), Description = request.Description };
        db.Categories.Add(category);
        await db.SaveChangesAsync(cancellationToken);
        return CategoryResponse.From(category);
    }

    public async Task<CategoryResponse> UpdateAsync(int id, CategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await FindAsync(id, cancellationToken);
        category.Name = request.Name.Trim();
        category.Description = request.Description;
        await db.SaveChangesAsync(cancellationToken);
        return CategoryResponse.From(category);
    }

    public async Task DeactivateAsync(int id, CancellationToken cancellationToken)
    {
        var category = await FindAsync(id, cancellationToken);
        category.IsActive = false;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateAsync(int id, CancellationToken cancellationToken)
    {
        var category = await FindAsync(id, cancellationToken);
        category.IsActive = true;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Category> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Categories.FindAsync([id], cancellationToken) ?? throw new NotFoundException($"Catalog category '{id}' was not found.");
}
