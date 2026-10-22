using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Orchestrator.Api.Access;

public sealed class JwtIssuer(IOptions<AuthOptions> options)
{
    public static SymmetricSecurityKey Key(AuthOptions o) => new(Encoding.UTF8.GetBytes(o.JwtKey));

    public (string Token, DateTime ExpiresAt) Create(int userId)
    {
        var o = options.Value;
        var expires = DateTime.UtcNow.AddMinutes(o.AccessMinutes);
        var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([new Claim("sub", userId.ToString(CultureInfo.InvariantCulture))]),
            Issuer = o.Issuer,
            Audience = o.Audience,
            Expires = expires,
            SigningCredentials = new SigningCredentials(Key(o), SecurityAlgorithms.HmacSha256),
        });
        return (token, expires);
    }
}
