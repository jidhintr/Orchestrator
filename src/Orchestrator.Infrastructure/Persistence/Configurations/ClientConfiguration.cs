using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orchestrator.Infrastructure.Persistence.Configurations;

internal sealed class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.ToTable("clients");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CompanyName).HasMaxLength(150);
        builder.Property(c => c.ContactName).HasMaxLength(100);
        builder.Property(c => c.Email).HasMaxLength(100).UseCollation(OrchestratorDbContext.CaseInsensitive);
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.ShippingAddress).HasMaxLength(500);
        builder.Property(c => c.Tier).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.TierDiscountPercent).HasPrecision(5, 2);
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
