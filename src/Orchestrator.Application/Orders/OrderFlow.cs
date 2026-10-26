using Microsoft.EntityFrameworkCore;
using Orchestrator.Domain;

namespace Orchestrator.Application.Orders;

internal static class OrderFlow
{
    public static IQueryable<Order> Detailed(this IAppDbContext db) =>
        db.Orders.Include(o => o.Client).Include(o => o.Priority).Include(o => o.Items).ThenInclude(i => i.Steps);

    public static OrderItemJobStepLog Log(OrderItemJobStep step, int actorId, StepEntryType type, StepStatus to, string? note) =>
        new() { AuthorUserId = actorId, FromStatus = step.Status, ToStatus = to, EntryType = type, Note = note };

    public static void MarkReady(OrderItemJobStep step, int actorId, DateTime now, string? note = null)
    {
        step.Logs.Add(Log(step, actorId, StepEntryType.Ready, StepStatus.Ready, note));
        step.Status = StepStatus.Ready;
        step.ReadyAt = now;
    }

    public static void Start(Order order, int actorId, DateTime now)
    {
        order.Status = OrderStatus.InProgress;
        foreach (var item in order.Items)
        {
            if (item.Steps.OrderBy(s => s.SortOrder).FirstOrDefault() is { } first)
            {
                MarkReady(first, actorId, now, "Order approved");
            }
        }
    }
}
