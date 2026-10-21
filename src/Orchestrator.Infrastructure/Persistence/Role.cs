namespace Orchestrator.Infrastructure.Persistence;

public sealed class Role : BaseEntity
{
    public required string Name { get; set; }

    public required string Description { get; set; }

    public bool IsSystem { get; init; }

    public List<User> Users { get; } = [];

    public List<Department> Departments { get; } = [];
}
