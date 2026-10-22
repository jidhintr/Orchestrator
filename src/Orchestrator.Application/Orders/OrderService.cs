using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Orchestrator.Application.Access;
using Orchestrator.Application.Errors;
using Orchestrator.Domain;

namespace Orchestrator.Application.Orders;

public sealed class OrderService(IAppDbContext db, CurrentUser currentUser)
{
    public async Task<IReadOnlyList<OrderResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Employee, cancellationToken);
        return [.. (await Scoped(actor).AsNoTracking().OrderByDescending(o => o.Id).ToListAsync(cancellationToken)).Select(OrderResponse.From)];
    }

    public async Task<OrderResponse> GetAsync(int id, CancellationToken cancellationToken) =>
        OrderResponse.From(await FindAsync(await currentUser.RequireAsync(Rank.Employee, cancellationToken), id, cancellationToken));

    public async Task<IReadOnlyList<OrderEventResponse>> EventsAsync(int id, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Employee, cancellationToken);
        await FindAsync(actor, id, cancellationToken);
        return [.. (await db.OrderEvents.AsNoTracking().Where(e => e.OrderId == id).OrderBy(e => e.Id).ToListAsync(cancellationToken))
            .Select(e => OrderEventResponse.From(e, actor.IsClient))];
    }

    public async Task<OrderResponse> PlaceAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Employee, cancellationToken);
        var client = actor.IsClient
            ? await db.Clients.SingleOrDefaultAsync(c => c.UserId == actor.Id, cancellationToken) ?? throw new ForbiddenException("Your account is not linked to a client.")
            : await db.Clients.FindAsync([request.ClientId ?? throw new BadRequestException("clientId is required.")], cancellationToken)
                ?? throw new BadRequestException($"Client '{request.ClientId}' does not exist.");
        if (client.Status == ClientStatus.Suspended || !client.IsActive)
        {
            throw new ConflictException($"Client '{client.Id}' cannot place orders ({client.Status}).");
        }

        var catalog = await db.Catalogs.Include(c => c.CatalogJobSteps).SingleOrDefaultAsync(c => c.Id == request.CatalogId && c.IsActive, cancellationToken)
            ?? throw new BadRequestException($"Catalog '{request.CatalogId}' does not exist or is deactivated.");

        if (request.DueDate < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new BadRequestException("dueDate cannot be in the past.");
        }

        var order = new Order
        {
            ClientId = client.Id,
            CatalogId = catalog.Id,
            Priority = request.Priority,
            Quantity = request.Quantity,
            DueDate = request.DueDate,
            Reference = request.Reference?.Trim(),
            Notes = request.Notes?.Trim(),
            UnitPrice = catalog.BasePrice,
            CatalogDiscountPercent = catalog.CatalogDiscountPercent,
            ClientDiscountPercent = client.TierDiscountPercent,
            QuotedTotal = Math.Round(catalog.BasePrice * request.Quantity * (1 - catalog.CatalogDiscountPercent / 100m) * (1 - client.TierDiscountPercent / 100m), 2, MidpointRounding.AwayFromZero),
        };
        order.Stages.AddRange(catalog.CatalogJobSteps.Select(s => new OrderStage { JobTypeId = s.JobTypeId, Sequence = s.Sequence }));
        order.Events.Add(new OrderEventHistory { Type = OrderEventType.Placed, ActorUserId = actor.Id, ToValue = order.Status.ToString() });
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(order.Id, cancellationToken);
    }

    public async Task<OrderResponse> CancelAsync(int id, CancelOrderRequest request, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Manager, cancellationToken);
        var order = await FindAsync(actor, id, cancellationToken);
        EnsureOpen(order, "cancelled");
        db.OrderEvents.Add(new OrderEventHistory
        {
            OrderId = id,
            Type = OrderEventType.Cancelled,
            ActorUserId = actor.Id,
            FromValue = order.Status.ToString(),
            ToValue = nameof(OrderStatus.Cancelled),
            Reason = request.Reason.Trim(),
        });
        order.Status = OrderStatus.Cancelled;
        order.ReturnedToDepartmentId = SystemIds.EstimationDepartment;
        await db.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }

    public async Task<OrderResponse> AdjustQuoteAsync(int id, AdjustQuoteRequest request, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Manager, cancellationToken);
        if (actor.Rank < Rank.Admin && !actor.DepartmentIds.Contains(SystemIds.EstimationDepartment))
        {
            throw new ForbiddenException("Only the Estimation department can adjust a quote.");
        }

        var order = await FindAsync(actor, id, cancellationToken);
        if (order.Status != OrderStatus.PendingApproval)
        {
            throw new ConflictException($"Order '{id}' is {order.Status}; its quote can only be adjusted before approval.");
        }

        var total = Math.Round(request.Total, 2, MidpointRounding.AwayFromZero);
        db.OrderEvents.Add(new OrderEventHistory
        {
            OrderId = id,
            Type = OrderEventType.QuoteAdjusted,
            ActorUserId = actor.Id,
            FromValue = order.QuotedTotal.ToString("F2", CultureInfo.InvariantCulture),
            ToValue = total.ToString("F2", CultureInfo.InvariantCulture),
            Reason = request.Reason.Trim(),
        });
        order.QuotedTotal = total;
        await db.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }

    public async Task<OrderEventResponse> RequestPriorityChangeAsync(int id, PriorityChangeRequest request, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Manager, cancellationToken);
        var order = await FindAsync(actor, id, cancellationToken);
        EnsureOpen(order, "changed");
        if (order.Priority == request.Priority)
        {
            throw new BadRequestException($"The order already has priority {order.Priority}.");
        }

        var requested = new OrderEventHistory
        {
            OrderId = id,
            Type = OrderEventType.PriorityChangeRequested,
            ActorUserId = actor.Id,
            FromValue = order.Priority.ToString(),
            ToValue = request.Priority.ToString(),
            Reason = request.Reason?.Trim(),
        };
        db.OrderEvents.Add(requested);
        await db.SaveChangesAsync(cancellationToken);
        return OrderEventResponse.From(requested, hideReason: false);
    }

    private static void EnsureOpen(Order order, string action)
    {
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
        {
            throw new ConflictException($"Order '{order.Id}' is {order.Status} and cannot be {action}.");
        }
    }

    private IQueryable<Order> Scoped(Actor actor)
    {
        var orders = db.Orders.Include(o => o.Client).Include(o => o.Catalog).Include(o => o.Stages).ThenInclude(s => s.JobType).AsQueryable();
        return actor.IsClient ? orders.Where(o => o.Client!.UserId == actor.Id) : orders;
    }

    private async Task<Order> FindAsync(Actor actor, int id, CancellationToken cancellationToken) =>
        await Scoped(actor).SingleOrDefaultAsync(o => o.Id == id, cancellationToken) ?? throw new NotFoundException($"Order '{id}' was not found.");
}
