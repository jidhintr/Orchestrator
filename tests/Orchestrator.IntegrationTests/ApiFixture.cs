using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
    public const int Admin = 2;
    public const int UnknownId = 999_999;

    public async ValueTask InitializeAsync()
    {
        _postgres = new PostgreSqlBuilder("postgres:17-alpine").Build();
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Orchestrator", _postgres.GetConnectionString());
        });

        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>().Database.MigrateAsync();

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
        if (actor is not null)
        {
            request.Headers.Add("X-Acting-User-Id", actor.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
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

    public Task<Reply> Get(string path, int? actor = null) => Send(HttpMethod.Get, "/api/v1/" + path, null, actor);

    public Task<Reply> Post(string path, object? body = null, int? actor = SuperAdmin) => Send(HttpMethod.Post, "/api/v1/" + path, body, actor);

    public Task<Reply> Put(string path, object body, int? actor = SuperAdmin) => Send(HttpMethod.Put, "/api/v1/" + path, body, actor);

    public Task<Reply> Delete(string path, int? actor = SuperAdmin) => Send(HttpMethod.Delete, "/api/v1/" + path, null, actor);

    public async Task AddOrder(int catalogId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrchestratorDbContext>();
        db.Orders.Add(new Order { CatalogId = catalogId });
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

    public async Task<int> NewUser(int[]? departments = null, string[]? roles = null, int actor = SuperAdmin) =>
        Created(await Post("users", new { firstName = "User", lastName = Unique(), email = $"u{Unique()}@test.local", mobile = "0123456789", departmentIds = departments, roles }, actor), r => r.Id);

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

    public static object ClientBody(string? email = null, string tier = "Normal") => new
    {
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
