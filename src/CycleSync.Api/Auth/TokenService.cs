using System.Security.Claims;
using System.Text;
using CycleSync.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CycleSync.Api.Auth;

public interface ITokenService
{
    string CreateSessionToken(User user);
}

/// <summary>Issues the short-lived application session JWT after a successful Google sign-in.</summary>
public sealed class TokenService(IOptions<AuthOptions> options, TimeProvider clock) : ITokenService
{
    private readonly AuthOptions.JwtOptions _jwt = options.Value.Jwt;

    public string CreateSessionToken(User user)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _jwt.Issuer,
            Audience = _jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(_jwt.LifetimeMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [ClaimTypes.Name] = user.DisplayName,
            },
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
