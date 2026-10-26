using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchestrator.Application.Auth;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace Orchestrator.Specs.Support;

internal static class ApiHost
{
    private const string Password = "Spec-Password-123";
    private const string SuperAdminEmail = "superadmin@orchestrator.local";

    private static readonly ConcurrentDictionary<int, string> Logins = new();
    private static readonly ConcurrentDictionary<int, Lazy<Task<string?>>> Tokens = new();

    private static PostgreSqlContainer? _postgres;
    private static WebApplicationFactory<Program>? _factory;
    private static WebApplicationFactory<Program>? _brokenDbFactory;

    public static int AdminId { get; private set; }

    public static HttpClient CreateClient(bool brokenDatabase = false) =>
        (brokenDatabase ? _brokenDbFactory : _factory ?? throw new InvalidOperationException("ApiHost has not been started."))!
        .CreateClient();

    public static void RegisterLogin(int userId, string login) => Logins[userId] = login;

    public static Task<string?> TokenAsync(int userId) => Tokens.GetOrAdd(userId, id => new Lazy<Task<string?>>(() => SignInAsync(id))).Value;

    public static async Task AddOrderAsync(int catalogId)
    {
        using var scope = _factory!.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        var client = new Client { CompanyName = "Acme", ContactName = "Jane", Email = $"o{Guid.NewGuid():N}@spec.local", Phone = "+1234567890", ShippingAddress = "1 Main St" };
        var order = new Order { Client = client, PriorityId = SystemIds.NormalPriority };
        order.Items.Add(new OrderItem { CatalogId = catalogId, CatalogSku = "SKU", CatalogName = "Catalog", Quantity = 1 });
        db.Orders.Add(order);
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
            builder.UseSetting("Auth:JwtKey", "spec-run-signing-key-0123456789-abcdefghijklmnopqrstuvwxyz");
            builder.UseSetting("Auth:LoginPermitsPerMinute", "1000000");
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
            await db.Database.MigrateAsync();

            var admin = new User { FirstName = "Admin", LastName = "Tester", Email = "admin.tester@orchestrator.local", Mobile = "0000000002" };
            admin.Departments.Add(await db.Departments.SingleAsync(d => d.Id == 2));
            db.Users.Add(admin);
            await db.SaveChangesAsync();
            AdminId = admin.Id;
            Logins[admin.Id] = admin.Email;
        }

        Logins[SystemIds.SuperAdminUser] = SuperAdminEmail;

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

    private static async Task<string?> SignInAsync(int userId)
    {
        if (!Logins.TryGetValue(userId, out var login))
        {
            return null;
        }

        using (var scope = _factory!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
            await scope.ServiceProvider.GetRequiredService<AuthService>().SetPasswordAsync(userId, Password, default);
            await db.SaveChangesAsync();
        }

        using var client = _factory!.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { login, password = Password });
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
    }
}
