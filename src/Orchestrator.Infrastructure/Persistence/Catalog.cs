namespace Orchestrator.Infrastructure.Persistence;

public sealed class Catalog : BaseEntity
{
    public required string Sku { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public decimal BasePrice { get; set; }

    public decimal CatalogDiscountPercent { get; set; }

    public decimal LaborCost { get; set; }

    public decimal CommissionPercent { get; set; }

    public decimal ProfitPercent { get; set; }

    public List<CatalogJobStep> CatalogJobSteps { get; } = [];
}
