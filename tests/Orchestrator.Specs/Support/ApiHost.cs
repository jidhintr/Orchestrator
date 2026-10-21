using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Orchestrator.Specs.Support;

internal static class ApiHost
{
    private static PostgreSqlContainer? _postgres;
    private static WebApplicationFactory<Program>? _factory;
    private static WebApplicationFactory<Program>? _brokenDbFactory;

    public static HttpClient CreateClient(bool brokenDatabase = false) =>
        (brokenDatabase ? _brokenDbFactory : _factory ?? throw new InvalidOperationException("ApiHost has not been started."))!
        .CreateClient();

    public static async Task AddOrderAsync(int catalogId)
    {
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        db.Orders.Add(new Order { CatalogId = catalogId });
        await db.SaveChangesAsync();
    }

    public static async Task StartAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Orchestrator", _postgres.GetConnectionString());
        });

        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>().Database.MigrateAsync();
        }

        _brokenDbFactory = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Orchestrator", "Host=localhost;Port=1;Database=x;Username=x;Password=x;Timeout=2"));
    }

    public static async Task StopAsync()
    {
        if (_brokenDbFactory is not null)
        {
            await _brokenDbFactory.DisposeAsync();
        }

        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        if (_postgres is not null)
        {
            await _postgres.DisposeAsync();
        }
    }
}
