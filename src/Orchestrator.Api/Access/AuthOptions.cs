using System.ComponentModel.DataAnnotations;

namespace Orchestrator.Api.Access;

public sealed class AuthOptions
{
    public const string Section = "Auth";

    [Required, MinLength(32)]
    public string JwtKey { get; init; } = "";

    public string Issuer { get; init; } = "orchestrator";

    public string Audience { get; init; } = "orchestrator-api";

    [Range(1, 60)]
    public int AccessMinutes { get; init; } = 15;
}
