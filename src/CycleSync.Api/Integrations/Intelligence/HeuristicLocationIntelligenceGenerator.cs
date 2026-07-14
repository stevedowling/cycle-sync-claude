using CycleSync.Domain.Locations;

namespace CycleSync.Api.Integrations.Intelligence;

/// <summary>
/// Deterministic, offline stand-in for AI-generated location intelligence. It produces plausible
/// climate/travel/visa summaries from the location and the requester's passports without calling an
/// external model, so Phase 2 is runnable and testable without secrets. Because it is templated
/// rather than model-generated, it reports <see cref="Confidence.Low"/>. Swap in an LLM-backed
/// implementation behind <see cref="ILocationIntelligenceGenerator"/> to raise fidelity.
/// </summary>
public sealed class HeuristicLocationIntelligenceGenerator : ILocationIntelligenceGenerator
{
    public Task<LocationIntelligenceContent> GenerateAsync(
        Location location,
        IReadOnlyCollection<string> passportCountries,
        CancellationToken cancellationToken)
    {
        var climate =
            $"{location.Name} has a generally temperate climate, with warmer, drier summers and " +
            "cooler, wetter winters. Expect pleasant conditions in the shoulder seasons.";

        var bestTimes =
            "The best times to visit are spring (roughly March–May) and early autumn " +
            "(September–October), when temperatures are comfortable and crowds are thinner.";

        var travelTips =
            $"Getting around {location.Country} is straightforward by public transport. Book " +
            "accommodation early around peak season, and carry a payment card as contactless is widely accepted.";

        var visaNotes = BuildVisaNotes(location, passportCountries);

        var content = new LocationIntelligenceContent(climate, bestTimes, travelTips, visaNotes, Confidence.Low);
        return Task.FromResult(content);
    }

    private static string BuildVisaNotes(Location location, IReadOnlyCollection<string> passportCountries)
    {
        if (passportCountries.Count == 0)
        {
            return "Add a passport to your profile to see visa guidance for this destination.";
        }

        var holders = string.Join(", ", passportCountries);
        return
            $"Visa guidance for {holders} passport holders travelling to {location.Country}: short " +
            "visits are frequently visa-exempt or eligible for a visa on arrival, but rules change — " +
            "always confirm current entry requirements before booking.";
    }
}
