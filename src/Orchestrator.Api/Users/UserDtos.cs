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

    public List<int>? DepartmentIds { get; init; }

    public List<string>? Roles { get; init; }
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

    public required bool IsActive { get; init; }
}

public sealed record AssignDepartmentsRequest
{
    [Required]
    public required List<int> DepartmentIds { get; init; }
}

public sealed record DepartmentRef(int Id, string Name);

public sealed record UserResponse(
    int Id,
    string FirstName,
    string LastName,
    string? Email,
    string Mobile,
    IReadOnlyList<DepartmentRef> Departments,
    IReadOnlyList<string> DepartmentRoles,
    IReadOnlyList<string> Roles,
    bool IsActive);
