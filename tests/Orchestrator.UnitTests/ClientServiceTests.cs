using Orchestrator.Application.Clients;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;
using Orchestrator.Infrastructure.Persistence;

namespace Orchestrator.UnitTests;

public class ClientServiceTests
{
    private readonly OrchestratorDbContext _db = TestDb.Create();
    private readonly ClientService _clients;

    public ClientServiceTests() => _clients = new ClientService(_db);

    private static ClientRequest Request(string company = "Acme", ClientTier tier = ClientTier.Normal) => new()
    {
        CompanyName = company,
        ContactName = "Jane Doe",
        Email = $"{company}@acme.test".ToLowerInvariant(),
        Phone = "+1234567890",
        ShippingAddress = "1 Main St",
        Tier = tier,
    };

    [Theory]
    [InlineData(ClientTier.Normal, 0)]
    [InlineData(ClientTier.Gold, 5)]
    [InlineData(ClientTier.Platinum, 10)]
    public async Task Create_sets_tier_discount_and_active_status(ClientTier tier, int discount)
    {
        var created = await _clients.CreateAsync(Request(tier: tier), default);
        Assert.Equal(tier, created.Tier);
        Assert.Equal(discount, created.TierDiscountPercent);
        Assert.Equal(ClientStatus.Active, created.Status);
        Assert.True(created.IsActive);
    }

    [Fact]
    public async Task Create_trims_text_fields()
    {
        var created = await _clients.CreateAsync(Request() with { CompanyName = "  Acme  ", ShippingAddress = "  1 Main St  " }, default);
        Assert.Equal("Acme", created.CompanyName);
        Assert.Equal("1 Main St", created.ShippingAddress);
    }

    [Fact]
    public async Task List_is_ordered_by_company_and_get_returns_one()
    {
        await _clients.CreateAsync(Request("Zed"), default);
        var acme = await _clients.CreateAsync(Request("Acme"), default);
        Assert.Equal(["Acme", "Zed"], (await _clients.ListAsync(default)).Select(c => c.CompanyName));
        Assert.Equal("Acme", (await _clients.GetAsync(acme.Id, default)).CompanyName);
    }

    [Fact]
    public async Task Get_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _clients.GetAsync(9999, default));

    [Fact]
    public async Task Update_changes_fields_and_recomputes_discount()
    {
        var created = await _clients.CreateAsync(Request(tier: ClientTier.Platinum), default);
        var updated = await _clients.UpdateAsync(created.Id, Request("Beta", ClientTier.Gold), default);
        Assert.Equal("Beta", updated.CompanyName);
        Assert.Equal(5, updated.TierDiscountPercent);
    }

    [Fact]
    public async Task Update_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _clients.UpdateAsync(9999, Request(), default));

    [Fact]
    public async Task SetStatus_inactive_deactivates_and_other_statuses_reactivate()
    {
        var created = await _clients.CreateAsync(Request(), default);

        var inactive = await _clients.SetStatusAsync(created.Id, new ClientStatusRequest { Status = ClientStatus.Inactive }, default);
        Assert.Equal(ClientStatus.Inactive, inactive.Status);
        Assert.False(inactive.IsActive);

        var pending = await _clients.SetStatusAsync(created.Id, new ClientStatusRequest { Status = ClientStatus.PaymentPending }, default);
        Assert.Equal(ClientStatus.PaymentPending, pending.Status);
        Assert.True(pending.IsActive);

        var suspended = await _clients.SetStatusAsync(created.Id, new ClientStatusRequest { Status = ClientStatus.Suspended }, default);
        Assert.True(suspended.IsActive);
    }

    [Fact]
    public async Task SetStatus_unknown_is_not_found() =>
        await Assert.ThrowsAsync<NotFoundException>(() => _clients.SetStatusAsync(9999, new ClientStatusRequest { Status = ClientStatus.Active }, default));
}
