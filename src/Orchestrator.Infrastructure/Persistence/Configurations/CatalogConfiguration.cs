using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestrator.Domain;

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
        builder.Property(o => o.Reference).HasMaxLength(100);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.UnitPrice).HasPrecision(18, 2);
        builder.Property(o => o.QuotedTotal).HasPrecision(18, 2);
        builder.Property(o => o.CatalogDiscountPercent).HasPrecision(5, 2);
        builder.Property(o => o.ClientDiscountPercent).HasPrecision(5, 2);
        builder.Property(o => o.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(o => o.Client).WithMany().HasForeignKey(o => o.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Catalog).WithMany().HasForeignKey(o => o.CatalogId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(o => o.ReturnedToDepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Stages).WithOne().HasForeignKey(s => s.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Events).WithOne().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderStageConfiguration : IEntityTypeConfiguration<OrderStage>
{
    public void Configure(EntityTypeBuilder<OrderStage> builder)
    {
        builder.ToTable("order_stages");
        builder.HasKey(s => s.Id);
        builder.HasOne(s => s.JobType).WithMany().HasForeignKey(s => s.JobTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.OrderId, s.Sequence }).IsUnique();
    }
}

internal sealed class OrderEventHistoryConfiguration : IEntityTypeConfiguration<OrderEventHistory>
{
    public void Configure(EntityTypeBuilder<OrderEventHistory> builder)
    {
        builder.ToTable("order_event_history");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Type).HasConversion<string>().HasMaxLength(40);
        builder.Property(e => e.FromValue).HasMaxLength(50);
        builder.Property(e => e.ToValue).HasMaxLength(50);
        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.HasOne<User>().WithMany().HasForeignKey(e => e.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(e => e.OrderId);
    }
}
