namespace CycleSync.Domain.Interests;

/// <summary>
/// A join between a <see cref="Users.User"/> and a <see cref="Locations.Location"/> expressing that
/// the user is interested in meeting there. Unique per (user, location): marking interest is
/// idempotent (a second mark is a no-op) and removing it deletes the row. The number of rows for a
/// location is its interest count, which drives the consensus sort — no user has more weight than
/// another (the Equal Access principle).
/// </summary>
public sealed class Interest
{
    public Guid UserId { get; private set; }
    public Guid LocationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Interest()
    {
        // EF Core
    }

    public static Interest Mark(Guid userId, Guid locationId, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user id is required.", nameof(userId));
        }

        if (locationId == Guid.Empty)
        {
            throw new ArgumentException("A location id is required.", nameof(locationId));
        }

        return new Interest
        {
            UserId = userId,
            LocationId = locationId,
            CreatedAt = clock.GetUtcNow(),
        };
    }
}
