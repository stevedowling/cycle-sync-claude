using CycleSync.Domain.Locations;

namespace CycleSync.Api.Features.Locations;

public sealed record CoordinatesDto(double Latitude, double Longitude);

public sealed record LocationSearchResultDto(string Name, string Country, CoordinatesDto Coordinates, string? AzureMapsId);

public sealed record PersistLocationRequest(string Name, string Country, double Latitude, double Longitude, string? AzureMapsId);

public sealed record LocationResponse(Guid Id, string Name, string Country, CoordinatesDto Coordinates, DateTimeOffset CreatedAt);

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
    public static LocationResponse ToResponse(this Location location) => new(
        location.Id,
        location.Name,
        location.Country,
        new CoordinatesDto(location.Coordinates.Latitude, location.Coordinates.Longitude),
        location.CreatedAt);

    public static LocationIntelligenceResponse ToResponse(this LocationIntelligence intelligence) => new(
        intelligence.LocationId,
        intelligence.ClimateSummary,
        intelligence.BestTimesToVisit,
        intelligence.TravelTips,
        intelligence.VisaNotes,
        intelligence.Confidence.ToString(),
        intelligence.GeneratedAt);
}
