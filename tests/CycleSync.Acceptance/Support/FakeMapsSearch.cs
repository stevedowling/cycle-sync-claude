using CycleSync.Api.Integrations.Maps;

namespace CycleSync.Acceptance.Support;

/// <summary>
/// Deterministic offline stand-in for Azure Maps. Backed by a small built-in gazetteer so
/// scenarios ("Azure Maps returns results for Lisbon") are reproducible without network access.
/// A result matches when the query appears in its name (case-insensitive).
/// </summary>
public sealed class FakeMapsSearch : IMapsSearch
{
    private static readonly MapsSearchResult[] Gazetteer =
    [
        new("Lisbon, Portugal", "Portugal", 38.7223, -9.1393, "PT/Lisbon"),
        new("Auckland, New Zealand", "New Zealand", -36.8485, 174.7633, "NZ/Auckland"),
        new("London, United Kingdom", "United Kingdom", 51.5072, -0.1276, "GB/London"),
        new("Barcelona, Spain", "Spain", 41.3874, 2.1686, "ES/Barcelona"),
        new("Singapore", "Singapore", 1.3521, 103.8198, "SG/Singapore"),
    ];

    public Task<IReadOnlyList<MapsSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        var trimmed = query.Trim();
        IReadOnlyList<MapsSearchResult> matches = Gazetteer
            .Where(entry => entry.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return Task.FromResult(matches);
    }
}
