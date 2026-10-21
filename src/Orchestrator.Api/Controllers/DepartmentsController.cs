using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Api.Departments;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/departments")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class DepartmentsController(DepartmentService departments) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List departments")]
    [EndpointDescription("All departments, active and deactivated, ordered by name.")]
    [ProducesResponseType<IReadOnlyList<DepartmentResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<DepartmentResponse>> List(CancellationToken cancellationToken) =>
        await departments.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = nameof(Get))]
    [EndpointSummary("Get a department")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<DepartmentResponse> Get(int id, CancellationToken cancellationToken) =>
        await departments.GetAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Create a department")]
    [EndpointDescription("Returns 201 with a Location header. 409 when the name (case-insensitive) or code is already used.")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(CreateDepartmentRequest request, CancellationToken cancellationToken)
    {
        var created = await departments.CreateAsync(request, cancellationToken);
        return CreatedAtRoute(nameof(Get), new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update a department")]
    [EndpointDescription("Changes name and description only; the code is immutable. 409 when the name is already used or a system department would be renamed.")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<DepartmentResponse> Update(int id, UpdateDepartmentRequest request, CancellationToken cancellationToken) =>
        await departments.UpdateAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/deactivate")]
    [EndpointSummary("Deactivate a department")]
    [EndpointDescription("Soft delete: the record is kept with isActive false. 409 for system departments (isSystem true).")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContentResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        await departments.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
