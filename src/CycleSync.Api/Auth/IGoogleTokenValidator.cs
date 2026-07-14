namespace CycleSync.Api.Auth;

/// <summary>
/// Validates a Google-issued ID token and returns the identity. The real implementation calls
/// Google; tests substitute a deterministic fake so scenarios run offline.
/// </summary>
public interface IGoogleTokenValidator
{
    Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
