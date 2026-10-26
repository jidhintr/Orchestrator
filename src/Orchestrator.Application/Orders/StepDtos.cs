using System.ComponentModel.DataAnnotations;
using Orchestrator.Domain;

namespace Orchestrator.Application.Orders;

public sealed record AssignStepRequest
{
    [Range(1, int.MaxValue)]
    public required int UserId { get; init; }
}

public sealed record StepProgressRequest
{
    [Range(0, 99)]
    public required int Progress { get; init; }

    [StringLength(500)]
    public string? Note { get; init; }
}

public sealed record StepReasonRequest
{
    [Required, StringLength(500, MinimumLength = 3)]
    public required string Reason { get; init; }
}

public sealed record StepResponse(
    int Id, int OrderId, string OrderNo, int OrderItemId, string ClientCompanyName, string CatalogName, string PriorityName, int PriorityRank,
    int JobTypeId, string JobTypeName, int SortOrder, int DepartmentId, string DepartmentName, StepStatus Status, int Progress, bool CanReject,
    int? AssignedUserId, int? CompletedByUserId, DateTime? ReadyAt, DateTime? StartedAt, DateTime? CompletedAt, int? StandardMinutes,
    double? WaitMinutes, double? WorkMinutes)
{
    public static StepResponse From(OrderItemJobStep s, DateTime now)
    {
        var order = s.OrderItem!.Order!;
        return new(
            s.Id, order.Id, order.OrderNo, s.OrderItemId, order.Client!.CompanyName, s.OrderItem.CatalogName, order.Priority!.Name, order.Priority.Rank,
            s.JobTypeId, s.JobTypeName, s.SortOrder, s.DepartmentId, s.Department!.Name, s.Status, s.Progress, s.CanReject,
            s.AssignedUserId, s.CompletedByUserId, s.ReadyAt, s.StartedAt, s.CompletedAt, s.StandardMinutes,
            Minutes(s.ReadyAt, s.StartedAt ?? now), Minutes(s.StartedAt, s.CompletedAt ?? now));
    }

    private static double? Minutes(DateTime? from, DateTime to) => from is { } f ? Math.Round(Math.Max(0, (to - f).TotalMinutes), 1) : null;
}

public sealed record StepLogResponse(int Id, int AuthorUserId, StepStatus FromStatus, StepStatus ToStatus, StepEntryType EntryType, string? Note, DateTime At)
{
    public static StepLogResponse From(OrderItemJobStepLog l) => new(l.Id, l.AuthorUserId, l.FromStatus, l.ToStatus, l.EntryType, l.Note, l.InsertedAt);
}

public sealed record ApprovalResponse(int Sequence, int DepartmentId, string DepartmentName, ApprovalDecision Decision, int? DecidedByUserId, DateTime? DecidedAt, string? Reason)
{
    public static ApprovalResponse From(OrderApproval a, bool hideReason) =>
        new(a.Sequence, a.DepartmentId, a.Department!.Name, a.Decision, a.DecidedByUserId, a.DecidedAt, hideReason ? null : a.Reason);
}
