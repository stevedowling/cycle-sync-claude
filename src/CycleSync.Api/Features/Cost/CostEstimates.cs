using CycleSync.Api.Features.OffCycles;
using CycleSync.Api.Integrations.Cost;
using CycleSync.Domain.Locations;
using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Api.Features.Cost;

/// <summary>
/// Shared helper that turns "who is asking + where + for how long" into a cost-estimate response.
/// Used by both the generic location estimate and the date-specific off-cycle estimate so the two
/// stay identical apart from the number of nights. Estimates are recomputed per request (cheaper
/// than cache invalidation for a heuristic), so they always reflect the caller's home/currency and
/// the current dates.
/// </summary>
public static class CostEstimates
{
    /// <summary>Nominal stay length assumed for a location estimate that is not tied to specific dates.</summary>
    public const int GenericStayNights = 3;

    private const string DefaultCurrency = "USD";

    public static async Task<CostEstimateResponse> BuildAsync(
        Guid userId,
        Location destination,
        int nights,
        CycleSyncDbContext db,
        ICostEstimator estimator,
        TimeProvider clock,
        CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        var originName = user?.HomeLocation?.Name;
        var currency = string.IsNullOrWhiteSpace(user?.PreferredCurrency) ? DefaultCurrency : user!.PreferredCurrency!;

        var estimate = await estimator.EstimateAsync(originName, destination, nights, currency, cancellationToken);
        return estimate.ToResponse(clock.GetUtcNow());
    }
}
