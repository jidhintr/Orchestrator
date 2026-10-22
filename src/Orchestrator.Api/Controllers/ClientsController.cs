using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Application.Clients;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/clients")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ClientsController(ClientService clients) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List clients")]
    [EndpointDescription("All clients in every status, ordered by company name.")]
    [ProducesResponseType<IReadOnlyList<ClientResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ClientResponse>> List(CancellationToken cancellationToken) =>
        await clients.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetClient")]
    [EndpointSummary("Get a client")]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ClientResponse> Get(int id, CancellationToken cancellationToken) =>
        await clients.GetAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Create a client")]
    [EndpointDescription("Starts Active. tierDiscountPercent is derived from the tier (placeholder: Normal 0, Gold 5, Platinum 10). Returns 201 with a Location header. 409 when the email is already used.")]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(ClientRequest request, CancellationToken cancellationToken)
    {
        var created = await clients.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetClient", new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update a client")]
    [EndpointDescription("Replaces the profile fields and tier; the discount follows the tier. The status is changed separately. 409 when the email is already used.")]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ClientResponse> Update(int id, ClientRequest request, CancellationToken cancellationToken) =>
        await clients.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/status")]
    [EndpointSummary("Set the account status of a client")]
    [EndpointDescription("Active, PaymentPending, Suspended or Inactive. Inactive also sets isActive false. Blocking order creation for Suspended clients arrives with orders.")]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ClientResponse> SetStatus(int id, ClientStatusRequest request, CancellationToken cancellationToken) =>
        await clients.SetStatusAsync(id, request, cancellationToken);
}
