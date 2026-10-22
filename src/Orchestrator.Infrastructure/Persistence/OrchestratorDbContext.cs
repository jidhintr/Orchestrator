using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orchestrator.Application;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Infrastructure.Persistence;

public sealed class OrchestratorDbContext(DbContextOptions<OrchestratorDbContext> options) : DbContext(options), IAppDbContext
{
    public const string Schema = "biz";
    public const string IdentitySchema = "identity";
    public const string CaseInsensitive = "case_insensitive";
    public const int FirstGeneratedId = 1000;

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<JobType> JobTypes => Set<JobType>();

    public DbSet<Catalog> Catalogs => Set<Catalog>();

    public DbSet<CatalogJobStep> CatalogJobSteps => Set<CatalogJobStep>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderStage> OrderStages => Set<OrderStage>();

    public DbSet<OrderEventHistory> OrderEvents => Set<OrderEventHistory>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<ApprovalChain> ApprovalChains => Set<ApprovalChain>();

    public DbSet<ApprovalChainStage> ApprovalChainStages => Set<ApprovalChainStage>();

    public DbSet<UserCredential> Credentials => Set<UserCredential>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
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

        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException("A record with the same unique value already exists.");
        }
        catch (DbUpdateException e) when (e.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new BadRequestException("A referenced record does not exist.");
        }
    }
}
