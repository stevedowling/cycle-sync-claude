using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace CycleSync.Api.Auth;

public interface ICurrentUser
{
    Guid Id { get; }
    string Email { get; }
}

/// <summary>Reads the authenticated user's identity from the current request's claims.</summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User
        ?? throw new InvalidOperationException("No authenticated user on the current request.");

    public Guid Id
    {
        get
        {
            var sub = Principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(sub, out var id)
                ? id
                : throw new InvalidOperationException("The session token has no valid subject claim.");
        }
    }

    public string Email =>
        Principal.FindFirstValue(JwtRegisteredClaimNames.Email)
        ?? Principal.FindFirstValue(ClaimTypes.Email)
        ?? throw new InvalidOperationException("The session token has no email claim.");
}
