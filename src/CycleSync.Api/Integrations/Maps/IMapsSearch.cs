namespace CycleSync.Api.Integrations.Maps;

/// <summary>A single destination returned by the maps provider.</summary>
public sealed record MapsSearchResult(string Name, string Country, double Latitude, double Longitude, string? AzureMapsId);

/// <summary>
/// Server-side abstraction over the maps provider (Azure Maps in production). Keeps the provider
/// key on the server; the SPA never talks to Azure directly. Swapped for a deterministic offline
/// fake in acceptance tests.
/// </summary>
public interface IMapsSearch
{
    Task<IReadOnlyList<MapsSearchResult>> SearchAsync(string query, CancellationToken cancellationToken);
}
