namespace CycleSync.Api.Auth;

/// <summary>
/// Offline stand-in for Google token validation, used in <c>Development</c> and the hermetic
/// <c>E2E</c> environment so the dev email sign-in works (and Playwright can sign in) without
/// contacting Google. The "ID token" is simply the user's email; the display name is derived from
/// the local part. Never registered in Production — see <c>Program.cs</c>.
/// </summary>
public sealed class OfflineGoogleTokenValidator : IGoogleTokenValidator
{
    public Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var email = idToken.Trim();
        var atIndex = email.IndexOf('@');
        var localPart = atIndex > 0 ? email[..atIndex] : email;
        var name = localPart.Length == 0
            ? email
            : char.ToUpperInvariant(localPart[0]) + localPart[1..];
        var hostedDomain = atIndex > 0 && atIndex < email.Length - 1 ? email[(atIndex + 1)..] : null;

        return Task.FromResult(new GoogleIdentity(email, name, hostedDomain));
    }
}
