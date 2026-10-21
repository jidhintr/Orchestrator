using System.Text.Json;
using Orchestrator.Specs.Support;

namespace Orchestrator.Specs.StepDefinitions;

[Binding]
public sealed class ApiSteps(ApiContext api)
{
    [Given("I use the correlation id {string}")]
    public void GivenIUseTheCorrelationId(string correlationId) => api.CorrelationId = correlationId;

    [When("I send a {word} request to {string}")]
    public Task WhenISendARequestTo(string method, string path) => api.SendAsync(method, path);

    [When("I send a {word} request to {string} with body:")]
    public Task WhenISendARequestToWithBody(string method, string path, string body) => api.SendAsync(method, path, body);

    [Then("the response status is {int}")]
    public void ThenTheResponseStatusIs(int status) => Assert.Equal(status, (int)api.Status);

    [Then("the response is a problem with status {int}")]
    public void ThenTheResponseIsAProblem(int status)
    {
        Assert.Equal("application/problem+json", api.ContentType);
        Assert.Equal(status, (int)api.Status);
        Assert.Equal(status.ToString(System.Globalization.CultureInfo.InvariantCulture), Property("status"));
    }

    [Then("the response JSON {string} is {string}")]
    public void ThenTheResponseJsonIs(string path, string expected) => Assert.Equal(api.Expand(expected), Property(path));

    [Then("the response JSON {string} is at least {int}")]
    public void ThenTheResponseJsonIsAtLeast(string path, int minimum) => Assert.True(int.Parse(Property(path)!, System.Globalization.CultureInfo.InvariantCulture) >= minimum);

    [Then("the response JSON {string} is present")]
    public void ThenTheResponseJsonIsPresent(string path) => Assert.NotNull(Property(path));

    [Then("the response body is {string}")]
    public void ThenTheResponseBodyIs(string expected) => Assert.Equal(expected, api.Body);

    [Then("the response body does not contain {string}")]
    public void ThenTheResponseBodyDoesNotContain(string text) => Assert.DoesNotContain(text, api.Body, StringComparison.OrdinalIgnoreCase);

    [Then("the response header {string} is present")]
    public void ThenTheResponseHeaderIsPresent(string name) => Assert.True(api.Headers.ContainsKey(name), $"Header '{name}' missing.");

    [Then("the response header {string} is {string}")]
    public void ThenTheResponseHeaderIs(string name, string expected) =>
        Assert.Equal(api.Expand(expected), api.Headers.GetValueOrDefault(name));

    [Then("the response list contains an item where {string} is {string}")]
    public void ThenTheResponseListContains(string property, string expected) =>
        Assert.True(ListContains(property, expected), $"No item with {property} = '{api.Expand(expected)}'.");

    [Then("the response list does not contain an item where {string} is {string}")]
    public void ThenTheResponseListDoesNotContain(string property, string expected) =>
        Assert.False(ListContains(property, expected), $"Unexpected item with {property} = '{api.Expand(expected)}'.");

    private string? Property(string path)
    {
        var current = api.Json;
        foreach (var name in api.Expand(path).Split('.'))
        {
            if (current is { ValueKind: JsonValueKind.Object } obj && obj.TryGetProperty(name, out var child))
            {
                current = child;
            }
            else
            {
                return null;
            }
        }

        return current is null ? null : AsText(current.Value);
    }

    private bool ListContains(string property, string expected)
    {
        var json = api.Json;
        Assert.True(json is { ValueKind: JsonValueKind.Array }, "The response body is not a JSON array.");
        return json!.Value.EnumerateArray().Any(item =>
            item.TryGetProperty(property, out var value) && AsText(value) == api.Expand(expected));
    }

    private static string AsText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()!,
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => element.GetRawText(),
    };
}
