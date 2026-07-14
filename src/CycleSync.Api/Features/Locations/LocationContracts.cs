using CycleSync.Domain.Locations;

namespace CycleSync.Api.Features.Locations;

public sealed record CoordinatesDto(double Latitude, double Longitude);

public sealed record LocationSearchResultDto(string Name, string Country, CoordinatesDto Coordinates, string? AzureMapsId);

public sealed record PersistLocationRequest(string Name, string Country, double Latitude, double Longitude, string? AzureMapsId);

public sealed record LocationResponse(
    Guid Id,
    string Name,
    string Country,
    CoordinatesDto Coordinates,
    DateTimeOffset CreatedAt,
    int InterestCount,
    bool IsInterested);

public sealed record LocationIntelligenceResponse(
    Guid LocationId,
    string? ClimateSummary,
    string? BestTimesToVisit,
    string? TravelTips,
    string? VisaNotes,
    string Confidence,
    DateTimeOffset GeneratedAt);

public static class LocationMapping
{
    /// <summary>
    /// Maps a location to its API response. <paramref name="interestCount"/> and
    /// <paramref name="isInterested"/> default to the "no interest known" values so callers that
    /// have not aggregated interest (e.g. the persist endpoint returning a freshly created row) can
    /// omit them; the list and detail endpoints supply the real figures.
    /// </summary>
    public static LocationResponse ToResponse(this Location location, int interestCount = 0, bool isInterested = false) => new(
        location.Id,
        location.Name,
        location.Country,
        new CoordinatesDto(location.Coordinates.Latitude, location.Coordinates.Longitude),
        location.CreatedAt,
        interestCount,
        isInterested);

    public static LocationIntelligenceResponse ToResponse(this LocationIntelligence intelligence) => new(
        intelligence.LocationId,
        intelligence.ClimateSummary,
        intelligence.BestTimesToVisit,
        intelligence.TravelTips,
        intelligence.VisaNotes,
        intelligence.Confidence.ToString(),
        intelligence.GeneratedAt);
}
