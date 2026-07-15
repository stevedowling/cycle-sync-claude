using CycleSync.Api.Auth;
using CycleSync.Api.Features.Locations;
using CycleSync.Api.Http;
using CycleSync.Domain.Interests;
using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Api.Features.Interests;

public static class InterestsEndpoints
{
    public static IEndpointRouteBuilder MapInterestsEndpoints(this IEndpointRouteBuilder app)
    {
        var locations = app.MapGroup("/api/locations").RequireAuthorization();

        // Mark interest — idempotent: marking an already-interested location is a no-op that still
        // returns 204. The composite (UserId, LocationId) key guarantees at most one row per pair.
        locations.MapPut("/{id:guid}/interest", async (
            Guid id,
            ICurrentUser current,
            CycleSyncDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var locationExists = await db.Locations.AnyAsync(l => l.Id == id, cancellationToken);
            if (!locationExists)
            {
                return Problems.NotFound();
            }

            var alreadyInterested = await db.Interests
                .AnyAsync(i => i.UserId == current.Id && i.LocationId == id, cancellationToken);
            if (!alreadyInterested)
            {
                db.Interests.Add(Interest.Mark(current.Id, id, clock));
                await db.SaveChangesAsync(cancellationToken);
            }

            return Results.NoContent();
        });

        // Remove interest — idempotent: removing when not interested is a no-op that returns 204.
        locations.MapDelete("/{id:guid}/interest", async (
            Guid id,
            ICurrentUser current,
            CycleSyncDbContext db,
            CancellationToken cancellationToken) =>
        {
            var existing = await db.Interests
                .FirstOrDefaultAsync(i => i.UserId == current.Id && i.LocationId == id, cancellationToken);
            if (existing is not null)
            {
                db.Interests.Remove(existing);
                await db.SaveChangesAsync(cancellationToken);
            }

            return Results.NoContent();
        });

        // The caller's interested locations, in consensus order (each row carries interestCount so the
        // UI can show team-wide traction alongside the user's own picks).
        var me = app.MapGroup("/api/me").RequireAuthorization();
        me.MapGet("/interests", async (
            ICurrentUser current,
            CycleSyncDbContext db,
            CancellationToken cancellationToken) =>
        {
            var interestedIds = await db.Interests
                .Where(i => i.UserId == current.Id)
                .Select(i => i.LocationId)
                .ToListAsync(cancellationToken);

            var counts = await db.Interests
                .Where(i => interestedIds.Contains(i.LocationId))
                .GroupBy(i => i.LocationId)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.LocationId, x => x.Count, cancellationToken);

            var locations = await db.Locations
                .AsNoTracking()
                .Where(l => interestedIds.Contains(l.Id))
                .ToListAsync(cancellationToken);

            var dtos = locations
                .OrderByDescending(l => counts.GetValueOrDefault(l.Id))
                .ThenBy(l => l.Name)
                .Select(l => l.ToResponse(counts.GetValueOrDefault(l.Id), isInterested: true))
                .ToArray();
            return Results.Ok(dtos);
        });

        return app;
    }
}
