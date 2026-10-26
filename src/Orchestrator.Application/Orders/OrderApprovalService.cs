using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.Orders;

public sealed class OrderApprovalService(IAppDbContext db, CurrentUser currentUser)
{
    public async Task<IReadOnlyList<ApprovalResponse>> ListAsync(int orderId, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Employee, cancellationToken);
        if (!await db.Orders.AnyAsync(o => o.Id == orderId && (!actor.IsClient || o.Client!.UserId == actor.Id), cancellationToken))
        {
            throw new NotFoundException($"Order '{orderId}' was not found.");
        }

        var approvals = await db.OrderApprovals.AsNoTracking().Include(a => a.Department).Where(a => a.OrderId == orderId).OrderBy(a => a.Sequence).ToListAsync(cancellationToken);
        return [.. approvals.Select(a => ApprovalResponse.From(a, actor.IsClient))];
    }

    public async Task<OrderResponse> ApproveAsync(int orderId, CancellationToken cancellationToken)
    {
        var (actor, order, pending) = await LoadAsync(orderId, cancellationToken);
        pending.Decision = ApprovalDecision.Approved;
        pending.DecidedByUserId = actor.Id;
        pending.DecidedAt = DateTime.UtcNow;
        var last = order.Approvals.All(a => a.Decision == ApprovalDecision.Approved);
        if (last)
        {
            OrderFlow.Start(order, actor.Id, pending.DecidedAt.Value);
        }

        db.OrderEvents.Add(new OrderEventHistory
        {
            OrderId = orderId,
            Type = OrderEventType.ApprovalApproved,
            ActorUserId = actor.Id,
            FromValue = nameof(OrderStatus.PendingApproval),
            ToValue = order.Status.ToString(),
            Reason = $"Approval {pending.Sequence} of {order.Approvals.Count}: {pending.Department!.Name}",
        });
        await db.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }

    public async Task<OrderResponse> DenyAsync(int orderId, StepReasonRequest request, CancellationToken cancellationToken)
    {
        var (actor, order, pending) = await LoadAsync(orderId, cancellationToken);
        pending.Decision = ApprovalDecision.Denied;
        pending.DecidedByUserId = actor.Id;
        pending.DecidedAt = DateTime.UtcNow;
        pending.Reason = request.Reason.Trim();
        order.Status = OrderStatus.Cancelled;
        order.ReturnedToDepartmentId = SystemIds.EstimationDepartment;
        db.OrderEvents.Add(new OrderEventHistory
        {
            OrderId = orderId,
            Type = OrderEventType.ApprovalDenied,
            ActorUserId = actor.Id,
            FromValue = nameof(OrderStatus.PendingApproval),
            ToValue = nameof(OrderStatus.Cancelled),
            Reason = $"{pending.Department!.Name}: {pending.Reason}",
        });
        await db.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }

    private async Task<(Actor Actor, Order Order, OrderApproval Pending)> LoadAsync(int orderId, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Manager, cancellationToken);
        var order = await db.Detailed().Include(o => o.Approvals).ThenInclude(a => a.Department).SingleOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundException($"Order '{orderId}' was not found.");
        if (order.Status != OrderStatus.PendingApproval)
        {
            throw new ConflictException($"Order '{orderId}' is {order.Status}, not waiting for approval.");
        }

        var pending = order.Approvals.OrderBy(a => a.Sequence).FirstOrDefault(a => a.Decision == ApprovalDecision.Pending)
            ?? throw new ConflictException($"Order '{orderId}' has no pending approval.");
        if (actor.Rank < Rank.Admin && !actor.DepartmentIds.Contains(pending.DepartmentId))
        {
            throw new ForbiddenException($"The next approval belongs to the {pending.Department!.Name} department.");
        }

        return (actor, order, pending);
    }
}
