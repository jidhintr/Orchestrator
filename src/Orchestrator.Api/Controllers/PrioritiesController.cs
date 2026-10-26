using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Application.Orders;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/priorities")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class PrioritiesController(OrderService orders) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List order priorities")]
    [EndpointDescription("Ordered by rank. cost is the flat amount added to the order total (placeholder values pending owner confirmation).")]
    [ProducesResponseType<IReadOnlyList<PriorityResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<PriorityResponse>> List(CancellationToken cancellationToken) =>
        await orders.PrioritiesAsync(cancellationToken);
}
