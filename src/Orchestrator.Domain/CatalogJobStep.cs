namespace Orchestrator.Domain;

public sealed class CatalogJobStep : BaseEntity
{
    public int CatalogId { get; set; }

    public int JobTypeId { get; set; }

    public JobType? JobType { get; set; }

    public int Sequence { get; set; }
}
