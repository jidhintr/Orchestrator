using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orchestrator.Infrastructure.Persistence.Configurations;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    private static readonly DateTime SeededAt = new(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(100).UseCollation(OrchestratorDbContext.CaseInsensitive);
        builder.Property(d => d.Code).HasMaxLength(32);
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.HasIndex(d => d.Code).IsUnique();
        builder.HasIndex(d => d.Name).IsUnique();

        // Seed ids stay below FirstGeneratedId so generated ids never collide with them. Names and codes are placeholders pending owner confirmation.
        builder.HasData(
            Seed(1, "Super Admin", "SUPER_ADMIN"),
            Seed(2, "Admin", "ADMIN"),
            Seed(3, "Accounts", "ACCOUNTS"),
            Seed(4, "QA", "QA"),
            Seed(5, "Delivery", "DELIVERY"),
            Seed(6, "Clients", "CLIENTS"));

        builder.HasMany(d => d.Roles).WithMany(r => r.Departments).UsingEntity(j =>
        {
            j.ToTable("department_roles");
            j.HasData(new { DepartmentsId = 1, RolesId = 1 }, new { DepartmentsId = 2, RolesId = 2 });
        });
    }

    private static object Seed(int id, string name, string code) =>
        new { Id = id, Name = name, Code = code, IsActive = true, IsSystem = true, InsertedAt = SeededAt, LastUpdatedAt = SeededAt };
}
