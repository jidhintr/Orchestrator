using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Orchestrator.Application.Auth;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Orchestrator.IntegrationTests.ApiFixture))]

namespace Orchestrator.IntegrationTests;

public sealed record Reply(HttpStatusCode Status, string Raw, string? MediaType, IReadOnlyDictionary<string, string> Headers)
{
    public JsonElement Body => string.IsNullOrWhiteSpace(Raw) ? default : JsonDocument.Parse(Raw).RootElement;

    public int Id => Body.GetProperty("id").GetInt32();

    public string Str(string name) => Body.GetProperty(name).GetString()!;

    public JsonElement this[string name] => Body.GetProperty(name);

    public IReadOnlyList<JsonElement> Items => [.. Body.EnumerateArray()];

    public IReadOnlyList<string> Names(string property = "name") => [.. Items.Select(i => i.GetProperty(property).GetString()!)];

    public IReadOnlyList<int> Ids => [.. Items.Select(i => i.GetProperty("id").GetInt32())];

    public string? Header(string name) => Headers.GetValueOrDefault(name);
}

public sealed class ApiFixture : IAsyncLifetime
{
    private PostgreSqlContainer _postgres = null!;
    private WebApplicationFactory<Program> _factory = null!;
    private WebApplicationFactory<Program> _brokenDb = null!;

    public const int SuperAdmin = 1;
    public const int UnknownId = 999_999;

    private const string Password = "Test-Password-123";

    private static long _mobile = Random.Shared.NextInt64(1_000_000_000, 8_000_000_000);

    private readonly ConcurrentDictionary<int, string> _logins = new();
    private readonly ConcurrentDictionary<int, Lazy<Task<string?>>> _tokens = new();

    public static int Admin { get; private set; }

    public static string Mobile() => Interlocked.Increment(ref _mobile).ToString(CultureInfo.InvariantCulture);

