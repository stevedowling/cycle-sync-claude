namespace CycleSync.Domain.Users;

/// <summary>
/// A named place. In Phase 1 this is free text (e.g. "Auckland, New Zealand"); later phases
/// enrich it with country and coordinates from Azure Maps.
/// </summary>
public sealed record GeoPlace(string Name, string? Country = null, double? Latitude = null, double? Longitude = null);
