using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orchestrator.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    private static readonly DateTime SeededAt = new(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.FirstName).HasMaxLength(50);
        builder.Property(u => u.LastName).HasMaxLength(50);
        builder.Property(u => u.Email).HasMaxLength(254).UseCollation(OrchestratorDbContext.CaseInsensitive);
        builder.Property(u => u.Mobile).HasMaxLength(20);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasOne(u => u.Department).WithMany().HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        // Placeholder identities pending owner confirmation; ids stay below FirstGeneratedId.
        builder.HasData(
            Seed(1, "Super", "Admin", "superadmin@orchestrator.local", 1),
            Seed(2, "Admin", "User", "admin@orchestrator.local", 2));

        builder.HasMany(u => u.Roles).WithMany(r => r.Users).UsingEntity(j => j.ToTable("user_roles"));
    }

    private static object Seed(int id, string firstName, string lastName, string email, int departmentId) =>
        new { Id = id, FirstName = firstName, LastName = lastName, Email = email, Mobile = "0000000000", DepartmentId = departmentId, IsActive = true, InsertedAt = SeededAt, LastUpdatedAt = SeededAt };
}
