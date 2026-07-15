namespace CycleSync.Api.Http;

/// <summary>
/// Centralised RFC 7807 <c>ProblemDetails</c> factory for CycleSync's error taxonomy. Every failure
/// the API returns falls into one of these categories, and each carries a stable <c>type</c> slug so
/// the SPA (and any other client) can branch on the <em>kind</em> of error instead of parsing prose.
///
/// Keeping the factory in one place is the Phase 5 "error taxonomy" hardening: before this, some
/// endpoints returned a body-less <c>404</c> (<c>Results.NotFound()</c>) while others returned a
/// typed problem detail, and the maps failure used a one-off <c>upstream-unavailable</c> slug. Now
/// every error response is a typed problem detail drawn from the same five categories.
/// </summary>
public static class Problems
{
    /// <summary>The requested resource does not exist (or is not visible). Maps to <c>404</c>.</summary>
    public static IResult NotFound(string detail = "the requested resource was not found") =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound,
            title: "Not found", detail: detail, type: "not-found");

    /// <summary>The request was well-formed but violates a rule or invariant. Maps to <c>400</c>.</summary>
    public static IResult Validation(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid request", detail: detail, type: "validation");

    /// <summary>The caller is authenticated but not permitted. Maps to <c>403</c>.</summary>
    public static IResult Forbidden(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status403Forbidden,
            title: "Access denied", detail: detail, type: "forbidden");

    /// <summary>The caller could not be authenticated. Maps to <c>401</c>.</summary>
    public static IResult Unauthorized(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized", detail: detail, type: "unauthorized");

    /// <summary>An upstream dependency (maps, LLM) failed or was unreachable. Maps to <c>502</c>.</summary>
    public static IResult Upstream(string detail) =>
        Results.Problem(statusCode: StatusCodes.Status502BadGateway,
            title: "Upstream unavailable", detail: detail, type: "upstream");
}
