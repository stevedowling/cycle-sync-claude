using CycleSync.Api.Auth;
using CycleSync.Api.Features.Auth;
using CycleSync.Api.Features.Interests;
using CycleSync.Api.Features.Locations;
using CycleSync.Api.Features.Profile;
using CycleSync.Api.Features.Users;
using CycleSync.Api.Integrations;
using CycleSync.Infrastructure;
using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery, resilience.
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddCycleSyncDatabase(builder.Configuration);
builder.Services.AddCycleSyncAuth(builder.Configuration);
builder.Services.AddCycleSyncIntegrations(builder.Configuration);

// Offline development & hermetic E2E: swap the external Google and Azure Maps dependencies for
// deterministic offline stand-ins so the app is fully clickable locally (and hermetic under
// Playwright) without a Google project or an Azure Maps key — the dev email sign-in accepts any
// allowed-domain address. Production always uses the real integrations; the database stays real
// SQL Server (Aspire locally, the CI service container in E2E).
if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("E2E"))
{
    builder.Services.RemoveAll<IGoogleTokenValidator>();
    builder.Services.AddSingleton<IGoogleTokenValidator, OfflineGoogleTokenValidator>();

    builder.Services.RemoveAll<CycleSync.Api.Integrations.Maps.IMapsSearch>();
    builder.Services.AddSingleton<CycleSync.Api.Integrations.Maps.IMapsSearch, CycleSync.Api.Integrations.Maps.OfflineMapsSearch>();
}

var app = builder.Build();

// In E2E mode the API owns schema creation (no separate migration service in the pipeline).
if (app.Environment.IsEnvironment("E2E"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CycleSyncDbContext>().Database.Migrate();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

// --- API surface ---
var api = app.MapGroup("/api");
api.MapGet("/ping", () => Results.Ok(new PingResponse("CycleSync", "ok")))
   .WithName("Ping");

app.MapAuthEndpoints();
app.MapProfileEndpoints();
app.MapUsersEndpoints();
app.MapLocationsEndpoints();
app.MapInterestsEndpoints();

// Maps /health and /alive (Development) from ServiceDefaults.
app.MapDefaultEndpoints();

// Serve the built SPA (wwwroot) in production/container scenarios, when present.
if (Directory.Exists(Path.Combine(app.Environment.ContentRootPath, "wwwroot")))
{
    app.UseFileServer();
}

app.Run();

internal sealed record PingResponse(string Service, string Status);

// Exposed so the acceptance test host (WebApplicationFactory<Program>) can boot the API.
public partial class Program;
