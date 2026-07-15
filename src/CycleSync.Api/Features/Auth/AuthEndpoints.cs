using CycleSync.Api.Auth;
using CycleSync.Api.Features.Users;
using CycleSync.Api.Http;
using CycleSync.Domain.Users;
using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        // Completes a Google sign-in: validates the ID token, enforces the domain restriction,
        // provisions the user on first sign-in, and issues an application session token.
        group.MapPost("/google", async (
            SignInRequest request,
            IGoogleTokenValidator validator,
            WorkspaceAccessPolicy accessPolicy,
            CycleSyncDbContext db,
            ITokenService tokens,
            TimeProvider clock,
            CancellationToken cancellationToken) =>
        {
            GoogleIdentity identity;
            try
            {
                identity = await validator.ValidateAsync(request.IdToken, cancellationToken);
            }
            catch (Exception)
            {
                return Problems.Unauthorized("the Google token could not be validated");
            }

            if (!accessPolicy.IsEmailAllowed(identity.Email))
            {
                return Problems.Forbidden("domain not permitted");
            }

            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == identity.Email, cancellationToken);
            if (user is null)
            {
                user = User.Provision(identity.Email, identity.Name, clock);
                db.Users.Add(user);
                await db.SaveChangesAsync(cancellationToken);
            }

            var token = tokens.CreateSessionToken(user);
            return Results.Ok(new SignInResponse(token, user.ToSummary()));
        })
        .AllowAnonymous()
        .WithName("GoogleSignIn");

        // The current user's identity + profile.
        group.MapGet("/me", async (ICurrentUser current, CycleSyncDbContext db, CancellationToken cancellationToken) =>
        {
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == current.Id, cancellationToken);
            return user is null ? Problems.NotFound() : Results.Ok(user.ToProfile());
        })
        .RequireAuthorization()
        .WithName("Me");

        return app;
    }
}
