using CycleSync.Api.Auth;
using CycleSync.Api.Features.Cost;
using CycleSync.Api.Integrations.Cost;
using CycleSync.Domain;
using CycleSync.Domain.Locations;
using CycleSync.Domain.OffCycles;
using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Api.Features.OffCycles;

public static class OffCyclesEndpoints
{
    public static IEndpointRouteBuilder MapOffCyclesEndpoints(this IEndpointRouteBuilder app)
    {
        // Off-cycles are visible to and editable by every authenticated user (equal access).
        var group = app.MapGroup("/api/off-cycles").RequireAuthorization();

        // Create an off-cycle for a location and date range. The creator is seeded as "Interested".
        group.MapPost("/", async (
            CreateOffCycleRequest request,
            ICurrentUser current,
            CycleSyncDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Validation("name is required");
            }

            var location = await db.Locations.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == request.LocationId, cancellationToken);
            if (location is null)
            {
                return Results.Problem(statusCode: StatusCodes.Status404NotFound,
                    title: "Unknown location", detail: "the location does not exist", type: "not-found");
            }

            OffCycle offCycle;
            try
            {
                offCycle = OffCycle.Create(request.Name, request.LocationId, request.StartDate, request.EndDate, current.Id, clock);
            }
            catch (DomainValidationException ex)
            {
                return Validation(ex.Message);
            }

            db.OffCycles.Add(offCycle);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/off-cycles/{offCycle.Id}", offCycle.ToResponse(location.Name));
        });

        // All off-cycles — visible to everyone (privacy-friendly).
        group.MapGet("/", async (CycleSyncDbContext db, CancellationToken cancellationToken) =>
        {
            var offCycles = await db.OffCycles.AsNoTracking().OrderBy(o => o.StartDate).ToListAsync(cancellationToken);
            var names = await LocationNamesAsync(db, offCycles.Select(o => o.LocationId), cancellationToken);
            return Results.Ok(offCycles.Select(o => o.ToResponse(names.GetValueOrDefault(o.LocationId, string.Empty))).ToArray());
        });

        group.MapGet("/{id:guid}", async (Guid id, CycleSyncDbContext db, CancellationToken cancellationToken) =>
        {
            var offCycle = await db.OffCycles.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (offCycle is null)
            {
                return Results.NotFound();
            }

            var location = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == offCycle.LocationId, cancellationToken);
            return Results.Ok(offCycle.ToResponse(location?.Name ?? string.Empty));
        });

        // Edit name/dates. Re-validates the range and triggers cost recalculation (estimates are
        // recomputed on read, so simply persisting the new dates is enough).
        group.MapPut("/{id:guid}", async (
            Guid id,
            UpdateOffCycleRequest request,
            CycleSyncDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var offCycle = await db.OffCycles.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (offCycle is null)
            {
                return Results.NotFound();
            }

            try
            {
                offCycle.Reschedule(request.Name, request.StartDate, request.EndDate, clock);
            }
            catch (DomainValidationException ex)
            {
                return Validation(ex.Message);
            }

            await db.SaveChangesAsync(cancellationToken);

            var location = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == offCycle.LocationId, cancellationToken);
            return Results.Ok(offCycle.ToResponse(location?.Name ?? string.Empty));
        });

        // Set my attendance status (idempotent). Unknown status values are rejected.
        group.MapPut("/{id:guid}/attendance", async (
            Guid id,
            SetAttendanceRequest request,
            ICurrentUser current,
            CycleSyncDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            if (!AttendanceStatusLabels.TryParse(request.Status, out var status))
            {
                return Validation("unknown attendance status");
            }

            var offCycle = await db.OffCycles
                .Include(o => o.Attendances)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (offCycle is null)
            {
                return Results.NotFound();
            }

            offCycle.SetAttendance(current.Id, status, clock);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        // Full roster plus per-status counts, keyed by display label.
        group.MapGet("/{id:guid}/attendance", async (Guid id, CycleSyncDbContext db, CancellationToken cancellationToken) =>
        {
            var offCycle = await db.OffCycles.AsNoTracking()
                .Include(o => o.Attendances)
                .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (offCycle is null)
            {
                return Results.NotFound();
            }

            var userIds = offCycle.Attendances.Select(a => a.UserId).ToList();
            var displayNames = await db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName, cancellationToken);

            var roster = offCycle.Attendances
                .Select(a => new AttendanceRosterEntry(
                    a.UserId,
                    displayNames.GetValueOrDefault(a.UserId, string.Empty),
                    a.Status.ToLabel()))
                .ToArray();

            var counts = offCycle.Attendances
                .GroupBy(a => a.Status.ToLabel())
                .ToDictionary(g => g.Key, g => g.Count());

            return Results.Ok(new AttendanceSummaryResponse(offCycle.Id, counts, roster));
        });

        // Date-specific cost estimate for the current user (nights derived from the off-cycle dates).
        group.MapGet("/{id:guid}/cost-estimate", async (
            Guid id,
            ICurrentUser current,
            CycleSyncDbContext db,
            ICostEstimator estimator,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var offCycle = await db.OffCycles.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
            if (offCycle is null)
            {
                return Results.NotFound();
            }

            var location = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == offCycle.LocationId, cancellationToken);
            if (location is null)
            {
                return Results.NotFound();
            }

            var estimate = await CostEstimates.BuildAsync(current.Id, location, offCycle.Nights, db, estimator, clock, cancellationToken);
            return Results.Ok(estimate);
        });

        return app;
    }

    private static IResult Validation(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid off-cycle", detail: detail, type: "validation");

    private static async Task<Dictionary<Guid, string>> LocationNamesAsync(
        CycleSyncDbContext db,
        IEnumerable<Guid> locationIds,
        CancellationToken cancellationToken)
    {
        var ids = locationIds.Distinct().ToList();
        return await db.Locations.AsNoTracking()
            .Where(l => ids.Contains(l.Id))
            .ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);
    }
}
