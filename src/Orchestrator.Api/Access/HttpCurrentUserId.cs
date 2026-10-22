using System.Security.Claims;
using Orchestrator.Application.Access;

namespace Orchestrator.Api.Access;

public sealed class HttpCurrentUserId(IHttpContextAccessor http) : ICurrentUserId
{
    public int? Id => int.TryParse(http.HttpContext?.User.FindFirstValue("sub"), out var id) ? id : null;
}
