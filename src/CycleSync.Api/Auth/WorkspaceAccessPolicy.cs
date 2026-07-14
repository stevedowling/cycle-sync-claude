using Microsoft.Extensions.Options;

namespace CycleSync.Api.Auth;

/// <summary>
/// Enforces the domain restriction: only users whose email is in an allowed domain may sign in.
/// If no domains are configured, access is unrestricted (development convenience).
/// </summary>
public sealed class WorkspaceAccessPolicy(IOptions<AuthOptions> options)
{
    private readonly string[] _allowedDomains = options.Value.AllowedDomains;

    public bool IsEmailAllowed(string email)
    {
        if (_allowedDomains.Length == 0)
        {
            return true;
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
        {
            return false;
        }

        var domain = email[(atIndex + 1)..];
        return _allowedDomains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase));
    }
}
