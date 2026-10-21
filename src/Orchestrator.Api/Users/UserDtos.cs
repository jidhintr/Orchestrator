using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Api.Users;

public sealed record CreateUserRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public required string FirstName { get; init; }

    [Required, StringLength(50, MinimumLength = 2)]
    public required string LastName { get; init; }

    [StringLength(254), EmailAddress]
    public string? Email { get; init; }

    [Required, StringLength(20, MinimumLength = 5), RegularExpression(@"^\+?[0-9]+$")]
    public required string Mobile { get; init; }

    public required int DepartmentId { get; init; }

    [Required, MinLength(1)]
    public required List<string> Roles { get; init; }
}

public sealed record UpdateUserRequest
{
    [Required, StringLength(50, MinimumLength = 2)]
    public required string FirstName { get; init; }

    [Required, StringLength(50, MinimumLength = 2)]
    public required string LastName { get; init; }

    [StringLength(254), EmailAddress]
    public string? Email { get; init; }

    [Required, StringLength(20, MinimumLength = 5), RegularExpression(@"^\+?[0-9]+$")]
    public required string Mobile { get; init; }

    public required int DepartmentId { get; init; }

    [Required, MinLength(1)]
    public required List<string> Roles { get; init; }

    public required bool IsActive { get; init; }
}

public sealed record UserResponse(int Id, string FirstName, string LastName, string? Email, string Mobile, int DepartmentId, string DepartmentName, IReadOnlyList<string> DepartmentRoles, IReadOnlyList<string> Roles, bool IsActive);
