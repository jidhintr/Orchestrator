using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Api.Access;
using Orchestrator.Application.ApprovalChains;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/approval-chains")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class ApprovalChainsController(ApprovalChainService chains) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List approval chains")]
    [EndpointDescription("The default chain first, then catalog-specific chains by name. Stages are in approval order.")]
    [ProducesResponseType<IReadOnlyList<ApprovalChainResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ApprovalChainResponse>> List(CancellationToken cancellationToken) =>
        await chains.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetApprovalChain")]
    [EndpointSummary("Get an approval chain")]
    [ProducesResponseType<ApprovalChainResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ApprovalChainResponse> Get(int id, CancellationToken cancellationToken) =>
        await chains.GetAsync(id, cancellationToken);

    [HttpGet("for-catalog/{catalogId:int}")]
    [EndpointSummary("Get the chain a catalog follows")]
    [EndpointDescription("The catalog's own chain when it has one, otherwise the default chain.")]
    [ProducesResponseType<ApprovalChainResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ApprovalChainResponse> ForCatalog(int catalogId, CancellationToken cancellationToken) =>
        await chains.ForCatalogAsync(catalogId, cancellationToken);

    [HttpPost]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a chain for one catalog")]
    [EndpointDescription("departmentIds is the ordered list of approving departments. A catalog has at most one own chain (409); catalogs without one follow the default. 400 when the catalog or a department does not exist, a department is inactive or repeated.")]
    [ProducesResponseType<ApprovalChainResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(CreateApprovalChainRequest request, CancellationToken cancellationToken)
    {
        var created = await chains.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetApprovalChain", new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Update an approval chain")]
    [EndpointDescription("Works for the default chain and catalog chains. Replaces name, description and the whole ordered department list. The default flag and the catalog cannot change.")]
    [ProducesResponseType<ApprovalChainResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ApprovalChainResponse> Update(int id, UpdateApprovalChainRequest request, CancellationToken cancellationToken) =>
        await chains.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:int}")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Delete a catalog chain")]
    [EndpointDescription("The catalog goes back to the default chain. 409 for the default chain.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContentResult> Delete(int id, CancellationToken cancellationToken)
    {
        await chains.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
