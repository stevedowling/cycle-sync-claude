namespace CycleSync.Domain.Locations;

/// <summary>
/// Cached, AI-generated insight for a location: climate, best times to visit, travel tips, and
/// visa notes. Read-optimised and <b>not authoritative</b> — it is regenerated when stale. Every
/// row carries <see cref="GeneratedAt"/> and <see cref="Confidence"/> so the UI can always disclose
/// freshness and confidence (the "transparent costs" principle). One current row per location.
/// </summary>
public sealed class LocationIntelligence
{
    public Guid Id { get; private set; }
    public Guid LocationId { get; private set; }
    public string? ClimateSummary { get; private set; }
    public string? BestTimesToVisit { get; private set; }
    public string? TravelTips { get; private set; }
    public string? VisaNotes { get; private set; }
    public Confidence Confidence { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    private LocationIntelligence()
    {
        // EF Core
    }

    public static LocationIntelligence Generate(
        Guid locationId,
        string? climateSummary,
        string? bestTimesToVisit,
        string? travelTips,
        string? visaNotes,
        Confidence confidence,
        TimeProvider clock)
    {
        return new LocationIntelligence
        {
            Id = Guid.NewGuid(),
            LocationId = locationId,
            ClimateSummary = climateSummary,
            BestTimesToVisit = bestTimesToVisit,
            TravelTips = travelTips,
            VisaNotes = visaNotes,
            Confidence = confidence,
            GeneratedAt = clock.GetUtcNow(),
        };
    }

    /// <summary>True when the cached insight is older than <paramref name="maxAge"/> and should be regenerated.</summary>
    public bool IsStale(TimeProvider clock, TimeSpan maxAge) => clock.GetUtcNow() - GeneratedAt > maxAge;
}
