using CycleSync.Api.Auth;

namespace CycleSync.Acceptance.Support;

/// <summary>
/// Deterministic offline stand-in for Google token validation. The "ID token" is simply the
/// user's email; the display name is derived from the local part. This exercises the real
/// domain-restriction and provisioning logic without contacting Google.
/// </summary>
public sealed class FakeGoogleTokenValidator : IGoogleTokenValidator
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
