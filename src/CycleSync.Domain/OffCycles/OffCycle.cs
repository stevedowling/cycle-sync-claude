namespace CycleSync.Domain.OffCycles;

/// <summary>
/// A concrete planned meetup: a permanent <c>Location</c> plus a date range and a roster of
/// per-user <see cref="Attendance"/>. The aggregate root for attendance — attendances are only ever
/// created or changed through it. Visible to every authenticated user (privacy-friendly).
/// </summary>
public sealed class OffCycle
{
    private readonly List<Attendance> _attendances = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public Guid LocationId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyCollection<Attendance> Attendances => _attendances;

    /// <summary>Nights booked = the span between start and end. Drives date-specific cost recalculation.</summary>
    public int Nights => EndDate.DayNumber - StartDate.DayNumber;

    private OffCycle()
    {
        // EF Core
        Name = string.Empty;
    }

    public static OffCycle Create(
        string name,
        Guid locationId,
        DateOnly startDate,
        DateOnly endDate,
        Guid createdByUserId,
        TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        EnsureValidRange(startDate, endDate);

        var now = clock.GetUtcNow();
        var offCycle = new OffCycle
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            LocationId = locationId,
            StartDate = startDate,
            EndDate = endDate,
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        // The creator is enrolled as "Interested" the moment the off-cycle exists.
        offCycle._attendances.Add(new Attendance(offCycle.Id, createdByUserId, AttendanceStatus.Interested, now));
        return offCycle;
    }

    /// <summary>Edits the name and/or date range; re-validates the range and bumps <see cref="UpdatedAt"/>.</summary>
    public void Reschedule(string name, DateOnly startDate, DateOnly endDate, TimeProvider clock)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        EnsureValidRange(startDate, endDate);

        Name = name.Trim();
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = clock.GetUtcNow();
    }

    /// <summary>Sets a user's attendance status, inserting the roster row on first use. Idempotent.</summary>
    public void SetAttendance(Guid userId, AttendanceStatus status, TimeProvider clock)
    {
        var now = clock.GetUtcNow();
        var existing = _attendances.FirstOrDefault(a => a.UserId == userId);
        if (existing is null)
        {
            _attendances.Add(new Attendance(Id, userId, status, now));
        }
        else
        {
            existing.SetStatus(status, now);
        }
    }

    public AttendanceStatus? StatusFor(Guid userId) =>
        _attendances.FirstOrDefault(a => a.UserId == userId)?.Status;

    private static void EnsureValidRange(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new DomainValidationException("end date must not precede start date");
        }
    }
}
