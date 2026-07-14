using Google.Apis.Auth;
using Microsoft.Extensions.Options;

namespace CycleSync.Api.Auth;

/// <summary>Validates Google ID tokens using Google's public keys and the configured client id.</summary>
public sealed class GoogleTokenValidator(IOptions<AuthOptions> options) : IGoogleTokenValidator
{
    private readonly AuthOptions _options = options.Value;

    public async Task<GoogleIdentity> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        var settings = new GoogleJsonWebSignature.ValidationSettings();
        if (!string.IsNullOrWhiteSpace(_options.Google.ClientId))
        {
            settings.Audience = [_options.Google.ClientId];
        }

        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        return new GoogleIdentity(payload.Email, payload.Name ?? payload.Email, payload.HostedDomain);
    }
}
