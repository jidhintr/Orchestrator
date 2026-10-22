using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Application.Orders;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/orders")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class OrdersController(OrderService orders) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List orders")]
    [EndpointDescription("Newest first. Client users see only their own client's orders.")]
    [ProducesResponseType<IReadOnlyList<OrderResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<OrderResponse>> List(CancellationToken cancellationToken) =>
        await orders.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetOrder")]
    [EndpointSummary("Get an order")]
    [EndpointDescription("Includes the stages copied from the catalog with their enter and exit times. 404 for an order a client user does not own.")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<OrderResponse> Get(int id, CancellationToken cancellationToken) =>
        await orders.GetAsync(id, cancellationToken);

    [HttpGet("{id:int}/events")]
    [EndpointSummary("List the event history of an order")]
    [EndpointDescription("Append-only, oldest first. Client users do not see reasons (internal notes).")]
    [ProducesResponseType<IReadOnlyList<OrderEventResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<OrderEventResponse>> Events(int id, CancellationToken cancellationToken) =>
        await orders.EventsAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Place an order")]
    [EndpointDescription("Starts PendingApproval with one stage per catalog step and a Placed event. quantity is required; dueDate (not in the past), reference and notes are optional. The quote is computed at placement: unitPrice (catalog base price) x quantity, reduced by the catalog discount and then the client tier discount; the percentages are stored with the order. Labor cost, commission and profit are never exposed. Staff pass clientId; a client user always orders for their own linked client. 400 for an unknown client or an unknown or deactivated catalog; 403 for a client user without a linked client; 409 when the client is Suspended or inactive.")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Place(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var created = await orders.PlaceAsync(request, cancellationToken);
        return CreatedAtRoute("GetOrder", new { version = "1", id = created.Id }, created);
    }

    [HttpPost("{id:int}/cancel")]
    [EndpointSummary("Cancel an order")]
    [EndpointDescription("Manager or above. Appends a Cancelled event and returns the order to the Estimation department. 409 when the order is already Cancelled or Completed.")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<OrderResponse> Cancel(int id, CancelOrderRequest request, CancellationToken cancellationToken) =>
        await orders.CancelAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/quote")]
    [EndpointSummary("Adjust the quote of an order")]
    [EndpointDescription("Overrides quotedTotal with a reason, logged as a QuoteAdjusted event. Estimation department managers, admins and super admins only. 409 once the order is no longer PendingApproval.")]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<OrderResponse> AdjustQuote(int id, AdjustQuoteRequest request, CancellationToken cancellationToken) =>
        await orders.AdjustQuoteAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/priority-requests")]
    [EndpointSummary("Request a priority change")]
    [EndpointDescription("Manager or above. Appends a PriorityChangeRequested event; the order's priority stays until the Estimation department approves (approval arrives with the approval flow). 400 when the priority is unchanged; 409 when the order is Cancelled or Completed.")]
    [ProducesResponseType<OrderEventResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtActionResult> RequestPriorityChange(int id, PriorityChangeRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Events), new { version = "1", id }, await orders.RequestPriorityChangeAsync(id, request, cancellationToken));
}
