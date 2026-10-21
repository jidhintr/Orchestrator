using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.JobTypes;

public sealed record JobTypeRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    [Range(typeof(decimal), "0", "1000000000")]
    public decimal? LaborCost { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? CommissionPercent { get; init; }

    [Range(typeof(decimal), "0", "100")]
    public decimal? ProfitPercent { get; init; }
}

public sealed record JobTypeResponse(
    int Id, string Name, string? Description, decimal? LaborCost, decimal? CommissionPercent, decimal? ProfitPercent, bool IsActive)
{
    public static JobTypeResponse From(JobType j) => new(j.Id, j.Name, j.Description, j.LaborCost, j.CommissionPercent, j.ProfitPercent, j.IsActive);
}
