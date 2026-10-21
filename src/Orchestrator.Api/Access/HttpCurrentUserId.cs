using Orchestrator.Application.Access;

namespace Orchestrator.Api.Access;

public sealed class HttpCurrentUserId(IHttpContextAccessor http) : ICurrentUserId
{
    public string? Raw => http.HttpContext?.Request.Headers[ActingUser.Header].ToString();
}
