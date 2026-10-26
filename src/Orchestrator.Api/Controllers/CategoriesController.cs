using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Api.Access;
using Orchestrator.Application.Categories;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/categories")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class CategoriesController(CategoryService categories) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List catalog categories")]
    [EndpointDescription("All categories, active and deactivated, ordered by name.")]
    [ProducesResponseType<IReadOnlyList<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<CategoryResponse>> List(CancellationToken cancellationToken) =>
        await categories.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetCategory")]
    [EndpointSummary("Get a catalog category")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<CategoryResponse> Get(int id, CancellationToken cancellationToken) =>
        await categories.GetAsync(id, cancellationToken);

    [HttpPost]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a catalog category")]
    [EndpointDescription("Returns 201 with a Location header. 409 when the name (case-insensitive) is already used.")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(CategoryRequest request, CancellationToken cancellationToken)
    {
        var created = await categories.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetCategory", new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Update a catalog category")]
    [EndpointDescription("Replaces name and description. 409 when the name is already used.")]
    [ProducesResponseType<CategoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CategoryResponse> Update(int id, CategoryRequest request, CancellationToken cancellationToken) =>
        await categories.UpdateAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/deactivate")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Deactivate a catalog category")]
    [EndpointDescription("Soft delete: the record is kept with isActive false and can no longer be chosen for catalogs; existing catalogs keep it.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContentResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await categories.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/activate")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Activate a catalog category")]
    [EndpointDescription("Undoes a deactivation: sets isActive back to true.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContentResult> Activate(int id, CancellationToken cancellationToken)
    {
        await categories.ActivateAsync(id, cancellationToken);
        return NoContent();
    }
}
