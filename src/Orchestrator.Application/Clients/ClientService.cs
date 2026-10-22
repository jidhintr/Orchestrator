using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.Clients;

public sealed class ClientService(IAppDbContext db)
{
    public async Task<IReadOnlyList<ClientResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await db.Clients.AsNoTracking().OrderBy(c => c.CompanyName).ToListAsync(cancellationToken)).Select(ClientResponse.From)];

    public async Task<ClientResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        ClientResponse.From(await FindAsync(id, cancellationToken));

    // Duplicate emails are rejected by the unique index and mapped to 409 by GlobalExceptionHandler.
    public async Task<ClientResponse> CreateAsync(ClientRequest request, CancellationToken cancellationToken)
    {
        var client = new Client { CompanyName = "", ContactName = "", Email = "", Phone = "", ShippingAddress = "" };
        Apply(client, request);
        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken);
        return ClientResponse.From(client);
    }

    public async Task<ClientResponse> UpdateAsync(int id, ClientRequest request, CancellationToken cancellationToken)
    {
        var client = await FindAsync(id, cancellationToken);
        Apply(client, request);
        await db.SaveChangesAsync(cancellationToken);
        return ClientResponse.From(client);
    }

    public async Task<ClientResponse> SetStatusAsync(int id, ClientStatusRequest request, CancellationToken cancellationToken)
    {
        var client = await FindAsync(id, cancellationToken);
        client.Status = request.Status;
        client.IsActive = request.Status != ClientStatus.Inactive;
        await db.SaveChangesAsync(cancellationToken);
        return ClientResponse.From(client);
    }

    // Placeholder discounts pending owner confirmation.
    private static void Apply(Client client, ClientRequest request)
    {
        client.CompanyName = request.CompanyName.Trim();
        client.ContactName = request.ContactName.Trim();
        client.Email = request.Email.Trim();
        client.Phone = request.Phone.Trim();
        client.ShippingAddress = request.ShippingAddress.Trim();
        client.Tier = request.Tier;
        client.UserId = request.UserId;
        client.TierDiscountPercent = request.Tier switch
        {
            ClientTier.Gold => 5m,
            ClientTier.Platinum => 10m,
            _ => 0m,
        };
    }

    private async Task<Client> FindAsync(int id, CancellationToken cancellationToken) =>
        await db.Clients.FindAsync([id], cancellationToken) ?? throw new NotFoundException($"Client '{id}' was not found.");
}
