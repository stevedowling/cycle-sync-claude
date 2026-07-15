namespace CycleSync.Domain.OffCycles;

/// <summary>
/// A user's commitment level for an off-cycle. Persisted as a tinyint in SQL Server. Transitions
/// between the five known values are unrestricted (a person's real plans change freely) — the only
/// rule is that the value must be one of these five. See <see cref="AttendanceStatusLabels"/> for
/// the user-facing display labels used on the wire.
/// </summary>
public enum AttendanceStatus : byte
{
    Interested = 0,
    CantMakeIt = 1,
    ProbablyComing = 2,
    DefinitelyComing = 3,
    Booked = 4,
}

/// <summary>
/// Maps <see cref="AttendanceStatus"/> to/from the human display labels ("Can't Make It", …) that
/// the API accepts and returns. Keeping the mapping here means both the domain and the API speak the
/// same vocabulary, and an unrecognised label is rejected in one place.
/// </summary>
public static class AttendanceStatusLabels
{
    private static readonly IReadOnlyDictionary<AttendanceStatus, string> ToLabels = new Dictionary<AttendanceStatus, string>
    {
        [AttendanceStatus.Interested] = "Interested",
        [AttendanceStatus.CantMakeIt] = "Can't Make It",
        [AttendanceStatus.ProbablyComing] = "Probably Coming",
        [AttendanceStatus.DefinitelyComing] = "Definitely Coming",
        [AttendanceStatus.Booked] = "Booked",
    };

    /// <summary>The display label for a status, e.g. <c>Can't Make It</c>.</summary>
    public static string ToLabel(this AttendanceStatus status) =>
        ToLabels.TryGetValue(status, out var label) ? label : status.ToString();

    /// <summary>
    /// Parses a display label ("Can't Make It") or the enum name ("CantMakeIt"), case-insensitively.
    /// Returns <see langword="false"/> for any unknown value so callers can reject it.
    /// </summary>
    public static bool TryParse(string? value, out AttendanceStatus status)
    {
        status = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        foreach (var (candidate, label) in ToLabels)
        {
            if (string.Equals(label, trimmed, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidate.ToString(), trimmed, StringComparison.OrdinalIgnoreCase))
            {
                status = candidate;
                return true;
            }
        }

        return false;
    }
}
