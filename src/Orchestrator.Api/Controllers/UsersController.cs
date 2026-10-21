using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Api.Roles;
using Orchestrator.Api.Users;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class UsersController(UserService users) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List users")]
    [EndpointDescription("All users, active and deactivated, ordered by name. Anonymous for now.")]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<UserResponse>> List(CancellationToken cancellationToken) =>
        await users.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetUser")]
    [EndpointSummary("Get a user")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<UserResponse> Get(int id, CancellationToken cancellationToken) =>
        await users.GetAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Create a user")]
    [EndpointDescription("Returns 201 with a Location header. Roles are names of existing enabled roles. 400 for an unknown role or department; 409 when the email (case-insensitive) is already used.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await users.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetUser", new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update a user")]
    [EndpointDescription("Full replace; isActive false deactivates (soft delete). 400 for an unknown role or department; 409 when the email is already used.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<UserResponse> Update(int id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        await users.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/roles")]
    [EndpointSummary("Assign roles to a user")]
    [EndpointDescription("Replaces the user's roles with the given role names. 400 when a role does not exist or is disabled.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<UserResponse> AssignRoles(int id, AssignRolesRequest request, CancellationToken cancellationToken) =>
        await users.AssignRolesAsync(id, request, cancellationToken);
}
