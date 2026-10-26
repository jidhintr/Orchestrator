namespace Orchestrator.Domain;

public sealed class Category : BaseEntity
{
    public required string Name { get; set; }

    public string? Description { get; set; }
}
