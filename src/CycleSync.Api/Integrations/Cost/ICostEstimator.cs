using CycleSync.Domain.Locations;

namespace CycleSync.Api.Integrations.Cost;

/// <summary>
/// A heuristic travel-cost estimate for one traveller to one destination. Amounts are already in the
/// requested currency. Carries <see cref="Confidence"/> so the API can disclose how much to trust it
/// (the "transparent costs" principle); the generation timestamp is stamped by the caller.
/// </summary>
public sealed record CostEstimate(
    string Currency,
    decimal Flights,
    decimal Accommodation,
    decimal DailyExpenses,
    int Nights,
    Confidence Confidence);

/// <summary>
/// Server-side abstraction over travel-cost estimation. The default implementation is a deterministic
/// heuristic (distance-based flights + per-night accommodation/expenses) so the feature is runnable
/// without a paid flight-pricing provider. Swap in a real provider behind this interface to raise
/// fidelity — the estimate is recomputed per request, so it always reflects the current dates.
/// </summary>
public interface ICostEstimator
{
    Task<CostEstimate> EstimateAsync(
        string? originName,
        Location destination,
        int nights,
        string currency,
        CancellationToken cancellationToken);
}
