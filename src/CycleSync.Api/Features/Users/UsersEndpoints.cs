using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Api.Features.Users;

public static class UsersEndpoints
{
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        // Privacy-friendly: all authenticated users can see everyone's profile.
        var group = app.MapGroup("/api/users").RequireAuthorization();

        group.MapGet("/", async (CycleSyncDbContext db, CancellationToken cancellationToken) =>
        {
            var users = await db.Users.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync(cancellationToken);
            return Results.Ok(users.Select(u => u.ToProfile()).ToArray());
        });

        group.MapGet("/{id:guid}", async (Guid id, CycleSyncDbContext db, CancellationToken cancellationToken) =>
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
            return user is null ? Results.NotFound() : Results.Ok(user.ToProfile());
        });

        return app;
    }
}
