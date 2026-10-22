using Microsoft.AspNetCore.Mvc.Filters;
using Orchestrator.Application.Access;
using Orchestrator.Domain;

namespace Orchestrator.Api.Access;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AdminOnlyAttribute : Attribute, IAsyncActionFilter, IOrderedFilter
{
    // Runs before the automatic 400 validation filter (-2000), so callers without rights never learn about validation.
    public int Order => -3000;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await context.HttpContext.RequestServices.GetRequiredService<ActingUser>()
            .RequireAsync(Rank.Admin, context.HttpContext.RequestAborted);
        await next();
    }
}
