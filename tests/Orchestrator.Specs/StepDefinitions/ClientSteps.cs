using System.Text.Json;
using Orchestrator.Specs.Support;

namespace Orchestrator.Specs.StepDefinitions;

[Binding]
public sealed class ClientSteps(ApiContext api)
{
    [Given("a client with email {string} exists")]
    public async Task GivenAClientExists(string email)
    {
        await api.SendAsync("POST", "/api/v1/clients", JsonSerializer.Serialize(new
        {
            companyName = $"Client {api.Unique}",
            contactName = "Jane Doe",
            email = api.Expand(email),
            phone = "+15551234567",
            shippingAddress = "1 Main St",
        }));
        Assert.Equal(201, (int)api.Status);
    }
}
