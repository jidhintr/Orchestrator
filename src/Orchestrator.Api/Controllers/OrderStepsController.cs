using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Orchestrator.Application.Orders;
using Orchestrator.Domain;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/order-steps")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class OrderStepsController(OrderStepService steps) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List the work queue of job steps")]
    [EndpointDescription("Steps that are no longer Waiting, for the caller's departments (admins see all), highest priority and longest waiting first, at most 500. Filter by status, departmentId or assignedToMe. Clients get 403.")]
    [ProducesResponseType<IReadOnlyList<StepResponse>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<StepResponse>> List(StepStatus? status, bool assignedToMe, int? departmentId, CancellationToken cancellationToken) =>
        await steps.ListAsync(status, assignedToMe, departmentId, cancellationToken);

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a job step")]
    [EndpointDescription("waitMinutes is ready to start, workMinutes is start to completion (held time included); both run to now while open. 404 for steps outside the caller's departments.")]
    [ProducesResponseType<StepResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<StepResponse> Get(int id, CancellationToken cancellationToken) =>
        await steps.GetAsync(id, cancellationToken);

    [HttpGet("{id:int}/logs")]
    [EndpointSummary("List the history of a job step")]
    [EndpointDescription("Append-only, oldest first: every ready, assign, start, progress, hold, resume, complete and reject.")]
    [ProducesResponseType<IReadOnlyList<StepLogResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IReadOnlyList<StepLogResponse>> Logs(int id, CancellationToken cancellationToken) =>
        await steps.LogsAsync(id, cancellationToken);

    [HttpPost("{id:int}/assign")]
    [EndpointSummary("Assign a job step")]
    [EndpointDescription("Employees of the step's department can assign only themselves; managers of the department (and admins) anyone in it. Allowed while Ready, InProgress or OnHold. 400 when the user is not an active member of the department.")]
    [ProducesResponseType<StepResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<StepResponse> Assign(int id, AssignStepRequest request, CancellationToken cancellationToken) =>
        await steps.AssignAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/start")]
    [EndpointSummary("Start a job step")]
    [EndpointDescription("Ready to InProgress, by the assignee or a manager of the department (a manager start assigns an unassigned step to them). Records the start time.")]
    [ProducesResponseType<StepResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<StepResponse> Start(int id, CancellationToken cancellationToken) =>
        await steps.StartAsync(id, cancellationToken);

    [HttpPost("{id:int}/progress")]
    [EndpointSummary("Report progress on a job step")]
    [EndpointDescription("InProgress only, 0 to 99 percent with an optional note; completing sets 100.")]
    [ProducesResponseType<StepResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<StepResponse> Progress(int id, StepProgressRequest request, CancellationToken cancellationToken) =>
        await steps.ProgressAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/hold")]
    [EndpointSummary("Put a job step on hold")]
    [EndpointDescription("InProgress to OnHold with a reason (for example a machine fault or out of stock).")]
    [ProducesResponseType<StepResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<StepResponse> Hold(int id, StepReasonRequest request, CancellationToken cancellationToken) =>
        await steps.HoldAsync(id, request, cancellationToken);

    [HttpPost("{id:int}/resume")]
    [EndpointSummary("Resume a job step on hold")]
    [ProducesResponseType<StepResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<StepResponse> Resume(int id, CancellationToken cancellationToken) =>
        await steps.ResumeAsync(id, cancellationToken);

    [HttpPost("{id:int}/complete")]
    [EndpointSummary("Complete a job step")]
    [EndpointDescription("InProgress to Completed, by the assignee or a manager. The next step of the item becomes Ready (the next department's alert); completing the last open step completes the order. Earnings are not calculated yet (see docs/TODO-earnings.md).")]
    [ProducesResponseType<StepResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<StepResponse> Complete(int id, CancellationToken cancellationToken) =>
        await steps.CompleteAsync(id, cancellationToken);

    [HttpPost("{id:int}/reject")]
    [EndpointSummary("Reject the work back to the previous step")]
    [EndpointDescription("Only for an InProgress step that can reject (every step but the first). The previous step is reopened as Ready and this one returns to Waiting; both are logged, so rework can be counted.")]
    [ProducesResponseType<StepResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<StepResponse> Reject(int id, StepReasonRequest request, CancellationToken cancellationToken) =>
        await steps.RejectAsync(id, request, cancellationToken);
}
