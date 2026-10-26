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
public enum StepStatus
{
    Waiting,
    Ready,
    InProgress,
    OnHold,
    Completed,
    Rejected,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderEventType
{
    Placed,
    PriorityChangeRequested,
    QuoteAdjusted,
    ApprovalApproved,
    ApprovalDenied,
    Completed,
    Cancelled,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StepEntryType
{
    Ready,
    Assigned,
    Started,
    Progress,
    Held,
    Resumed,
    Completed,
    Rejected,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApprovalDecision
{
    Pending,
    Approved,
    Denied,
}

public sealed class Priority : BaseEntity
{
    public required string Name { get; set; }

    public int Rank { get; set; }

    public decimal Cost { get; set; }
}

public sealed class Order : BaseEntity
{
    public int ClientId { get; set; }

    public Client? Client { get; set; }

    public int PriorityId { get; set; }

    public Priority? Priority { get; set; }

    public decimal PriorityCost { get; set; }

    public DateOnly? CommitmentDeadline { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountTotal { get; set; }

    public decimal NetAmount { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingApproval;

    public int? ReturnedToDepartmentId { get; set; }

    public List<OrderItem> Items { get; } = [];

    public List<OrderEventHistory> Events { get; } = [];

    public List<OrderApproval> Approvals { get; } = [];
}

public sealed class OrderItem : BaseEntity
{
    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public int CatalogId { get; set; }

    public Catalog? Catalog { get; set; }

    public int Quantity { get; set; }

    public required string CatalogSku { get; set; }

    public required string CatalogName { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal CatalogDiscountPercent { get; set; }

    public decimal ClientDiscountPercent { get; set; }

    public decimal LineNet { get; set; }

    public List<OrderItemJobStep> Steps { get; } = [];
}

public sealed class OrderItemJobStep : BaseEntity
{
    public int OrderItemId { get; set; }

    public OrderItem? OrderItem { get; set; }

    public int JobTypeId { get; set; }

    public JobType? JobType { get; set; }

    public required string JobTypeName { get; set; }

    public int SortOrder { get; set; }

    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    public StepStatus Status { get; set; } = StepStatus.Waiting;

    public int Progress { get; set; }

    public bool CanReject { get; set; }

    public int? AssignedUserId { get; set; }

    public int? CompletedByUserId { get; set; }

    public DateTime? ReadyAt { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public int? StandardMinutes { get; set; }

    public decimal? LaborCost { get; set; }

    public decimal? CommissionPercent { get; set; }

    public decimal? ProfitPercent { get; set; }

    public decimal EarnedAmount { get; set; }

    public List<OrderItemJobStepLog> Logs { get; } = [];
}

public sealed class OrderItemJobStepLog : BaseEntity
{
    public int OrderItemJobStepId { get; set; }

    public int AuthorUserId { get; set; }

    public StepStatus FromStatus { get; set; }

    public StepStatus ToStatus { get; set; }

    public StepEntryType EntryType { get; set; }

    public string? Note { get; set; }
}

public sealed class OrderApproval : BaseEntity
{
    public int OrderId { get; set; }

    public int Sequence { get; set; }

    public int DepartmentId { get; set; }

    public Department? Department { get; set; }

    public ApprovalDecision Decision { get; set; } = ApprovalDecision.Pending;

    public int? DecidedByUserId { get; set; }

    public DateTime? DecidedAt { get; set; }

    public string? Reason { get; set; }
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
