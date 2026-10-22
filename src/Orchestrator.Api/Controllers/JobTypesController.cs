using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Api.Access;
using Orchestrator.Application.Catalogs;
using Orchestrator.Application.JobTypes;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/job-types")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class JobTypesController(JobTypeService jobTypes) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List job types")]
    [EndpointDescription("All job types, active and deactivated, ordered by name.")]
    [ProducesResponseType<IReadOnlyList<JobTypeResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<JobTypeResponse>> List(CancellationToken cancellationToken) =>
        await jobTypes.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetJobType")]
    [EndpointSummary("Get a job type")]
    [ProducesResponseType<JobTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<JobTypeResponse> Get(int id, CancellationToken cancellationToken) =>
        await jobTypes.GetAsync(id, cancellationToken);

    [HttpGet("{id:int}/catalogs")]
    [EndpointSummary("List the catalogs that use a job type")]
    [EndpointDescription("Catalogs with this job type in their sequence, ordered by name.")]
    [ProducesResponseType<IReadOnlyList<CatalogResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<CatalogResponse>> Catalogs(int id, CancellationToken cancellationToken) =>
        await jobTypes.CatalogsAsync(id, cancellationToken);

    [HttpPost]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a job type")]
    [EndpointDescription("Cost, commission and profit are optional step overrides; when null the catalog values apply. Returns 201 with a Location header. 409 when the name (case-insensitive) is already used.")]
    [ProducesResponseType<JobTypeResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(JobTypeRequest request, CancellationToken cancellationToken)
    {
        var created = await jobTypes.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetJobType", new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Update a job type")]
    [EndpointDescription("Replaces name, description and the cost overrides. 409 when the name is already used.")]
    [ProducesResponseType<JobTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<JobTypeResponse> Update(int id, JobTypeRequest request, CancellationToken cancellationToken) =>
        await jobTypes.UpdateAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/deactivate")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Deactivate a job type")]
    [EndpointDescription("Soft delete: the record is kept with isActive false and can no longer be added to catalogs; existing catalogs keep it.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContentResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await jobTypes.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/activate")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Activate a job type")]
    [EndpointDescription("Undoes a deactivation: sets isActive back to true so it can be added to catalogs again.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContentResult> Activate(int id, CancellationToken cancellationToken)
    {
        await jobTypes.ActivateAsync(id, cancellationToken);
        return NoContent();
    }
}
