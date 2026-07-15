namespace CycleSync.Domain;

/// <summary>
/// Raised when an operation would violate a domain invariant (e.g. an off-cycle whose end date
/// precedes its start date). Carries a stable, user-facing <see cref="Exception.Message"/> that the
/// API surfaces verbatim as the RFC 7807 <c>detail</c> under <c>type: validation</c>.
/// </summary>
public sealed class DomainValidationException(string message) : Exception(message);
