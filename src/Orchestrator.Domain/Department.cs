namespace Orchestrator.Domain;

public sealed class Department : BaseEntity
{
    public required string Name { get; set; }

    public required string Code { get; init; }

    public string? Description { get; set; }

    public bool IsSystem { get; init; }

    public List<Role> Roles { get; } = [];

    public List<User> Users { get; } = [];
}
