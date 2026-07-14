using CycleSync.Api.Auth;
using CycleSync.Api.Features.Users;
using CycleSync.Domain.Users;
using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Api.Features.Profile;

public static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me").RequireAuthorization();

        group.MapGet("/profile", async (ICurrentUser current, CycleSyncDbContext db, CancellationToken cancellationToken) =>
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == current.Id, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user.ToProfile());
        });

        group.MapPut("/profile", async (
            UpdateProfileRequest request,
            ICurrentUser current,
            CycleSyncDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == current.Id, cancellationToken);
            if (user is null)
            {
                return Results.NotFound();
            }

            var home = string.IsNullOrWhiteSpace(request.HomeLocation) ? null : new GeoPlace(request.HomeLocation.Trim());
            user.UpdateProfile(home, request.PreferredCurrency, request.PreferredLanguage, clock);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(user.ToProfile());
        });

        group.MapGet("/passports", async (ICurrentUser current, CycleSyncDbContext db, CancellationToken cancellationToken) =>
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == current.Id, cancellationToken);
            return user is null
                ? Results.NotFound()
                : Results.Ok(user.Passports.Select(p => p.Country).ToArray());
        });

        group.MapPost("/passports", async (
            AddPassportRequest request,
            ICurrentUser current,
            CycleSyncDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Country))
            {
                return Results.Problem(statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid passport", detail: "country is required", type: "validation");
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == current.Id, cancellationToken);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.AddPassport(request.Country, clock);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(user.ToProfile());
        });

        group.MapDelete("/passports/{country}", async (
            string country,
            ICurrentUser current,
            CycleSyncDbContext db,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == current.Id, cancellationToken);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.RemovePassport(Uri.UnescapeDataString(country), clock);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(user.ToProfile());
        });

        return app;
    }
}
