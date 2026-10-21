namespace Orchestrator.Infrastructure.Persistence;

public sealed class Order : BaseEntity
{
    public int CatalogId { get; set; }

    public Catalog? Catalog { get; set; }
}
