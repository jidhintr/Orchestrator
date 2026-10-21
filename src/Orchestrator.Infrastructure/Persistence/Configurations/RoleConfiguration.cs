using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orchestrator.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    private static readonly DateTime SeededAt = new(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).HasMaxLength(50).UseCollation(OrchestratorDbContext.CaseInsensitive);
        builder.Property(r => r.Description).HasMaxLength(500);
        builder.HasIndex(r => r.Name).IsUnique();

        builder.HasData(
            Seed(1, "Super Admin", "Full system-wide access; manages all users, department pages and approval chains."),
            Seed(2, "Admin", "Full operational access; manages managers, employees and departments, but not other admins."),
            Seed(3, "Manager", "Department-scoped: manages own-department employees and approves or alters orders at their stage."),
            Seed(4, "Employee", "Executes and completes orders within their department and adds notes."),
            Seed(5, "Client", "Read-only access to their own orders; no commissions, margins or internal notes."));
    }

    private static object Seed(int id, string name, string description) =>
        new { Id = id, Name = name, Description = description, IsActive = true, IsSystem = true, InsertedAt = SeededAt, LastUpdatedAt = SeededAt };
}
