using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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

internal sealed class PriorityConfiguration : IEntityTypeConfiguration<Priority>
{
    private static readonly DateTime SeededAt = new(2026, 10, 23, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Priority> builder)
    {
        builder.ToTable("priorities");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(50).UseCollation(OrchestratorDbContext.CaseInsensitive);
        builder.Property(p => p.Cost).HasPrecision(18, 2);
        builder.HasIndex(p => p.Name).IsUnique();

        // Placeholder costs pending owner confirmation; seed ids stay below FirstGeneratedId.
        builder.HasData(Seed(1, "Normal", 1, 0m), Seed(2, "High", 2, 100m), Seed(3, "Premium", 3, 250m), Seed(4, "Ultra High", 4, 500m));
    }

    private static object Seed(int id, string name, int rank, decimal cost) =>
        new { Id = id, Name = name, Rank = rank, Cost = cost, IsActive = true, InsertedAt = SeededAt, LastUpdatedAt = SeededAt };
}

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Reference).HasMaxLength(100);
        builder.Property(o => o.Notes).HasMaxLength(1000);
        builder.Property(o => o.PriorityCost).HasPrecision(18, 2);
        builder.Property(o => o.SubTotal).HasPrecision(18, 2);
        builder.Property(o => o.DiscountTotal).HasPrecision(18, 2);
        builder.Property(o => o.NetAmount).HasPrecision(18, 2);
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(o => o.Client).WithMany().HasForeignKey(o => o.ClientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(o => o.Priority).WithMany().HasForeignKey(o => o.PriorityId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(o => o.ReturnedToDepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(o => o.Items).WithOne(i => i.Order).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Approvals).WithOne().HasForeignKey(a => a.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Events).WithOne().HasForeignKey(e => e.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Description).HasMaxLength(500);
        builder.Property(i => i.Specs).HasColumnType("jsonb").HasConversion(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => JsonSerializer.Deserialize<List<ItemSpec>>(v, (JsonSerializerOptions?)null) ?? new List<ItemSpec>(),
            new ValueComparer<List<ItemSpec>>((a, b) => a!.SequenceEqual(b!), v => v.Count, v => v.ToList()));
        builder.Property(i => i.CatalogSku).HasMaxLength(50);
        builder.Property(i => i.CatalogName).HasMaxLength(150);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.LineNet).HasPrecision(18, 2);
        builder.Property(i => i.CatalogDiscountPercent).HasPrecision(5, 2);
        builder.Property(i => i.ClientDiscountPercent).HasPrecision(5, 2);
        builder.HasOne(i => i.Catalog).WithMany().HasForeignKey(i => i.CatalogId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(i => i.Steps).WithOne(s => s.OrderItem).HasForeignKey(s => s.OrderItemId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.CatalogId);
    }
}

internal sealed class OrderItemJobStepConfiguration : IEntityTypeConfiguration<OrderItemJobStep>
{
    public void Configure(EntityTypeBuilder<OrderItemJobStep> builder)
    {
        builder.ToTable("order_item_job_steps");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.JobTypeName).HasMaxLength(100);
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);
        builder.HasOne(s => s.JobType).WithMany().HasForeignKey(s => s.JobTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(s => s.LaborCost).HasPrecision(18, 2);
        builder.Property(s => s.CommissionPercent).HasPrecision(5, 2);
        builder.Property(s => s.ProfitPercent).HasPrecision(5, 2);
        builder.Property(s => s.EarnedAmount).HasPrecision(18, 2);
        builder.HasOne(s => s.Department).WithMany().HasForeignKey(s => s.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.AssignedUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.CompletedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(s => s.Logs).WithOne().HasForeignKey(l => l.OrderItemJobStepId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(s => new { s.OrderItemId, s.SortOrder }).IsUnique();
        builder.HasIndex(s => s.JobTypeId);
        builder.HasIndex(s => new { s.DepartmentId, s.Status });
        builder.HasIndex(s => s.AssignedUserId);
    }
}

internal sealed class OrderItemJobStepLogConfiguration : IEntityTypeConfiguration<OrderItemJobStepLog>
{
    public void Configure(EntityTypeBuilder<OrderItemJobStepLog> builder)
    {
        builder.ToTable("order_item_job_step_logs");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.FromStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.ToStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.EntryType).HasConversion<string>().HasMaxLength(20);
        builder.Property(l => l.Note).HasMaxLength(500);
        builder.HasOne<User>().WithMany().HasForeignKey(l => l.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(l => l.OrderItemJobStepId);
    }
}

internal sealed class OrderApprovalConfiguration : IEntityTypeConfiguration<OrderApproval>
{
    public void Configure(EntityTypeBuilder<OrderApproval> builder)
    {
        builder.ToTable("order_approvals");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Decision).HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Reason).HasMaxLength(500);
        builder.HasOne(a => a.Department).WithMany().HasForeignKey(a => a.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(a => new { a.OrderId, a.Sequence }).IsUnique();
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
