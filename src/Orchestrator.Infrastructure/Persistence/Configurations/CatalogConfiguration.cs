using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orchestrator.Infrastructure.Persistence.Configurations;

internal sealed class CatalogConfiguration : IEntityTypeConfiguration<Catalog>
{
    public void Configure(EntityTypeBuilder<Catalog> builder)
    {
        builder.ToTable("catalogs");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Sku).HasMaxLength(50).UseCollation(OrchestratorDbContext.CaseInsensitive);
        builder.Property(c => c.Name).HasMaxLength(150);
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.BasePrice).HasPrecision(18, 2);
        builder.Property(c => c.LaborCost).HasPrecision(18, 2);
        builder.Property(c => c.CatalogDiscountPercent).HasPrecision(5, 2);
        builder.Property(c => c.CommissionPercent).HasPrecision(5, 2);
        builder.Property(c => c.ProfitPercent).HasPrecision(5, 2);
        builder.HasIndex(c => c.Sku).IsUnique();
        builder.HasMany(c => c.CatalogJobSteps).WithOne().HasForeignKey(s => s.CatalogId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CatalogJobStepConfiguration : IEntityTypeConfiguration<CatalogJobStep>
{
    public void Configure(EntityTypeBuilder<CatalogJobStep> builder)
    {
        builder.ToTable("catalog_job_steps");
        builder.HasKey(s => s.Id);
        builder.HasOne(s => s.JobType).WithMany().HasForeignKey(s => s.JobTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.HasOne(o => o.Catalog).WithMany().HasForeignKey(o => o.CatalogId).OnDelete(DeleteBehavior.Restrict);
    }
}
