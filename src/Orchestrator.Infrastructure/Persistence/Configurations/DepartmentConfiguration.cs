using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestrator.Domain;

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
            Seed(2, "Administratives", "ADMINISTRATIVES"),
            Seed(3, "Accounting", "ACCOUNTING"),
            Seed(4, "Quality Check", "QUALITY_CHECK"),
            Seed(6, "Customers", "CUSTOMERS"),
            Seed(7, "Unassigned", "UNASSIGNED"),
            Seed(SystemIds.EstimationDepartment, "Estimation", "ESTIMATION"));

        builder.HasMany(d => d.Roles).WithMany(r => r.Departments).UsingEntity(j =>
        {
            j.ToTable("department_roles");
            j.HasData(new { DepartmentsId = 2, RolesId = 2 }, new { DepartmentsId = 6, RolesId = 5 });
        });
    }

    private static object Seed(int id, string name, string code) =>
        new { Id = id, Name = name, Code = code, IsActive = true, IsSystem = true, InsertedAt = SeededAt, LastUpdatedAt = SeededAt };
}
