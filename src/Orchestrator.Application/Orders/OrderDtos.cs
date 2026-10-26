using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.Orders;

public sealed record OrderItemRequest
{
    [Range(1, int.MaxValue)]
    public required int CatalogId { get; init; }

    [Range(1, 1_000_000)]
    public required int Quantity { get; init; }

    [StringLength(500)]
    public string? Description { get; init; }

    [MaxLength(20)]
    public IReadOnlyList<ItemSpecRequest>? Specs { get; init; }
}

public sealed record ItemSpecRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public required string Name { get; init; }

    [Required, StringLength(200, MinimumLength = 1)]
    public required string Value { get; init; }
}

public sealed record PlaceOrderRequest
{
    public int? ClientId { get; init; }

    [Required, MinLength(1), MaxLength(50)]
    public required IReadOnlyList<OrderItemRequest> Items { get; init; }

    [Range(1, int.MaxValue)]
    public int PriorityId { get; init; } = SystemIds.NormalPriority;

    public DateOnly? CommitmentDeadline { get; init; }

    [StringLength(100)]
    public string? Reference { get; init; }

    [StringLength(1000)]
    public string? Notes { get; init; }
}

public sealed record CancelOrderRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public required string Reason { get; init; }
}

public sealed record PriorityChangeRequest
{
    [Range(1, int.MaxValue)]
    public required int PriorityId { get; init; }

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

public sealed record PriorityResponse(int Id, string Name, int Rank, decimal Cost, bool IsActive)
{
    public static PriorityResponse From(Priority p) => new(p.Id, p.Name, p.Rank, p.Cost, p.IsActive);
}

public sealed record OrderStepResponse(int Id, int JobTypeId, string JobTypeName, int SortOrder, StepStatus Status, int Progress, DateTime? StartedAt, DateTime? CompletedAt);

public sealed record OrderItemResponse(
    int Id, int CatalogId, string CatalogSku, string CatalogName, int Quantity, string? Description, IReadOnlyList<ItemSpec> Specs, decimal UnitPrice,
    decimal CatalogDiscountPercent, decimal ClientDiscountPercent, decimal LineNet, IReadOnlyList<OrderStepResponse> Steps)
{
    public static OrderItemResponse From(OrderItem i) => new(
        i.Id, i.CatalogId, i.CatalogSku, i.CatalogName, i.Quantity, i.Description, i.Specs, i.UnitPrice, i.CatalogDiscountPercent, i.ClientDiscountPercent, i.LineNet,
        [.. i.Steps.OrderBy(s => s.SortOrder).Select(s => new OrderStepResponse(s.Id, s.JobTypeId, s.JobTypeName, s.SortOrder, s.Status, s.Progress, s.StartedAt, s.CompletedAt))]);
}

public sealed record OrderResponse(
    int Id, int ClientId, string ClientCompanyName, int PriorityId, string PriorityName, decimal PriorityCost,
    DateOnly? CommitmentDeadline, string? Reference, string? Notes, decimal SubTotal, decimal DiscountTotal, decimal NetAmount,
    OrderStatus Status, int? ReturnedToDepartmentId, DateTime PlacedAt, IReadOnlyList<OrderItemResponse> Items)
{
    public static OrderResponse From(Order o) => new(
        o.Id, o.ClientId, o.Client!.CompanyName, o.PriorityId, o.Priority!.Name, o.PriorityCost,
        o.CommitmentDeadline, o.Reference, o.Notes, o.SubTotal, o.DiscountTotal, o.NetAmount,
        o.Status, o.ReturnedToDepartmentId, o.InsertedAt, [.. o.Items.OrderBy(i => i.Id).Select(OrderItemResponse.From)]);
}

public sealed record OrderEventResponse(int Id, OrderEventType Type, int ActorUserId, string? FromValue, string? ToValue, string? Reason, DateTime At)
{
    public static OrderEventResponse From(OrderEventHistory e, bool hideReason) =>
        new(e.Id, e.Type, e.ActorUserId, e.FromValue, e.ToValue, hideReason ? null : e.Reason, e.InsertedAt);
}
