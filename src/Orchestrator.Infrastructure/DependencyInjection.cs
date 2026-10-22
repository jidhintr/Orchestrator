using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orchestrator.Application;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Orchestrator")
            ?? throw new InvalidOperationException("Connection string 'Orchestrator' is not configured. See README.md.");

        services.AddDbContext<OrchestratorDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<OrchestratorDbContext>());
        services.AddHealthChecks().AddDbContextCheck<OrchestratorDbContext>("postgres");

        return services;
    }
}
