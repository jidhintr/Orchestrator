using System.ComponentModel.DataAnnotations;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Api.ApprovalChains;

public sealed record UpdateApprovalChainRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public required string Name { get; init; }

    [StringLength(250)]
    public string? Description { get; init; }

    [Required, MinLength(1)]
    public required IReadOnlyList<int> DepartmentIds { get; init; }
}

public sealed record CreateApprovalChainRequest
{
    [Required, StringLength(80, MinimumLength = 2)]
    public required string Name { get; init; }

    [StringLength(250)]
    public string? Description { get; init; }

    [Range(1, int.MaxValue)]
    public int CatalogId { get; init; }

    [Required, MinLength(1)]
    public required IReadOnlyList<int> DepartmentIds { get; init; }
}

public sealed record ApprovalStageResponse(int Sequence, int DepartmentId, string DepartmentName);

public sealed record ApprovalChainResponse(
    int Id, string Name, string? Description, bool IsDefault, int? CatalogId, IReadOnlyList<ApprovalStageResponse> Stages)
{
    public static ApprovalChainResponse From(ApprovalChain c) => new(
        c.Id, c.Name, c.Description, c.IsDefault, c.CatalogId,
        [.. c.Stages.OrderBy(s => s.Sequence).Select(s => new ApprovalStageResponse(s.Sequence, s.DepartmentId, s.Department!.Name))]);
}
