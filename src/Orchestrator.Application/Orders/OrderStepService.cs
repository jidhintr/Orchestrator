using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.Orders;

public sealed class OrderStepService(IAppDbContext db, CurrentUser currentUser)
{
    public async Task<IReadOnlyList<StepResponse>> ListAsync(StepStatus? status, bool assignedToMe, int? departmentId, CancellationToken cancellationToken)
    {
        var actor = await RequireStaffAsync(cancellationToken);
        var query = Detailed().AsNoTracking().Where(s => s.Status != StepStatus.Waiting);
        if (actor.Rank < Rank.Admin)
        {
            var departments = actor.DepartmentIds.ToList();
            query = query.Where(s => departments.Contains(s.DepartmentId));
        }

        if (status is not null)
        {
            query = query.Where(s => s.Status == status);
        }

        if (assignedToMe)
        {
            query = query.Where(s => s.AssignedUserId == actor.Id);
        }

        if (departmentId is not null)
        {
            query = query.Where(s => s.DepartmentId == departmentId);
        }

        var now = DateTime.UtcNow;
        var steps = await query.OrderByDescending(s => s.OrderItem!.Order!.Priority!.Rank).ThenBy(s => s.ReadyAt).ThenBy(s => s.Id).Take(500).ToListAsync(cancellationToken);
        return [.. steps.Select(s => StepResponse.From(s, now))];
    }

    public async Task<StepResponse> GetAsync(int id, CancellationToken cancellationToken)
    {
        var (_, step) = await FindAsync(id, requireInProgress: false, cancellationToken);
        return StepResponse.From(step, DateTime.UtcNow);
    }

    public async Task<IReadOnlyList<StepLogResponse>> LogsAsync(int id, CancellationToken cancellationToken)
    {
        await FindAsync(id, requireInProgress: false, cancellationToken);
        return [.. (await db.OrderItemJobStepLogs.AsNoTracking().Where(l => l.OrderItemJobStepId == id).OrderBy(l => l.Id).ToListAsync(cancellationToken)).Select(StepLogResponse.From)];
    }

    public async Task<StepResponse> AssignAsync(int id, AssignStepRequest request, CancellationToken cancellationToken)
    {
        var (actor, step) = await FindAsync(id, requireInProgress: true, cancellationToken);
        Require(step, "assigned", StepStatus.Ready, StepStatus.InProgress, StepStatus.OnHold);
        if (!Manages(actor, step) && !(request.UserId == actor.Id && actor.DepartmentIds.Contains(step.DepartmentId)))
        {
            throw new ForbiddenException("Employees can only assign themselves; managers assign anyone in their department.");
        }

        if (!await db.Users.AnyAsync(u => u.Id == request.UserId && u.IsActive && u.Departments.Any(d => d.Id == step.DepartmentId), cancellationToken))
        {
            throw new BadRequestException("The user must be an active member of the step's department.");
        }

        step.AssignedUserId = request.UserId;
        step.Logs.Add(OrderFlow.Log(step, actor.Id, StepEntryType.Assigned, step.Status, $"Assigned to user {request.UserId}"));
        return await SaveAsync(step, cancellationToken);
    }

    public async Task<StepResponse> StartAsync(int id, CancellationToken cancellationToken)
    {
        var (actor, step) = await FindAsync(id, requireInProgress: true, cancellationToken);
        Require(step, "started", StepStatus.Ready);
        RequireWorker(actor, step);
        step.StartedAt = DateTime.UtcNow;
        step.AssignedUserId ??= actor.Id;
        Move(step, actor, StepEntryType.Started, StepStatus.InProgress, null);
        return await SaveAsync(step, cancellationToken);
    }

    public async Task<StepResponse> ProgressAsync(int id, StepProgressRequest request, CancellationToken cancellationToken)
    {
        var (actor, step) = await FindAsync(id, requireInProgress: true, cancellationToken);
        Require(step, "updated", StepStatus.InProgress);
        RequireWorker(actor, step);
        step.Progress = request.Progress;
        step.Logs.Add(OrderFlow.Log(step, actor.Id, StepEntryType.Progress, StepStatus.InProgress, $"{request.Progress}%{(string.IsNullOrWhiteSpace(request.Note) ? "" : ": " + request.Note.Trim())}"));
        return await SaveAsync(step, cancellationToken);
    }

    public async Task<StepResponse> HoldAsync(int id, StepReasonRequest request, CancellationToken cancellationToken)
    {
        var (actor, step) = await FindAsync(id, requireInProgress: true, cancellationToken);
        Require(step, "put on hold", StepStatus.InProgress);
        RequireWorker(actor, step);
        Move(step, actor, StepEntryType.Held, StepStatus.OnHold, request.Reason.Trim());
        return await SaveAsync(step, cancellationToken);
    }

    public async Task<StepResponse> ResumeAsync(int id, CancellationToken cancellationToken)
    {
        var (actor, step) = await FindAsync(id, requireInProgress: true, cancellationToken);
        Require(step, "resumed", StepStatus.OnHold);
        RequireWorker(actor, step);
        Move(step, actor, StepEntryType.Resumed, StepStatus.InProgress, null);
        return await SaveAsync(step, cancellationToken);
    }

