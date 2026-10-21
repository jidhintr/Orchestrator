using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orchestrator.Infrastructure.Persistence.Configurations;

internal sealed class JobTypeConfiguration : IEntityTypeConfiguration<JobType>
{
    private static readonly DateTime SeededAt = new(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<JobType> builder)
    {
        builder.ToTable("job_types");
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Name).HasMaxLength(100).UseCollation(OrchestratorDbContext.CaseInsensitive);
        builder.Property(j => j.Description).HasMaxLength(500);
        builder.Property(j => j.LaborCost).HasPrecision(18, 2);
        builder.Property(j => j.CommissionPercent).HasPrecision(5, 2);
        builder.Property(j => j.ProfitPercent).HasPrecision(5, 2);
        builder.HasIndex(j => j.Name).IsUnique();

        // Placeholder job types pending owner confirmation; ids stay below FirstGeneratedId.
        builder.HasData(Seed(1, "Cutting"), Seed(2, "Laminating"), Seed(3, "Quality Assurance"), Seed(4, "Delivery"));
    }

    private static object Seed(int id, string name) =>
        new { Id = id, Name = name, IsActive = true, InsertedAt = SeededAt, LastUpdatedAt = SeededAt };
}
