namespace CycleSync.Domain.Locations;

/// <summary>
/// How much trust to place in a generated figure (intelligence, cost estimate). Always surfaced
/// alongside the data per the "transparent costs" principle. Encoded as a tinyint in SQL Server.
/// </summary>
public enum Confidence : byte
{
    Low = 0,
    Medium = 1,
    High = 2,
}
