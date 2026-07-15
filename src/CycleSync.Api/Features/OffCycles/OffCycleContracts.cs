using CycleSync.Api.Integrations.Cost;
using CycleSync.Domain.OffCycles;

namespace CycleSync.Api.Features.OffCycles;

public sealed record CreateOffCycleRequest(string Name, Guid LocationId, DateOnly StartDate, DateOnly EndDate);

public sealed record UpdateOffCycleRequest(string Name, DateOnly StartDate, DateOnly EndDate);

public sealed record SetAttendanceRequest(string Status);

public sealed record OffCycleResponse(
    Guid Id,
    string Name,
    Guid LocationId,
    string LocationName,
    DateOnly StartDate,
    DateOnly EndDate,
    int Nights,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AttendanceRosterEntry(Guid UserId, string DisplayName, string Status);

public sealed record AttendanceSummaryResponse(
    Guid OffCycleId,
    IReadOnlyDictionary<string, int> Counts,
    IReadOnlyList<AttendanceRosterEntry> Roster);

public sealed record CostEstimateResponse(
    string Currency,
    decimal Flights,
    decimal Accommodation,
    decimal DailyExpenses,
    int Nights,
    string Confidence,
    DateTimeOffset GeneratedAt);

public static class OffCycleMapping
{
    public static OffCycleResponse ToResponse(this OffCycle offCycle, string locationName) => new(
        offCycle.Id,
        offCycle.Name,
        offCycle.LocationId,
        locationName,
        offCycle.StartDate,
        offCycle.EndDate,
        offCycle.Nights,
        offCycle.CreatedByUserId,
        offCycle.CreatedAt,
        offCycle.UpdatedAt);

    public static CostEstimateResponse ToResponse(this CostEstimate estimate, DateTimeOffset generatedAt) => new(
        estimate.Currency,
        estimate.Flights,
        estimate.Accommodation,
        estimate.DailyExpenses,
        estimate.Nights,
        estimate.Confidence.ToString(),
        generatedAt);
}