    public async ValueTask InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Orchestrator", _postgres.GetConnectionString());
            builder.UseSetting("Auth:JwtKey", "integration-run-signing-key-0123456789-abcdefghijklmnopqrstuvwxyz");
            builder.UseSetting("Auth:LoginPermitsPerMinute", "1000000");
        });

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
            await db.Database.MigrateAsync();

            var admin = new User { FirstName = "Admin", LastName = "Tester", Email = "admin.tester@orchestrator.local", Mobile = Mobile() };
            admin.Departments.Add(await db.Departments.SingleAsync(d => d.Id == 2));
            db.Users.Add(admin);
            await db.SaveChangesAsync();
            Admin = admin.Id;
            _logins[admin.Id] = admin.Email;
        }

        _logins[SuperAdmin] = "superadmin@orchestrator.local";

        _brokenDb = _factory.WithWebHostBuilder(builder =>
            builder.UseSetting("ConnectionStrings:Orchestrator", "Host=localhost;Port=1;Database=x;Username=x;Password=x;Timeout=2"));
    }

    public async ValueTask DisposeAsync()
    {
        await _brokenDb.DisposeAsync();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public static string Unique() => Guid.NewGuid().ToString("N")[..8];

    public async Task<Reply> Send(HttpMethod method, string url, object? body = null, int? actor = SuperAdmin, string? correlationId = null, bool brokenDb = false)
    {
        using var client = (brokenDb ? _brokenDb : _factory).CreateClient();
        using var request = new HttpRequestMessage(method, url);
        if (actor is not null && await Token(actor.Value) is { } token)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (correlationId is not null)
        {
            request.Headers.Add("X-Correlation-Id", correlationId);
        }

        if (body is not null)
        {
            request.Content = new StringContent(body as string ?? JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request);
        var headers = response.Headers.Concat(response.Content.Headers).ToDictionary(h => h.Key, h => string.Join(",", h.Value), StringComparer.OrdinalIgnoreCase);
        return new Reply(response.StatusCode, await response.Content.ReadAsStringAsync(), response.Content.Headers.ContentType?.MediaType, headers);
    }

    public Task<Reply> Get(string path, int? actor = SuperAdmin) => Send(HttpMethod.Get, "/api/v1/" + path, null, actor);

    public Task<Reply> Post(string path, object? body = null, int? actor = SuperAdmin) => Send(HttpMethod.Post, "/api/v1/" + path, body, actor);

    public Task<Reply> Put(string path, object body, int? actor = SuperAdmin) => Send(HttpMethod.Put, "/api/v1/" + path, body, actor);

    public Task<Reply> Delete(string path, int? actor = SuperAdmin) => Send(HttpMethod.Delete, "/api/v1/" + path, null, actor);

    private Task<string?> Token(int userId) => _tokens.GetOrAdd(userId, id => new Lazy<Task<string?>>(() => SignIn(id))).Value;

    private async Task<string?> SignIn(int userId)
    {
        if (!_logins.TryGetValue(userId, out var login))
        {
            return null;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<AuthService>().SetPasswordAsync(userId, Password, default);
            await scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>().SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        using var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { login, password = Password });
        return response.IsSuccessStatusCode ? (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString() : null;
    }

    public async Task AddOrder(int catalogId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        db.Orders.Add(new Order { CatalogId = catalogId, ClientId = await NewClient() });
        await db.SaveChangesAsync();
    }

    private static T Created<T>(Reply reply, Func<Reply, T> pick)
    {
        Assert.True(reply.Status is HttpStatusCode.Created or HttpStatusCode.OK, $"{reply.Status}: {reply.Raw}");
        return pick(reply);
    }

    public async Task<int> NewRole(string? name = null) =>
        Created(await Post("roles", new { name = name ?? "Role" + Unique(), description = "test" }), r => r.Id);

    public async Task<int> NewDepartment(params string[] roles)
    {
        var id = Created(await Post("departments", new { name = "Dept " + Unique(), code = "D" + Unique().ToUpperInvariant() }), r => r.Id);
        if (roles.Length > 0)
        {
            Created(await Put($"departments/{id}/roles", new { roles }), r => 0);
        }

        return id;
    }

    public async Task<int> NewUser(int[]? departments = null, string[]? roles = null, int actor = SuperAdmin)
    {
        var email = $"u{Unique()}@test.local";
        var id = Created(await Post("users", new { firstName = "User", lastName = Unique(), email, mobile = Mobile(), departmentIds = departments, roles }, actor), r => r.Id);
        _logins[id] = email;
        return id;
    }

    public async Task<int> NewManager(int departmentId) => await NewUser([departmentId], ["Manager"]);

    public async Task<int> NewEmployee() => await NewUser();

    public async Task<int> NewJobType() =>
        Created(await Post("job-types", new { name = "Job " + Unique() }), r => r.Id);

    public async Task<int> NewCatalog(params int[] jobTypeIds) =>
        Created(await Post("catalogs", CatalogBody("SKU-" + Unique(), jobTypeIds.Length > 0 ? jobTypeIds : [await NewJobType()])), r => r.Id);

    public static object CatalogBody(string sku, int[] jobTypeIds, decimal basePrice = 100m) => new
    {
        sku,
        name = "Catalog " + sku,
        description = "Hardcover",
        basePrice,
        catalogDiscountPercent = 5,
        laborCost = 20,
        commissionPercent = 3,
        profitPercent = 10,
        jobTypeIds,
    };

    public static object ClientBody(string? email = null, string tier = "Normal", int? userId = null) => new
    {
        userId,
        companyName = "Company " + Unique(),
        contactName = "Jane Doe",
        email = email ?? $"c{Unique()}@acme.test",
        phone = "+1234567890",
        shippingAddress = "1 Main St",
        tier,
    };

    public async Task<int> NewClient() => Created(await Post("clients", ClientBody()), r => r.Id);
}

public static class ProblemAssert
{
    public static void Is(Reply reply, HttpStatusCode status)
    {
        Assert.Equal(status, reply.Status);
        Assert.Equal("application/problem+json", reply.MediaType);
    }
}
