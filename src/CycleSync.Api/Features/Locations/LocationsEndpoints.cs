using CycleSync.Api.Auth;
using CycleSync.Api.Features.Cost;
using CycleSync.Api.Http;
using CycleSync.Api.Integrations.Cost;
using CycleSync.Api.Integrations.Intelligence;
using CycleSync.Api.Integrations.Maps;
using CycleSync.Domain.Locations;
using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Api.Features.Locations;

public static class LocationsEndpoints
{
    /// <summary>Cached intelligence older than this is regenerated on the next request.</summary>
    private static readonly TimeSpan IntelligenceMaxAge = TimeSpan.FromDays(30);

    public static IEndpointRouteBuilder MapLocationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/locations").RequireAuthorization();

        // Search Azure Maps (proxied server-side so the provider key stays on the server).
        group.MapGet("/search", async (string? q, IMapsSearch maps, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Problems.Validation("a search query is required");
            }

            try
            {
                var results = await maps.SearchAsync(q, cancellationToken);
                var dtos = results
                    .Select(r => new LocationSearchResultDto(r.Name, r.Country, new CoordinatesDto(r.Latitude, r.Longitude), r.AzureMapsId))
                    .ToArray();
                return Results.Ok(dtos);
            }
            catch (Exception)
            {
                return Problems.Upstream("the maps provider could not be reached");
            }
        });

        // Persist a chosen search result. De-duplicated: returns the existing location (200) rather
        // than creating a duplicate (201). Locations are permanent — there is no delete counterpart.
        group.MapPost("/", async (
            PersistLocationRequest request,
            CycleSyncDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Country))
            {
                return Problems.Validation("name and country are required");
            }

            var existing = await FindExistingAsync(db, request, cancellationToken);
            if (existing is not null)
            {
                return Results.Ok(existing.ToResponse());
            }

            var location = Location.Create(
                request.Name,
                request.Country,
                new GeoCoordinates(request.Latitude, request.Longitude),
                request.AzureMapsId,
                clock);
            db.Locations.Add(location);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/locations/{location.Id}", location.ToResponse());
        });

        // All persisted locations — visible to every authenticated user (privacy-friendly). Each row
        // carries its team-wide interest count and whether the caller is interested. Pass
        // ?sort=interest for consensus order (most interest first, name as a stable tie-break).
        group.MapGet("/", async (
            string? sort,
            ICurrentUser current,
            CycleSyncDbContext db,
            CancellationToken cancellationToken) =>
        {
            var locations = await db.Locations.AsNoTracking().ToListAsync(cancellationToken);

            var counts = await db.Interests
                .GroupBy(i => i.LocationId)
                .Select(g => new { LocationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.LocationId, x => x.Count, cancellationToken);

            var mine = (await db.Interests
                .Where(i => i.UserId == current.Id)
                .Select(i => i.LocationId)
                .ToListAsync(cancellationToken)).ToHashSet();

            IEnumerable<Location> ordered = string.Equals(sort, "interest", StringComparison.OrdinalIgnoreCase)
                ? locations.OrderByDescending(l => counts.GetValueOrDefault(l.Id)).ThenBy(l => l.Name)
                : locations.OrderBy(l => l.Name);

            var dtos = ordered
                .Select(l => l.ToResponse(counts.GetValueOrDefault(l.Id), mine.Contains(l.Id)))
                .ToArray();
            return Results.Ok(dtos);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            ICurrentUser current,
            CycleSyncDbContext db,
            CancellationToken cancellationToken) =>
        {
            var location = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (location is null)
            {
                return Problems.NotFound();
            }

            var interestCount = await db.Interests.CountAsync(i => i.LocationId == id, cancellationToken);
            var isInterested = await db.Interests
                .AnyAsync(i => i.LocationId == id && i.UserId == current.Id, cancellationToken);

            return Results.Ok(location.ToResponse(interestCount, isInterested));
        });

        // AI-generated intelligence: served from cache and regenerated only when stale. The visa
        // guidance is tailored to the requesting user's passports.
        group.MapGet("/{id:guid}/intelligence", async (
            Guid id,
            ICurrentUser current,
            CycleSyncDbContext db,
            ILocationIntelligenceGenerator generator,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var location = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (location is null)
            {
                return Problems.NotFound();
            }

            var cached = await db.LocationIntelligence.FirstOrDefaultAsync(i => i.LocationId == id, cancellationToken);
            if (cached is not null && !cached.IsStale(clock, IntelligenceMaxAge))
            {
                return Results.Ok(cached.ToResponse());
            }

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == current.Id, cancellationToken);
            var passports = user?.Passports.Select(p => p.Country).ToArray() ?? [];

            var content = await generator.GenerateAsync(location, passports, cancellationToken);
            var intelligence = LocationIntelligence.Generate(
                location.Id,
                content.ClimateSummary,
                content.BestTimesToVisit,
                content.TravelTips,
                content.VisaNotes,
                content.Confidence,
                clock);

            if (cached is not null)
            {
                db.LocationIntelligence.Remove(cached);
            }

            db.LocationIntelligence.Add(intelligence);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(intelligence.ToResponse());
        });

        // Generic heuristic cost estimate for the current user (a nominal stay, not tied to dates).
        // The date-specific counterpart lives at /api/off-cycles/{id}/cost-estimate.
        group.MapGet("/{id:guid}/cost-estimate", async (
            Guid id,
            ICurrentUser current,
            CycleSyncDbContext db,
            ICostEstimator estimator,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var location = await db.Locations.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (location is null)
            {
                return Problems.NotFound();
            }

            var estimate = await CostEstimates.BuildAsync(
                current.Id, location, CostEstimates.GenericStayNights, db, estimator, clock, cancellationToken);
            return Results.Ok(estimate);
        });

        return app;
    }

    private static async Task<Location?> FindExistingAsync(
        CycleSyncDbContext db,
        PersistLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.AzureMapsId))
        {
            var byExternalId = await db.Locations
                .FirstOrDefaultAsync(l => l.AzureMapsId == request.AzureMapsId, cancellationToken);
            if (byExternalId is not null)
            {
                return byExternalId;
            }
        }

        var name = request.Name.Trim();
        var country = request.Country.Trim();
        return await db.Locations
            .FirstOrDefaultAsync(l => l.Name == name && l.Country == country, cancellationToken);
    }
}
