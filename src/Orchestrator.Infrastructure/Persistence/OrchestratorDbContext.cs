using Microsoft.EntityFrameworkCore;

namespace Orchestrator.Infrastructure.Persistence;

public sealed class OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : DbContext(options)
{
    public const string Schema = "biz";
    public const string CaseInsensitive = "case_insensitive";
    public const int FirstGeneratedId = 1000;

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        // Lets unique indexes ignore case, so the database enforces "QA" == "qa".
        modelBuilder.HasCollation(schema: "public", name: CaseInsensitive, locale: "und-u-ks-level2", provider: "icu", deterministic: false);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrchestratorDbContext).Assembly);

        foreach (var entity in modelBuilder.Model.GetEntityTypes().Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
        {
            modelBuilder.Entity(entity.ClrType).Property(nameof(BaseEntity.Id))
                .UseIdentityByDefaultColumn()
                .HasIdentityOptions(startValue: FirstGeneratedId);
        }
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.InsertedAt = now;
                entry.Entity.LastUpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.LastUpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }
}
