using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.Orders;

public sealed record PlaceOrderRequest
{
    public int? ClientId { get; init; }

    [Range(1, int.MaxValue)]
    public required int CatalogId { get; init; }

    [Range(1, 1_000_000)]
    public required int Quantity { get; init; }

    public DateOnly? DueDate { get; init; }

    [StringLength(100)]
    public string? Reference { get; init; }

    [StringLength(1000)]
    public string? Notes { get; init; }

    [EnumDataType(typeof(OrderPriority))]
    public OrderPriority Priority { get; init; } = OrderPriority.Normal;
}

public sealed record CancelOrderRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public required string Reason { get; init; }
}

public sealed record PriorityChangeRequest
{
    [EnumDataType(typeof(OrderPriority))]
    public required OrderPriority Priority { get; init; }

    [StringLength(500)]
    public string? Reason { get; init; }
}

public sealed record AdjustQuoteRequest
{
    [Range(typeof(decimal), "0", "1000000000")]
    public required decimal Total { get; init; }

    [Required, StringLength(500, MinimumLength = 3)]
    public required string Reason { get; init; }
}

public sealed record OrderStageResponse(int JobTypeId, string JobTypeName, int Sequence, DateTime? EnteredAt, DateTime? ExitedAt);

public sealed record OrderResponse(
    int Id, int ClientId, string ClientCompanyName, int CatalogId, string CatalogSku, string CatalogName,
    int Quantity, DateOnly? DueDate, string? Reference, string? Notes,
    decimal UnitPrice, decimal CatalogDiscountPercent, decimal ClientDiscountPercent, decimal QuotedTotal,
    OrderPriority Priority, OrderStatus Status, int? ReturnedToDepartmentId, DateTime PlacedAt, IReadOnlyList<OrderStageResponse> Stages)
{
    public static OrderResponse From(Order o) => new(
        o.Id, o.ClientId, o.Client!.CompanyName, o.CatalogId, o.Catalog!.Sku, o.Catalog.Name,
        o.Quantity, o.DueDate, o.Reference, o.Notes, o.UnitPrice, o.CatalogDiscountPercent, o.ClientDiscountPercent, o.QuotedTotal,
        o.Priority, o.Status, o.ReturnedToDepartmentId, o.InsertedAt,
        [.. o.Stages.OrderBy(s => s.Sequence).Select(s => new OrderStageResponse(s.JobTypeId, s.JobType!.Name, s.Sequence, s.EnteredAt, s.ExitedAt))]);
}

public sealed record OrderEventResponse(int Id, OrderEventType Type, int ActorUserId, string? FromValue, string? ToValue, string? Reason, DateTime At)
{
    public static OrderEventResponse From(OrderEventHistory e, bool hideReason) =>
        new(e.Id, e.Type, e.ActorUserId, e.FromValue, e.ToValue, hideReason ? null : e.Reason, e.InsertedAt);
}