    public async Task<StepResponse> CompleteAsync(int id, CancellationToken cancellationToken)
    {
        var (actor, step) = await FindAsync(id, requireInProgress: true, cancellationToken);
        Require(step, "completed", StepStatus.InProgress);
        RequireWorker(actor, step);
        var now = DateTime.UtcNow;
        step.Progress = 100;
        step.CompletedAt = now;
        step.CompletedByUserId = actor.Id;
        Move(step, actor, StepEntryType.Completed, StepStatus.Completed, null);

        var next = step.OrderItem!.Steps.Where(s => s.SortOrder > step.SortOrder).OrderBy(s => s.SortOrder).FirstOrDefault();
        var order = step.OrderItem.Order!;
        if (next is not null)
        {
            OrderFlow.MarkReady(next, actor.Id, now, $"Step '{step.JobTypeName}' completed");
        }
        else if (!await db.OrderItemJobSteps.AnyAsync(s => s.OrderItem!.OrderId == order.Id && s.Id != step.Id && s.Status != StepStatus.Completed, cancellationToken))
        {
            order.Status = OrderStatus.Completed;
            db.OrderEvents.Add(new OrderEventHistory { OrderId = order.Id, Type = OrderEventType.Completed, ActorUserId = actor.Id, FromValue = nameof(OrderStatus.InProgress), ToValue = nameof(OrderStatus.Completed) });
        }

        return await SaveAsync(step, cancellationToken);
    }

    public async Task<StepResponse> RejectAsync(int id, StepReasonRequest request, CancellationToken cancellationToken)
    {
        var (actor, step) = await FindAsync(id, requireInProgress: true, cancellationToken);
        Require(step, "rejected", StepStatus.InProgress);
        RequireWorker(actor, step);
        var previous = step.CanReject ? step.OrderItem!.Steps.Where(s => s.SortOrder < step.SortOrder).OrderByDescending(s => s.SortOrder).FirstOrDefault() : null;
        if (previous is null)
        {
            throw new ConflictException("This step cannot reject work back to the previous step.");
        }

        var reason = request.Reason.Trim();
        previous.Logs.Add(OrderFlow.Log(previous, actor.Id, StepEntryType.Ready, StepStatus.Ready, $"Reopened: '{step.JobTypeName}' rejected the work: {reason}"));
        previous.Status = StepStatus.Ready;
        previous.ReadyAt = DateTime.UtcNow;
        previous.StartedAt = null;
        previous.CompletedAt = null;
        previous.CompletedByUserId = null;
        previous.Progress = 0;

        Move(step, actor, StepEntryType.Rejected, StepStatus.Waiting, reason);
        step.ReadyAt = null;
        step.StartedAt = null;
        step.AssignedUserId = null;
        step.Progress = 0;
        return await SaveAsync(step, cancellationToken);
    }

    private static bool Manages(Actor actor, OrderItemJobStep step) =>
        actor.Rank >= Rank.Admin || (actor.Rank >= Rank.Manager && actor.DepartmentIds.Contains(step.DepartmentId));

    private static void RequireWorker(Actor actor, OrderItemJobStep step)
    {
        if (!Manages(actor, step) && step.AssignedUserId != actor.Id)
        {
            throw new ForbiddenException("Only the assignee or a manager of this department can do that.");
        }
    }

    private static void Require(OrderItemJobStep step, string action, params StepStatus[] allowed)
    {
        if (!allowed.Contains(step.Status))
        {
            throw new ConflictException($"A step that is {step.Status} cannot be {action}.");
        }
    }

    private static void Move(OrderItemJobStep step, Actor actor, StepEntryType type, StepStatus to, string? note)
    {
        step.Logs.Add(OrderFlow.Log(step, actor.Id, type, to, note));
        step.Status = to;
    }

    private IQueryable<OrderItemJobStep> Detailed() => db.OrderItemJobSteps
        .Include(s => s.Department)
        .Include(s => s.OrderItem).ThenInclude(i => i!.Order).ThenInclude(o => o!.Client)
        .Include(s => s.OrderItem).ThenInclude(i => i!.Order).ThenInclude(o => o!.Priority);

    private IQueryable<OrderItemJobStep> Query() => Detailed().Include(s => s.OrderItem).ThenInclude(i => i!.Steps);

    private async Task<Actor> RequireStaffAsync(CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Employee, cancellationToken);
        return actor.IsClient ? throw new ForbiddenException("Clients cannot use the assembly line.") : actor;
    }

    private async Task<(Actor Actor, OrderItemJobStep Step)> FindAsync(int id, bool requireInProgress, CancellationToken cancellationToken)
    {
        var actor = await RequireStaffAsync(cancellationToken);
        var step = await Query().SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (step is null || (actor.Rank < Rank.Admin && !actor.DepartmentIds.Contains(step.DepartmentId)))
        {
            throw new NotFoundException($"Step '{id}' was not found.");
        }

        var status = step.OrderItem!.Order!.Status;
        if (requireInProgress && status != OrderStatus.InProgress)
        {
            throw new ConflictException($"Order '{step.OrderItem.OrderId}' is {status}; its steps cannot change.");
        }

        return (actor, step);
    }

    private async Task<StepResponse> SaveAsync(OrderItemJobStep step, CancellationToken cancellationToken)
    {
        await db.SaveChangesAsync(cancellationToken);
        return StepResponse.From(step, DateTime.UtcNow);
    }
}
