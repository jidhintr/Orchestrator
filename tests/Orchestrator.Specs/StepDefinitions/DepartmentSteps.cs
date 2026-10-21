using System.Text.Json;
using Orchestrator.Specs.Support;

namespace Orchestrator.Specs.StepDefinitions;

[Binding]
public sealed class DepartmentSteps(ApiContext api)
{
    [Given("the database is unreachable")]
    public void GivenTheDatabaseIsUnreachable() => api.UseBrokenDatabase = true;

    [Given("a department {string} with code {string} exists")]
    public async Task GivenADepartmentExists(string name, string code)
    {
        await api.SendAsSuperAdminAsync("POST", "/api/v1/departments", JsonSerializer.Serialize(new { name, code }));
        Assert.Equal(201, (int)api.Status);
        api.Aliases["dept:" + api.Expand(code)] = api.LastId!;
    }

    [Given("I have the id of the department with code {string}")]
    public async Task GivenIHaveTheIdOfTheDepartmentWithCode(string code)
    {
        await api.SendAsync("GET", "/api/v1/departments");
        var match = api.Json!.Value.EnumerateArray().Single(d => d.GetProperty("code").GetString() == code);
        api.LastId = match.GetProperty("id").ToString();
    }

    [Then("the response list contains the departments:")]
    public void ThenTheResponseListContainsTheDepartments(DataTable table)
    {
        var items = api.Json!.Value.EnumerateArray().ToList();
        foreach (var row in table.Rows)
        {
            Assert.Contains(items, d =>
                d.GetProperty("code").GetString() == row["code"] &&
                d.GetProperty("name").GetString() == row["name"] &&
                d.GetProperty("isSystem").GetBoolean());
        }
    }
}
