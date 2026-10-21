using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Api.Access;
using Orchestrator.Api.Roles;
using Orchestrator.Api.Users;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/roles")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class RolesController(RoleService roles) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List roles")]
    [EndpointDescription("All roles, enabled and disabled, ordered by name. Anonymous for now.")]
    [ProducesResponseType<IReadOnlyList<RoleResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<RoleResponse>> List(CancellationToken cancellationToken) =>
        await roles.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetRole")]
    [EndpointSummary("Get a role")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<RoleResponse> Get(int id, CancellationToken cancellationToken) =>
        await roles.GetAsync(id, cancellationToken);

    [HttpGet("{id:int}/users")]
    [EndpointSummary("List the users that have a role")]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<UserResponse>> Users(int id, CancellationToken cancellationToken) =>
        await roles.UsersAsync(id, cancellationToken);

    [HttpPost]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Create a role")]
    [EndpointDescription("Returns 201 with a Location header. 409 when the name (case-insensitive) is already used.")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(RoleRequest request, CancellationToken cancellationToken)
    {
        var created = await roles.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetRole", new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Update a role")]
    [EndpointDescription("409 when the name is already used or a system role would be renamed.")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<RoleResponse> Update(int id, RoleRequest request, CancellationToken cancellationToken) =>
        await roles.UpdateAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/disable")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Disable a role")]
    [EndpointDescription("A disabled role cannot be assigned to users; existing assignments are kept. 409 for system roles.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContentResult> Disable(int id, CancellationToken cancellationToken)
    {
        await roles.SetActiveAsync(id, false, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/enable")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Enable a role")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<NoContentResult> Enable(int id, CancellationToken cancellationToken)
    {
        await roles.SetActiveAsync(id, true, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [AdminOnly]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Delete a role")]
    [EndpointDescription("Permanent. 409 for system roles and for roles still assigned to users.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<NoContentResult> Delete(int id, CancellationToken cancellationToken)
    {
        await roles.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
