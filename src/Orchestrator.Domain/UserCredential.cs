namespace Orchestrator.Domain;

public sealed class UserCredential : BaseEntity
{
    public int UserId { get; set; }

    public User? User { get; set; }

    public required string PasswordHash { get; set; }

    public DateTime PasswordChangedAt { get; set; }

    public int FailedCount { get; set; }

    public DateTime? LockoutEnd { get; set; }
}

public sealed class RefreshToken : BaseEntity
{
    public int UserId { get; set; }

    public required string TokenHash { get; set; }

    public Guid FamilyId { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? RevokedAt { get; set; }
}
