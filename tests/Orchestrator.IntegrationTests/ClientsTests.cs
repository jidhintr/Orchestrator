namespace Orchestrator.IntegrationTests;

public class ClientsTests(ApiFixture api)
{
    [Theory]
    [InlineData("Normal", 0)]
    [InlineData("Gold", 5)]
    [InlineData("Platinum", 10)]
    public async Task Create_returns_201_with_tier_discount_and_active_status(string tier, int discount)
    {
        var reply = await api.Post("clients", ApiFixture.ClientBody(tier: tier));
        Assert.Equal(HttpStatusCode.Created, reply.Status);
        Assert.EndsWith($"/api/v1/clients/{reply.Id}", reply.Header("Location"), StringComparison.Ordinal);
        Assert.Equal(tier, reply.Str("tier"));
        Assert.Equal(discount, reply["tierDiscountPercent"].GetDecimal());
        Assert.Equal("Active", reply.Str("status"));
        Assert.True(reply["isActive"].GetBoolean());
    }

    [Fact]
    public async Task List_and_get_return_created_clients()
    {
        var id = await api.NewClient();
        Assert.Contains(id, (await api.Get("clients")).Ids);
        Assert.Equal(id, (await api.Get($"clients/{id}")).Id);
    }

    [Fact]
    public async Task Get_unknown_is_404() =>
        ProblemAssert.Is(await api.Get("clients/" + ApiFixture.UnknownId), HttpStatusCode.NotFound);

    [Fact]
    public async Task Create_duplicate_email_ignoring_case_is_409()
    {
        var email = $"dup{ApiFixture.Unique()}@acme.test";
        await api.Post("clients", ApiFixture.ClientBody(email));
        ProblemAssert.Is(await api.Post("clients", ApiFixture.ClientBody(email.ToUpperInvariant())), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Create_with_invalid_fields_is_400()
    {
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("clients", new { companyName = "A", contactName = "Jane", email = "a@b.test", phone = "+1234567", shippingAddress = "x" })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("clients", ApiFixture.ClientBody("not-an-email"))).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("clients", new { companyName = "Acme", contactName = "Jane", email = "a@b.test", phone = "abc", shippingAddress = "x" })).Status);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("clients", new { companyName = "Acme", contactName = "Jane", email = "a@b.test", phone = "+1234567", shippingAddress = "" })).Status);
    }

    [Fact]
    public async Task Create_with_an_unknown_tier_is_400() =>
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Post("clients", ApiFixture.ClientBody(tier: "Diamond"))).Status);

    [Fact]
    public async Task Update_changes_fields_and_recomputes_the_discount()
    {
        var id = await api.NewClient();
        var email = $"new{ApiFixture.Unique()}@acme.test";
        var reply = await api.Put($"clients/{id}", ApiFixture.ClientBody(email, "Platinum"));
        Assert.Equal(HttpStatusCode.OK, reply.Status);
        Assert.Equal(email, reply.Str("email"));
        Assert.Equal(10, reply["tierDiscountPercent"].GetDecimal());
    }

    [Fact]
    public async Task Update_to_a_used_email_is_409()
    {
        var email = $"taken{ApiFixture.Unique()}@acme.test";
        await api.Post("clients", ApiFixture.ClientBody(email));
        ProblemAssert.Is(await api.Put($"clients/{await api.NewClient()}", ApiFixture.ClientBody(email)), HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_unknown_is_404_and_bad_body_is_400()
    {
        ProblemAssert.Is(await api.Put($"clients/{ApiFixture.UnknownId}", ApiFixture.ClientBody()), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"clients/{await api.NewClient()}", ApiFixture.ClientBody("nope"))).Status);
    }

    [Fact]
    public async Task Status_inactive_deactivates_and_other_statuses_reactivate()
    {
        var id = await api.NewClient();

        var inactive = await api.Put($"clients/{id}/status", new { status = "Inactive" });
        Assert.Equal(HttpStatusCode.OK, inactive.Status);
        Assert.Equal("Inactive", inactive.Str("status"));
        Assert.False(inactive["isActive"].GetBoolean());

        foreach (var status in new[] { "PaymentPending", "Suspended", "Active" })
        {
            var reply = await api.Put($"clients/{id}/status", new { status });
            Assert.Equal(status, reply.Str("status"));
            Assert.True(reply["isActive"].GetBoolean());
        }
    }

    [Fact]
    public async Task Status_rejects_unknown_client_and_unknown_status()
    {
        ProblemAssert.Is(await api.Put($"clients/{ApiFixture.UnknownId}/status", new { status = "Active" }), HttpStatusCode.NotFound);
        Assert.Equal(HttpStatusCode.BadRequest, (await api.Put($"clients/{await api.NewClient()}/status", new { status = "Gone" })).Status);
    }
}
