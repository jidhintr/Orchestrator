namespace Orchestrator.Domain;

public abstract class BaseEntity
{
    public int Id { get; init; }

    public DateTime InsertedAt { get; set; }

    public int? InsertedById { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public int? LastUpdatedById { get; set; }

    public bool IsActive { get; set; } = true;
}
