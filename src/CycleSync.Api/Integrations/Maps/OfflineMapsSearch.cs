namespace CycleSync.Api.Integrations.Maps;

/// <summary>
/// Deterministic, offline gazetteer used <b>only</b> in the hermetic <c>E2E</c> environment so
/// Playwright can search without an Azure Maps key. A result matches when the query appears in its
/// name (case-insensitive). Never registered in Development or Production — see <c>Program.cs</c>.
/// </summary>
public sealed class OfflineMapsSearch : IMapsSearch
{
    private static readonly MapsSearchResult[] Gazetteer =
    [
        new("Lisbon, Portugal", "Portugal", 38.7223, -9.1393, "PT/Lisbon"),
        new("Auckland, New Zealand", "New Zealand", -36.8485, 174.7633, "NZ/Auckland"),
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
