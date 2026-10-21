using Microsoft.EntityFrameworkCore;
using Orchestrator.Domain;

namespace Orchestrator.Application;

public interface IAppDbContext
{
    DbSet<Department> Departments { get; }

    DbSet<User> Users { get; }

    DbSet<Role> Roles { get; }

    DbSet<JobType> JobTypes { get; }

    DbSet<Catalog> Catalogs { get; }

    DbSet<CatalogJobStep> CatalogJobSteps { get; }

    DbSet<Order> Orders { get; }

    DbSet<Client> Clients { get; }

    DbSet<ApprovalChain> ApprovalChains { get; }

    DbSet<ApprovalChainStage> ApprovalChainStages { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
