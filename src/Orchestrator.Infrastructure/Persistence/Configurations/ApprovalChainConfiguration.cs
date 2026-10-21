using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orchestrator.Domain;

namespace Orchestrator.Infrastructure.Persistence.Configurations;

internal sealed class ApprovalChainConfiguration : IEntityTypeConfiguration<ApprovalChain>
{
    private static readonly DateTime SeededAt = new(2026, 10, 21, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<ApprovalChain> builder)
    {
        builder.ToTable("approval_chains");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(80);
        builder.Property(c => c.Description).HasMaxLength(250);
        builder.HasIndex(c => c.IsDefault).IsUnique().HasFilter("\"IsDefault\"");
        builder.HasIndex(c => c.CatalogId).IsUnique();
        builder.HasOne(c => c.Catalog).WithMany().HasForeignKey(c => c.CatalogId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(c => c.Stages).WithOne().HasForeignKey(s => s.ApprovalChainId).OnDelete(DeleteBehavior.Cascade);

        // The default chain starts with no stages; admins configure its departments.
        builder.HasData(new { Id = 1, Name = "Default", IsDefault = true, IsActive = true, InsertedAt = SeededAt, LastUpdatedAt = SeededAt });
    }
}

internal sealed class ApprovalChainStageConfiguration : IEntityTypeConfiguration<ApprovalChainStage>
{
    public void Configure(EntityTypeBuilder<ApprovalChainStage> builder)
    {
        builder.ToTable("approval_chain_stages");
        builder.HasKey(s => s.Id);
        builder.HasOne(s => s.Department).WithMany().HasForeignKey(s => s.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
