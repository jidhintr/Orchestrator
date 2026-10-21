namespace Orchestrator.Infrastructure.Persistence;

public sealed class User : BaseEntity
{
    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public string? Email { get; set; }

    public required string Mobile { get; set; }

    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    public List<Role> Roles { get; } = [];
}
