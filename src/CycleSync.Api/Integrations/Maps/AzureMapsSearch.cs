using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace CycleSync.Api.Integrations.Maps;

/// <summary>
/// Calls the Azure Maps fuzzy-search API and projects results into <see cref="MapsSearchResult"/>.
/// Requires a configured subscription key; without one it throws so the endpoint can surface an
/// <c>upstream-unavailable</c> problem. In acceptance tests this is replaced by an offline fake.
/// </summary>
public sealed class AzureMapsSearch(HttpClient httpClient, IOptions<MapsOptions> options) : IMapsSearch
{
    private readonly MapsOptions _options = options.Value;

    public async Task<IReadOnlyList<MapsSearchResult>> SearchAsync(string query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.SubscriptionKey))
        {
            throw new InvalidOperationException("Azure Maps subscription key is not configured.");
        }

        var url =
            $"{_options.BaseUrl.TrimEnd('/')}/search/fuzzy/json" +
            $"?api-version=1.0&limit=10&typeahead=true" +
            $"&subscription-key={Uri.EscapeDataString(_options.SubscriptionKey)}" +
            $"&query={Uri.EscapeDataString(query)}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
        return Project(payload);
    }

    private static List<MapsSearchResult> Project(JsonElement payload)
    {
        var results = new List<MapsSearchResult>();
        if (!payload.TryGetProperty("results", out var items) || items.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("position", out var position))
            {
                continue;
            }

            var address = item.TryGetProperty("address", out var addr) ? addr : default;
            var country = address.ValueKind == JsonValueKind.Object && address.TryGetProperty("country", out var c)
                ? c.GetString() ?? string.Empty
                : string.Empty;
            var name = BuildName(item, address, country);
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(country))
            {
                continue;
            }

            var lat = position.GetProperty("lat").GetDouble();
            var lon = position.GetProperty("lon").GetDouble();
            var id = item.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;

            results.Add(new MapsSearchResult(name, country, lat, lon, id));
        }

        return results;
    }

    private static string BuildName(JsonElement item, JsonElement address, string country)
    {
        // Prefer a POI name, then the free-form address, then "municipality, country".
        if (item.TryGetProperty("poi", out var poi) && poi.TryGetProperty("name", out var poiName))
        {
            return poiName.GetString() ?? string.Empty;
        }

        if (address.ValueKind == JsonValueKind.Object)
        {
            if (address.TryGetProperty("freeformAddress", out var freeform))
            {
                return freeform.GetString() ?? string.Empty;
            }

            if (address.TryGetProperty("municipality", out var municipality))
            {
                var city = municipality.GetString();
                return string.IsNullOrWhiteSpace(city)
                    ? country
                    : string.Create(CultureInfo.InvariantCulture, $"{city}, {country}");
            }
        }

        return country;
    }
}
