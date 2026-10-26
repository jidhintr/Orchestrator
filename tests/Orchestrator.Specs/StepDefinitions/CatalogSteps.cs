using System.Text.Json;
using Orchestrator.Specs.Support;

namespace Orchestrator.Specs.StepDefinitions;

[Binding]
public sealed class CatalogSteps(ApiContext api)
{
    private static readonly int[] JobTypeIds = [1, 2, 3];

    [Given("a catalog with SKU {string} exists")]
    public async Task GivenACatalogExists(string sku)
    {
        await api.SendAsync("POST", "/api/v1/catalogs", JsonSerializer.Serialize(new { sku = api.Expand(sku), name = $"Catalog {api.Expand(sku)}", jobTypeIds = JobTypeIds }));
        Assert.Equal(201, (int)api.Status);
        api.Aliases["catalog:current"] = api.LastId!;
    }

    [Given("an order exists for the catalog")]
    public Task GivenAnOrderExistsForTheCatalog() => ApiHost.AddOrderAsync(int.Parse(api.LastId!, System.Globalization.CultureInfo.InvariantCulture));

    [Then("the catalog job types are {string}")]
    public void ThenTheCatalogJobTypesAre(string expected) =>
        Assert.Equal(expected, string.Join(",", api.Json!.Value.GetProperty("steps").EnumerateArray().Select(s => s.GetProperty("jobTypeName").GetString())));

    [Given("an approval chain for the catalog with the departments {string} exists")]
    public async Task GivenAnApprovalChainExists(string departmentCodes)
    {
        var departmentIds = departmentCodes.Split(',').Select(code => int.Parse(api.Expand("{dept:" + code.Trim() + "}"), System.Globalization.CultureInfo.InvariantCulture));
        await api.SendAsSuperAdminAsync("POST", "/api/v1/approval-chains", JsonSerializer.Serialize(new { name = $"Chain {api.Unique}", catalogId = int.Parse(api.Aliases["catalog:current"], System.Globalization.CultureInfo.InvariantCulture), departmentIds }));
        Assert.Equal(201, (int)api.Status);
    }

    [Then("the approval chain departments are {string}")]
    public void ThenTheApprovalChainDepartmentsAre(string expected) =>
        Assert.Equal(api.Expand(expected), string.Join(",", api.Json!.Value.GetProperty("stages").EnumerateArray().Select(s => s.GetProperty("departmentName").GetString())));

    [Given("a catalog category {string} exists")]
    public async Task GivenACategoryExists(string name)
    {
        await api.SendAsSuperAdminAsync("POST", "/api/v1/categories", JsonSerializer.Serialize(new { name = api.Expand(name) }));
        Assert.Equal(201, (int)api.Status);
    }

    [Given("a job type {string} exists")]
    public async Task GivenAJobTypeExists(string name)
    {
        await api.SendAsSuperAdminAsync("POST", "/api/v1/job-types", JsonSerializer.Serialize(new { name = api.Expand(name) }));
        Assert.Equal(201, (int)api.Status);
    }
}
