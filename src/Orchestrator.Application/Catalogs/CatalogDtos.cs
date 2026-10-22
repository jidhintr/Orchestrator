using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.Catalogs;

public sealed record CatalogRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public required string Sku { get; init; }

    [Required, StringLength(150, MinimumLength = 3)]
    public required string Name { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal BasePrice { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal CatalogDiscountPercent { get; init; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal LaborCost { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal CommissionPercent { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal ProfitPercent { get; init; }

    [Required, MinLength(1)]
    public required IReadOnlyList<int> JobTypeIds { get; init; }
}

public sealed record CloneCatalogRequest
{
    [Required, StringLength(50, MinimumLength = 3)]
    public required string Sku { get; init; }

    [Required, StringLength(150, MinimumLength = 3)]
    public required string Name { get; init; }
}

public sealed record CatalogStepResponse(int JobTypeId, string JobTypeName, int Sequence);

public sealed record CatalogResponse(
    int Id, string Sku, string Name, string? Description, decimal BasePrice, decimal CatalogDiscountPercent,
    decimal LaborCost, decimal CommissionPercent, decimal ProfitPercent, bool IsActive, IReadOnlyList<CatalogStepResponse> Steps)
{
    public static CatalogResponse From(Catalog c) => new(
        c.Id, c.Sku, c.Name, c.Description, c.BasePrice, c.CatalogDiscountPercent, c.LaborCost, c.CommissionPercent, c.ProfitPercent, c.IsActive,
        [.. c.CatalogJobSteps.OrderBy(s => s.Sequence).Select(s => new CatalogStepResponse(s.JobTypeId, s.JobType!.Name, s.Sequence))]);
}
