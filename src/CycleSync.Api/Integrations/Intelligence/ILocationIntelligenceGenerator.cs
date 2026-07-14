using CycleSync.Domain.Locations;

namespace CycleSync.Api.Integrations.Intelligence;

/// <summary>The generated insight for a location, tailored to the requesting user's passports.</summary>
public sealed record LocationIntelligenceContent(
    string ClimateSummary,
    string BestTimesToVisit,
    string TravelTips,
    string VisaNotes,
    Confidence Confidence);

/// <summary>
/// Server-side abstraction over the location-intelligence provider. The production implementation
/// can call an LLM; the default heuristic implementation is deterministic and offline-friendly so
/// the feature is runnable without external secrets. Output is cached per location and regenerated
/// when stale.
/// </summary>
public interface ILocationIntelligenceGenerator
{
    Task<LocationIntelligenceContent> GenerateAsync(
        Location location,
        IReadOnlyCollection<string> passportCountries,
        CancellationToken cancellationToken);
}
