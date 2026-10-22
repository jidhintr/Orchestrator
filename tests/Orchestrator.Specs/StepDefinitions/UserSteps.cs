using System.Text.Json;
using Orchestrator.Specs.Support;

namespace Orchestrator.Specs.StepDefinitions;

[Binding]
public sealed class UserSteps(ApiContext api)
{
    [Given("I act as {string}")]
    public void GivenIActAs(string who) => api.ActingUserId = api.Aliases.GetValueOrDefault("user:" + who, who);

    [Given("I act as nobody")]
    public void GivenIActAsNobody() => api.ActingUserId = null;

    [Given("a role {string} exists")]
    public async Task GivenARoleExists(string name)
    {
        await api.SendAsSuperAdminAsync("POST", "/api/v1/roles", JsonSerializer.Serialize(new { name = api.Expand(name), description = "test role" }));
        Assert.Equal(201, (int)api.Status);
        api.Aliases["role:" + api.Expand(name)] = api.LastId!;
    }

    [Given("the department with code {string} has the roles {string}")]
    public async Task GivenTheDepartmentHasTheRoles(string code, string roles)
    {
        await api.SendAsSuperAdminAsync("PUT", "/api/v1/departments/{dept:" + code + "}/roles", JsonSerializer.Serialize(new { roles = Split(roles) }));
        Assert.Equal(200, (int)api.Status);
    }

    [Given("a user {string} exists in the departments {string} with the roles {string}")]
    public async Task GivenAUserExists(string alias, string departmentCodes, string roles)
    {
        var departmentIds = Split(departmentCodes).Select(code => int.Parse(api.Expand("{dept:" + code + "}"), System.Globalization.CultureInfo.InvariantCulture));
        var body = JsonSerializer.Serialize(new { firstName = alias, lastName = "Tester", mobile = "9876543210", departmentIds, roles = Split(roles) });
        await api.SendAsSuperAdminAsync("POST", "/api/v1/users", body);
        Assert.Equal(201, (int)api.Status);
        api.Aliases["user:" + alias] = api.LastId!;
    }

    private string[] Split(string list) =>
        list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(api.Expand).ToArray();
}
