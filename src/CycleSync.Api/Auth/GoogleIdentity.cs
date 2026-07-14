namespace CycleSync.Api.Auth;

/// <summary>The identity established by validating a Google ID token.</summary>
public sealed record GoogleIdentity(string Email, string Name, string? HostedDomain);
