using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Orchestrator.Api.Access;
using Orchestrator.Application.Auth;

namespace Orchestrator.Api.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/auth")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
public sealed class AuthController(AuthService auth, JwtIssuer jwt) : ControllerBase
{
    private const string RefreshCookie = "refresh_token";

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [EndpointSummary("Sign in")]
    [EndpointDescription("login is the user's email or registered mobile number. Returns a short-lived access token (send it as 'Authorization: Bearer ...') and sets an httpOnly refresh cookie. 401 with one generic message for any wrong login or password; the account locks for 15 minutes after 5 wrong passwords. 429 when called too often.")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<AuthResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Issue(await auth.LoginAsync(request, cancellationToken));

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    [EndpointSummary("Get a new access token")]
    [EndpointDescription("Uses the refresh cookie and rotates it. Reusing an old refresh cookie ends the whole session. 401 when the cookie is missing, expired or revoked.")]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<AuthResponse> Refresh(CancellationToken cancellationToken) =>
        Issue(await auth.RefreshAsync(Request.Cookies[RefreshCookie], cancellationToken));

    [HttpPost("logout")]
    [AllowAnonymous]
    [EndpointSummary("Sign out")]
    [EndpointDescription("Revokes the session behind the refresh cookie and clears it. Always 204.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<NoContentResult> Logout(CancellationToken cancellationToken)
    {
        await auth.LogoutAsync(Request.Cookies[RefreshCookie], cancellationToken);
        Response.Cookies.Delete(RefreshCookie, CookieOptions(null));
        return NoContent();
    }

    [HttpPost("change-password")]
    [EndpointSummary("Change your own password")]
    [EndpointDescription("Needs the current password; the new one is 10 to 128 characters. Ends every session, so sign in again afterwards. 400 when the current password is wrong.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<NoContentResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        await auth.ChangePasswordAsync(request, cancellationToken);
        Response.Cookies.Delete(RefreshCookie, CookieOptions(null));
        return NoContent();
    }

    private AuthResponse Issue(SignedIn signedIn)
    {
        Response.Cookies.Append(RefreshCookie, signedIn.RefreshToken, CookieOptions(signedIn.RefreshExpiresAt));
        var (token, expiresAt) = jwt.Create(signedIn.UserId);
        return new AuthResponse(token, expiresAt, signedIn.UserId);
    }

    private static CookieOptions CookieOptions(DateTime? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = "/api/v1/auth",
        Expires = expires,
    };
}
