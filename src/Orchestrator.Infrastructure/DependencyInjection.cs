using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "Orchestrator";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. See docs/operations/local-development.md.");

        services.AddDbContext<OrchestratorDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHealthChecks().AddDbContextCheck<OrchestratorDbContext>("postgres", tags: ["ready"]);

        return services;
    }
}
