using CycleSync.Api.Integrations.Maps;
using CycleSync.Domain.Locations;

namespace CycleSync.Api.Integrations.Cost;

/// <summary>
/// Deterministic, offline-friendly cost heuristic so Phase 4 is runnable without a paid flight
/// provider. Flights scale with the great-circle distance from the traveller's home to the
/// destination — so two travellers with different home locations get different flight figures. The
/// home is geocoded through <see cref="IMapsSearch"/>; when it cannot be resolved, a stable
/// name-derived fallback keeps estimates deterministic and still home-specific. Accommodation and
/// daily expenses scale with the number of nights. Reports <see cref="Confidence.Medium"/> — it is
/// distance-aware but not a live quote. Replace behind <see cref="ICostEstimator"/> to raise fidelity.
/// </summary>
public sealed class HeuristicCostEstimator(IMapsSearch maps) : ICostEstimator
{
    private const decimal FlightBaseFare = 150m;        // fixed per-trip component (USD-ish)
    private const decimal FlightPerKm = 0.11m;          // marginal cost per km flown
    private const decimal NightlyAccommodation = 120m;  // per night (USD-ish)
    private const decimal DailyExpenses = 85m;          // per night on the ground (USD-ish)

    // Rough currency multipliers so the headline figure reads plausibly in the traveller's currency.
    // Not FX rates — a heuristic, disclosed as such via Confidence.Medium.
    private static readonly IReadOnlyDictionary<string, decimal> CurrencyFactors =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["USD"] = 1.00m,
            ["EUR"] = 0.92m,
            ["GBP"] = 0.79m,
            ["NZD"] = 1.65m,
            ["AUD"] = 1.52m,
        };

    public async Task<CostEstimate> EstimateAsync(
        string? originName,
        Location destination,
        int nights,
        string currency,
        CancellationToken cancellationToken)
    {
        var effectiveNights = Math.Max(nights, 1);
        var factor = CurrencyFactors.TryGetValue(currency, out var f) ? f : 1.00m;

        var distanceKm = await EstimateOriginDistanceKmAsync(originName, destination, cancellationToken);
        var flightsUsd = FlightBaseFare + ((decimal)distanceKm * FlightPerKm);

        var flights = Round(flightsUsd * factor);
        var accommodation = Round(NightlyAccommodation * effectiveNights * factor);
        var daily = Round(DailyExpenses * effectiveNights * factor);

        return new CostEstimate(currency, flights, accommodation, daily, nights, Confidence.Medium);
    }

    /// <summary>
    /// Great-circle distance (km) from the traveller's home to the destination. Falls back to a
    /// stable, name-derived pseudo-distance when the home cannot be geocoded, so the estimate stays
    /// deterministic and home-specific even offline.
    /// </summary>
    private async Task<double> EstimateOriginDistanceKmAsync(
        string? originName,
        Location destination,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(originName))
        {
            var matches = await maps.SearchAsync(originName, cancellationToken);
            var origin = matches.Count > 0 ? matches[0] : null;
            if (origin is not null)
            {
                return HaversineKm(
                    origin.Latitude, origin.Longitude,
                    destination.Coordinates.Latitude, destination.Coordinates.Longitude);
            }

            // Deterministic fallback keyed on the home name: 500–8500 km.
            var seed = (uint)StableHash(originName.Trim());
            return 500 + (seed % 8000);
        }

        return 0;
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = (Math.Sin(dLat / 2) * Math.Sin(dLat / 2)) +
                (Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                 Math.Sin(dLon / 2) * Math.Sin(dLon / 2));
        return earthRadiusKm * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180.0);

    /// <summary>A small, stable (non-cryptographic) hash — deterministic across runs, unlike string.GetHashCode.</summary>
    private static int StableHash(string value)
    {
        unchecked
        {
            var hash = 17;
            foreach (var c in value)
            {
                hash = (hash * 31) + c;
            }

            return hash & 0x7fffffff;
        }
    }
}
