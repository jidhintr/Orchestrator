using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.Clients;

public sealed record ClientRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public required string CompanyName { get; init; }

    [Required, StringLength(100, MinimumLength = 2)]
    public required string ContactName { get; init; }

    [Required, EmailAddress, StringLength(100, MinimumLength = 5)]
    public required string Email { get; init; }

    [Required, Phone, StringLength(30, MinimumLength = 7)]
    public required string Phone { get; init; }

    [Required, StringLength(500, MinimumLength = 1)]
    public required string ShippingAddress { get; init; }

    [EnumDataType(typeof(ClientTier))]
    public ClientTier Tier { get; init; } = ClientTier.Normal;

    [Range(1, int.MaxValue)]
    public int? UserId { get; init; }
}

public sealed record ClientStatusRequest
{
    [EnumDataType(typeof(ClientStatus))]
    public required ClientStatus Status { get; init; }
}

public sealed record ClientResponse(
    int Id, string CompanyName, string ContactName, string Email, string Phone, string ShippingAddress,
    ClientTier Tier, ClientStatus Status, decimal TierDiscountPercent, bool IsActive, int? UserId)
{
    public static ClientResponse From(Client c) => new(
        c.Id, c.CompanyName, c.ContactName, c.Email, c.Phone, c.ShippingAddress, c.Tier, c.Status, c.TierDiscountPercent, c.IsActive, c.UserId);
}
