namespace CycleSync.Domain.Locations;

/// <summary>
/// A geographic point. Immutable value object; equality is by latitude/longitude.
/// </summary>
public sealed record GeoCoordinates(double Latitude, double Longitude);
