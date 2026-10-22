namespace Orchestrator.Domain;

public sealed class JobType : BaseEntity
{
    public required string Name { get; set; }

    public string? Description { get; set; }

    public decimal? LaborCost { get; set; }

    public decimal? CommissionPercent { get; set; }

    public decimal? ProfitPercent { get; set; }
}
