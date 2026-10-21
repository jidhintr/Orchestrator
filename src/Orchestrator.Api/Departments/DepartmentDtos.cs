using System.ComponentModel.DataAnnotations;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Api.Departments;

public sealed record CreateDepartmentRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [Required, StringLength(32, MinimumLength = 2), RegularExpression("^[A-Z0-9_]+$")] string Code,
    [StringLength(500)] string? Description);

public sealed record UpdateDepartmentRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [StringLength(500)] string? Description);

public sealed record DepartmentResponse(int Id, string Name, string Code, string? Description, bool IsActive, bool IsSystem)
{
    public static DepartmentResponse From(Department d) => new(d.Id, d.Name, d.Code, d.Description, d.IsActive, d.IsSystem);
}
