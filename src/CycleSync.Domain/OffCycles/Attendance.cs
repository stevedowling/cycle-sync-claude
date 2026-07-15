namespace CycleSync.Domain.OffCycles;

/// <summary>
/// A single user's attendance status for an off-cycle. Part of the <see cref="OffCycle"/> aggregate;
/// one row per (off-cycle, user). Created and mutated only through the parent aggregate.
/// </summary>
public sealed class Attendance
{
    public Guid OffCycleId { get; private set; }
    public Guid UserId { get; private set; }
    public AttendanceStatus Status { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private Attendance()
    {
        // EF Core
    }

    internal Attendance(Guid offCycleId, Guid userId, AttendanceStatus status, DateTimeOffset updatedAt)
    {
        OffCycleId = offCycleId;
        UserId = userId;
        Status = status;
        UpdatedAt = updatedAt;
    }

    internal void SetStatus(AttendanceStatus status, DateTimeOffset updatedAt)
    {
        Status = status;
        UpdatedAt = updatedAt;
    }
}
