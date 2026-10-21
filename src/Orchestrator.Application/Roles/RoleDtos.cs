using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.Roles;

public sealed record RoleRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public required string Name { get; init; }

    [StringLength(500)]
    public required string Description { get; init; }
}

public sealed record RoleResponse(int Id, string Name, string Description, bool IsActive, bool IsSystem)
{
    public static RoleResponse From(Role r) => new(r.Id, r.Name, r.Description, r.IsActive, r.IsSystem);
}

public sealed record AssignRolesRequest
{
    [Required]
    public required List<string> Roles { get; init; }
}
