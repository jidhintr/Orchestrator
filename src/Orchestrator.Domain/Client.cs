using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClientTier
{
    Normal,
    Gold,
    Platinum,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClientStatus
{
    Active,
    PaymentPending,
    Suspended,
    Inactive,
}

public sealed class Client : BaseEntity
{
    public required string CompanyName { get; set; }

    public required string ContactName { get; set; }

    public required string Email { get; set; }

    public required string Phone { get; set; }

    public required string ShippingAddress { get; set; }

    public ClientTier Tier { get; set; } = ClientTier.Normal;

    public ClientStatus Status { get; set; } = ClientStatus.Active;

    public decimal TierDiscountPercent { get; set; }

    public int? UserId { get; set; }
}
