namespace CycleSync.Api.Features.Locations;

public static class LocationsEndpoints
{
    /// <summary>
    /// Phase 1 stub: a protected, empty locations list. It exists so authentication scenarios can
    /// prove that unauthenticated requests are rejected. Phase 2 implements search and persistence.
    /// </summary>
    public static IEndpointRouteBuilder MapLocationsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/locations").RequireAuthorization();

        group.MapGet("/", () => Results.Ok(Array.Empty<object>()));

        return app;
    }
}
