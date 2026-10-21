using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.Departments;

public sealed record CreateDepartmentRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(32, MinimumLength = 2), RegularExpression("^[A-Z0-9_]+$")]
    public required string Code { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed record UpdateDepartmentRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public required string Name { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }
}

public sealed record DepartmentResponse(int Id, string Name, string Code, string? Description, bool IsActive, bool IsSystem)
{
    public static DepartmentResponse From(Department d) => new(d.Id, d.Name, d.Code, d.Description, d.IsActive, d.IsSystem);
}
