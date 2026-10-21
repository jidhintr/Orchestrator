namespace Orchestrator.Domain;

public sealed class ApprovalChain : BaseEntity
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsDefault { get; init; }

    public int? CatalogId { get; init; }

    public Catalog? Catalog { get; set; }

    public List<ApprovalChainStage> Stages { get; } = [];
}

public sealed class ApprovalChainStage : BaseEntity
{
    public int ApprovalChainId { get; set; }

    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    public int Sequence { get; set; }
}
