using System.Globalization;
using System.Text.Json;
using Orchestrator.Specs.Support;

namespace Orchestrator.Specs.StepDefinitions;

[Binding]
public sealed class OrderSteps(ApiContext api)
{
    private static readonly int[] JobTypeIds = [1, 2, 3];

    [Given("a client and a catalog exist")]
    public async Task GivenAClientAndACatalogExist()
    {
        await api.SendAsSuperAdminAsync("POST", "/api/v1/clients", JsonSerializer.Serialize(ClientBody($"order{api.Unique}@example.com", null)));
        Assert.Equal(201, (int)api.Status);
        api.Aliases["client:current"] = api.LastId!;
        await api.SendAsSuperAdminAsync("POST", "/api/v1/catalogs", JsonSerializer.Serialize(new { sku = $"ORD{api.Unique}", name = $"Catalog {api.Unique}", jobTypeIds = JobTypeIds }));
        Assert.Equal(201, (int)api.Status);
        api.Aliases["catalog:current"] = api.LastId!;
        await api.SendAsSuperAdminAsync("POST", "/api/v1/approval-chains", JsonSerializer.Serialize(new { name = $"Chain {api.Unique}", catalogId = Id("catalog:current"), departmentIds = new[] { 3 } }));
        Assert.Equal(201, (int)api.Status);
    }

    [Given("an order is placed for that client and catalog")]
    public async Task GivenAnOrderIsPlaced()
    {
        await api.SendAsSuperAdminAsync("POST", "/api/v1/orders", JsonSerializer.Serialize(new { clientId = Id("client:current"), items = new[] { new { catalogId = Id("catalog:current"), quantity = 5 } } }));
        Assert.Equal(201, (int)api.Status);
        api.Aliases["order:current"] = api.LastId!;
    }

    [Given("the order is approved")]
    public async Task GivenTheOrderIsApproved()
    {
        await api.SendAsSuperAdminAsync("POST", $"/api/v1/orders/{api.Aliases["order:current"]}/approvals/approve");
        Assert.Equal(200, (int)api.Status);
    }

    [Given("I remember step {int} of the order as {string}")]
    public async Task GivenIRememberStep(int number, string name)
    {
        await api.SendAsSuperAdminAsync("GET", $"/api/v1/orders/{api.Aliases["order:current"]}");
        Assert.Equal(200, (int)api.Status);
        api.Aliases["step:" + name] = api.Json!.Value.GetProperty("items")[0].GetProperty("steps")[number - 1].GetProperty("id").ToString();
    }

    [Given("a client user {string} exists")]
    public async Task GivenAClientUserExists(string alias)
    {
        var mobile = ApiContext.NextMobile();
        await api.SendAsSuperAdminAsync("POST", "/api/v1/users", JsonSerializer.Serialize(new { firstName = alias, lastName = "Client", mobile, departmentIds = new[] { 6 } }));
        Assert.Equal(201, (int)api.Status);
        var userId = int.Parse(api.LastId!, CultureInfo.InvariantCulture);
        api.Aliases["user:" + alias] = api.LastId!;
        ApiHost.RegisterLogin(userId, mobile);
        await api.SendAsSuperAdminAsync("POST", "/api/v1/clients", JsonSerializer.Serialize(ClientBody($"{alias}{api.Unique}@example.com", userId)));
        Assert.Equal(201, (int)api.Status);
        api.Aliases["client:" + alias] = api.LastId!;
    }

    private static int Id(string alias, ApiContext api) => int.Parse(api.Aliases[alias], CultureInfo.InvariantCulture);

    private int Id(string alias) => Id(alias, api);

    private object ClientBody(string email, int? userId) => new
    {
        companyName = $"Client {api.Unique}",
        contactName = "Jane Doe",
        email,
        phone = "+15551234567",
        shippingAddress = "1 Main St",
        userId,
    };
}
