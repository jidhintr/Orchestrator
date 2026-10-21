using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Application.Roles;
using Orchestrator.Application.Users;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/users")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class UsersController(UserService users) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List users")]
    [EndpointDescription("All users, active and deactivated, ordered by name.")]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<UserResponse>> List(CancellationToken cancellationToken) =>
        await users.ListAsync(cancellationToken);

    [HttpGet("{id:int}", Name = "GetUser")]
    [EndpointSummary("Get a user")]
    [EndpointDescription("departments are the user's departments, departmentRoles the roles inherited from them, roles the user's own extra roles.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<UserResponse> Get(int id, CancellationToken cancellationToken) =>
        await users.GetAsync(id, cancellationToken);

    [HttpPost]
    [EndpointSummary("Create a user")]
    [EndpointDescription("Needs the X-Acting-User-Id header (temporary, until authentication) of a manager, admin or super admin. Without departmentIds the user joins the Unassigned department, or the manager's own departments when a manager creates them. Managers may only use their own departments and roles of those departments; admins cannot create admins or super admins. 400 for an unknown department or role; 409 when the email is already used.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CreatedAtRouteResult> Create(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var created = await users.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetUser", new { version = "1", id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update a user's profile")]
    [EndpointDescription("Name, email, mobile and isActive (false deactivates). Needs X-Acting-User-Id; nobody can edit a user at or above their own rank, and managers only users in their own departments. 409 when the email is already used.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<UserResponse> Update(int id, UpdateUserRequest request, CancellationToken cancellationToken) =>
        await users.UpdateAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/departments")]
    [EndpointSummary("Set a user's departments")]
    [EndpointDescription("Replaces the user's departments; an empty list leaves them in Unassigned. A manager replaces only the departments they manage, the user keeps the rest. 403 for departments or users outside your rights; 400 for an unknown or deactivated department.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<UserResponse> AssignDepartments(int id, AssignDepartmentsRequest request, CancellationToken cancellationToken) =>
        await users.AssignDepartmentsAsync(id, request, cancellationToken);

    [HttpPut("{id:int}/roles")]
    [EndpointSummary("Set a user's extra roles")]
    [EndpointDescription("Replaces the user's own roles (on top of the department roles). You can only give roles below your rank; a manager only roles that belong to their own departments. 400 when a role does not exist or is disabled.")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<UserResponse> AssignRoles(int id, AssignRolesRequest request, CancellationToken cancellationToken) =>
        await users.AssignRolesAsync(id, request, cancellationToken);
}
