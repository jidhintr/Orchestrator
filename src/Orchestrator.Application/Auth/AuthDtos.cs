using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Application.Auth;

public sealed record LoginRequest
{
    [Required, StringLength(254, MinimumLength = 3)]
    public required string Login { get; init; }

    [Required, StringLength(128, MinimumLength = 1)]
    public required string Password { get; init; }
}

public sealed record ChangePasswordRequest
{
    [Required, StringLength(128, MinimumLength = 1)]
    public required string CurrentPassword { get; init; }

    [Required, StringLength(128, MinimumLength = 10)]
    public required string NewPassword { get; init; }
}

public sealed record SetPasswordRequest
{
    [Required, StringLength(128, MinimumLength = 10)]
    public required string Password { get; init; }
}

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, int UserId);

public sealed record SignedIn(int UserId, string RefreshToken, DateTime RefreshExpiresAt);
