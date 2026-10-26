using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Api.Access;
using Orchestrator.Application.Catalogs;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/catalogs")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CatalogsController(CatalogService catalogs) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List catalogs")]
    [EndpointDescription("All catalogs, active and deactivated, ordered by name, each with its job types in sequence.")]
    [ProducesResponseType<IReadOnlyList<CatalogResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CatalogResponse>> List(CancellationToken cancellationToken) =>
        await catalogs.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetCatalog")]
    [EndpointSummary("Get a catalog")]
    [ProducesResponseType<CatalogResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<CatalogResponse> Get(int id, CancellationToken cancellationToken) =>
        await catalogs.GetAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Create a catalog")]
    [EndpointDescription("jobTypeIds is the ordered sequence of job types (repeats allowed). The catalog starts unapproved (see approve). categoryId is optional; availableFrom and availableTo optionally limit when it can be ordered. Returns 201 with a Location header. 400 when a job type or category does not exist or is inactive, or availableFrom is after availableTo; 409 when the SKU is already used.")]
    [ProducesResponseType<CatalogResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(CatalogRequest request, CancellationToken cancellationToken)
    {
        var created = await catalogs.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetCatalog", new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update a catalog")]
    [EndpointDescription("Replaces the fields and the whole job type sequence; reorder, add or remove job types by sending the new list. 409 when any order references the catalog (clone it instead) or the SKU is already used.")]
    [ProducesResponseType<CatalogResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CatalogResponse> Update(int id, CatalogRequest request, CancellationToken cancellationToken) =>
        await catalogs.UpdateAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/deactivate")]
    [EndpointSummary("Deactivate a catalog")]
    [EndpointDescription("Soft delete: the record is kept with isActive false. 409 when any order references the catalog.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContentResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await catalogs.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/activate")]
    [EndpointSummary("Activate a catalog")]
    [EndpointDescription("Undoes a deactivation: sets isActive back to true.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContentResult> Activate(int id, CancellationToken cancellationToken)
    {
        await catalogs.ActivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/approve")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Approve a catalog")]
    [EndpointDescription("Admins only. A new or cloned catalog cannot be ordered until it is approved. 409 when it is already approved.")]
    [ProducesResponseType<CatalogResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CatalogResponse> Approve(int id, CancellationToken cancellationToken) =>
        await catalogs.ApproveAsync(id, cancellationToken);

    [HttpPost("{id:int}/clone")]
    [EndpointSummary("Clone a catalog")]
    [EndpointDescription("Copies fields, category, availability dates and job type sequence into a new, unapproved catalog with the given SKU and name, linked to the source by basedOnCatalogId; allowed even when the source has orders. 409 when the SKU is already used.")]
    [ProducesResponseType<CatalogResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Clone(int id, CloneCatalogRequest request, CancellationToken cancellationToken)
    {
        var created = await catalogs.CloneAsync(id, request, cancellationToken);
        return CreatedAtRoute("GetCatalog", new { version = "1", id = created.Id }, created);
    }
}
