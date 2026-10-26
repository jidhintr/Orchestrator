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

    public async Task<IReadOnlyList<PriorityResponse>> PrioritiesAsync(CancellationToken cancellationToken)
    {
        await currentUser.RequireAsync(Rank.Employee, cancellationToken);
        return [.. (await db.Priorities.AsNoTracking().OrderBy(p => p.Rank).ToListAsync(cancellationToken)).Select(PriorityResponse.From)];
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

        if (request.CommitmentDeadline < DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new BadRequestException("commitmentDeadline cannot be in the past.");
        }

        var catalogIds = request.Items.Select(i => i.CatalogId).ToList();
        if (catalogIds.Distinct().Count() != catalogIds.Count)
        {
            throw new BadRequestException("A catalog can appear only once per order.");
        }

        var priority = await db.Priorities.SingleOrDefaultAsync(p => p.Id == request.PriorityId && p.IsActive, cancellationToken)
            ?? throw new BadRequestException($"Priority {request.PriorityId} does not exist or is deactivated.");
        var catalogs = await db.Catalogs.Include(c => c.CatalogJobSteps).ThenInclude(s => s.JobType)
            .Where(c => catalogIds.Contains(c.Id) && c.IsActive).ToDictionaryAsync(c => c.Id, cancellationToken);
        if (catalogs.Count != catalogIds.Count)
        {
            throw new BadRequestException("Every catalog must exist and be active.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        foreach (var catalog in catalogs.Values)
        {
            if (catalog.ApprovedAt is null)
            {
                throw new ConflictException($"Catalog '{catalog.Sku}' is not approved yet.");
            }

            if (catalog.AvailableFrom > today || catalog.AvailableTo < today)
            {
                throw new ConflictException($"Catalog '{catalog.Sku}' is not available today.");
            }
        }

        var order = new Order
        {
            ClientId = client.Id,
            PriorityId = priority.Id,
            PriorityCost = priority.Cost,
            CommitmentDeadline = request.CommitmentDeadline,
            Reference = request.Reference?.Trim(),
            Notes = request.Notes?.Trim(),
        };
        foreach (var line in request.Items)
        {
            var catalog = catalogs[line.CatalogId];
            var gross = catalog.BasePrice * line.Quantity;
            var item = new OrderItem
            {
                CatalogId = catalog.Id,
                CatalogSku = catalog.Sku,
                CatalogName = catalog.Name,
                Quantity = line.Quantity,
                Description = line.Description?.Trim(),
                Specs = [.. (line.Specs ?? []).Select(x => new ItemSpec(x.Name.Trim(), x.Value.Trim()))],
                UnitPrice = catalog.BasePrice,
                CatalogDiscountPercent = catalog.CatalogDiscountPercent,
                ClientDiscountPercent = client.TierDiscountPercent,
                LineNet = Math.Round(gross * (1 - catalog.CatalogDiscountPercent / 100m) * (1 - client.TierDiscountPercent / 100m), 2, MidpointRounding.AwayFromZero),
            };
            item.Steps.AddRange(catalog.CatalogJobSteps.Select(s => new OrderItemJobStep
            {
                JobTypeId = s.JobTypeId,
                JobTypeName = s.JobType!.Name,
                SortOrder = s.Sequence,
                DepartmentId = s.JobType.DepartmentId,
                StandardMinutes = s.StandardMinutes,
                CanReject = s.Sequence > 1,
                LaborCost = s.JobType.LaborCost ?? catalog.LaborCost,
                CommissionPercent = s.JobType.CommissionPercent ?? catalog.CommissionPercent,
                ProfitPercent = s.JobType.ProfitPercent ?? catalog.ProfitPercent,
            }));
            order.Items.Add(item);
            order.SubTotal += gross;
        }

        var lines = order.Items.Sum(i => i.LineNet);
        order.DiscountTotal = order.SubTotal - lines;
        order.NetAmount = lines + priority.Cost;
        order.Events.Add(new OrderEventHistory { Type = OrderEventType.Placed, ActorUserId = actor.Id, ToValue = nameof(OrderStatus.PendingApproval) });
        order.Approvals.AddRange((await ApprovalDepartmentsAsync(catalogIds, cancellationToken)).Select((departmentId, index) => new OrderApproval { DepartmentId = departmentId, Sequence = index + 1 }));
        if (order.Approvals.Count == 0)
        {
            OrderFlow.Start(order, actor.Id, DateTime.UtcNow);
            order.Events.Add(new OrderEventHistory
            {
                Type = OrderEventType.ApprovalApproved,
                ActorUserId = actor.Id,
                FromValue = nameof(OrderStatus.PendingApproval),
                ToValue = nameof(OrderStatus.InProgress),
                Reason = "No approval stages: approved automatically",
            });
        }

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
            FromValue = order.NetAmount.ToString("F2", CultureInfo.InvariantCulture),
            ToValue = total.ToString("F2", CultureInfo.InvariantCulture),
            Reason = request.Reason.Trim(),
        });
        order.NetAmount = total;
        await db.SaveChangesAsync(cancellationToken);
        return OrderResponse.From(order);
    }

    public async Task<OrderEventResponse> RequestPriorityChangeAsync(int id, PriorityChangeRequest request, CancellationToken cancellationToken)
    {
        var actor = await currentUser.RequireAsync(Rank.Manager, cancellationToken);
        var order = await FindAsync(actor, id, cancellationToken);
        EnsureOpen(order, "changed");
        var priority = await db.Priorities.SingleOrDefaultAsync(p => p.Id == request.PriorityId && p.IsActive, cancellationToken)
            ?? throw new BadRequestException($"Priority {request.PriorityId} does not exist or is deactivated.");
        if (order.PriorityId == priority.Id)
        {
            throw new BadRequestException($"The order already has priority {priority.Name}.");
        }

        var requested = new OrderEventHistory
        {
            OrderId = id,
            Type = OrderEventType.PriorityChangeRequested,
            ActorUserId = actor.Id,
            FromValue = order.Priority!.Name,
            ToValue = priority.Name,
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

    private async Task<IReadOnlyList<int>> ApprovalDepartmentsAsync(List<int> catalogIds, CancellationToken cancellationToken)
    {
        var chains = await db.ApprovalChains.Include(c => c.Stages)
            .Where(c => c.IsActive && (c.IsDefault || (c.CatalogId != null && catalogIds.Contains(c.CatalogId.Value)))).ToListAsync(cancellationToken);
        var fallback = chains.Single(c => c.IsDefault);
        return [.. catalogIds.Select(id => chains.FirstOrDefault(c => c.CatalogId == id) ?? fallback).Distinct()
            .SelectMany(c => c.Stages.OrderBy(s => s.Sequence)).Select(s => s.DepartmentId).Distinct()];
    }

    private IQueryable<Order> Scoped(Actor actor)
    {
        var orders = db.Detailed();
        return actor.IsClient ? orders.Where(o => o.Client!.UserId == actor.Id) : orders;
    }

    private async Task<Order> FindAsync(Actor actor, int id, CancellationToken cancellationToken) =>
        await Scoped(actor).SingleOrDefaultAsync(o => o.Id == id, cancellationToken) ?? throw new NotFoundException($"Order '{id}' was not found.");
}
