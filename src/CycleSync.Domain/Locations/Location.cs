namespace CycleSync.Domain.Locations;

/// <summary>
/// A permanent meetup destination. Locations are <b>never deleted</b> (the Location Permanence
/// principle) — there is no delete behaviour anywhere in the domain or API. Created by persisting
/// a chosen Azure Maps search result; de-duplicated on <see cref="AzureMapsId"/> or (name, country).
/// </summary>
public sealed class Location
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Country { get; private set; }
    public GeoCoordinates Coordinates { get; private set; }
    public string? AzureMapsId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Location()
    {
        // EF Core
        Name = string.Empty;
        Country = string.Empty;
        Coordinates = new GeoCoordinates(0, 0);
    }

    public static Location Create(string name, string country, GeoCoordinates coordinates, string? azureMapsId, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);
        ArgumentNullException.ThrowIfNull(coordinates);

        return new Location
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Country = country.Trim(),
            Coordinates = coordinates,
            AzureMapsId = string.IsNullOrWhiteSpace(azureMapsId) ? null : azureMapsId.Trim(),
            CreatedAt = clock.GetUtcNow(),
        };
    }
}
