using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestrator.Domain;

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
        builder.HasIndex(u => u.Mobile).IsUnique();

        // Dev-phase Super Admin only, removed at the end of dev (docs/TODO-auth-end-of-dev.md); ids stay below FirstGeneratedId.
        builder.HasData(Seed(1, "Super", "Admin", "superadmin@orchestrator.local"));

        builder.HasMany(u => u.Roles).WithMany(r => r.Users).UsingEntity(j =>
        {
            j.ToTable("user_roles");
            j.HasData(new { UsersId = SystemIds.SuperAdminUser, RolesId = SystemIds.SuperAdminRole });
        });
        builder.HasMany(u => u.Departments).WithMany(d => d.Users).UsingEntity(j =>
        {
            j.ToTable("user_departments");
            j.HasData(new { UsersId = SystemIds.SuperAdminUser, DepartmentsId = SystemIds.AdministrativesDepartment });
        });
    }

    private static object Seed(int id, string firstName, string lastName, string email) =>
        new { Id = id, FirstName = firstName, LastName = lastName, Email = email, Mobile = "0000000000", IsActive = true, InsertedAt = SeededAt, LastUpdatedAt = SeededAt };
}
