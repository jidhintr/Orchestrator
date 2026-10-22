using System.Text.Json.Serialization;

namespace Orchestrator.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    PendingApproval,
    InProgress,
    OnHold,
    Completed,
    Cancelled,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderPriority
{
    Normal,
    High,
    Premium,
    UltraHigh,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderEventType
{
    Placed,
    PriorityChangeRequested,
    QuoteAdjusted,
    Cancelled,
}

public sealed class Order : BaseEntity
{
    public int ClientId { get; set; }

    public Client? Client { get; set; }

    public int CatalogId { get; set; }

    public Catalog? Catalog { get; set; }

    public int Quantity { get; set; }

    public DateOnly? DueDate { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal CatalogDiscountPercent { get; set; }

    public decimal ClientDiscountPercent { get; set; }

    public decimal QuotedTotal { get; set; }

    public OrderPriority Priority { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingApproval;

    public int? ReturnedToDepartmentId { get; set; }

    public List<OrderStage> Stages { get; } = [];

    public List<OrderEventHistory> Events { get; } = [];
}

public sealed class OrderStage : BaseEntity
{
    public int OrderId { get; set; }

    public int JobTypeId { get; set; }

    public JobType? JobType { get; set; }

    public int Sequence { get; set; }

    public DateTime? EnteredAt { get; set; }

    public DateTime? ExitedAt { get; set; }
}

public sealed class OrderEventHistory : BaseEntity
{
    public int OrderId { get; set; }

    public OrderEventType Type { get; set; }

    public int ActorUserId { get; set; }

    public string? FromValue { get; set; }

    public string? ToValue { get; set; }

    public string? Reason { get; set; }
}
