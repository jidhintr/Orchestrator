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
    }

    [Given("an order exists for the catalog")]
    public Task GivenAnOrderExistsForTheCatalog() => ApiHost.AddOrderAsync(int.Parse(api.LastId!, System.Globalization.CultureInfo.InvariantCulture));

    [Then("the catalog job types are {string}")]
    public void ThenTheCatalogJobTypesAre(string expected) =>
        Assert.Equal(expected, string.Join(",", api.Json!.Value.GetProperty("steps").EnumerateArray().Select(s => s.GetProperty("jobTypeName").GetString())));
}
