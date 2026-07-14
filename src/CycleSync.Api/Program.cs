using CycleSync.Api.Auth;
using CycleSync.Api.Features.Auth;
using CycleSync.Api.Features.Locations;
using CycleSync.Api.Features.Profile;
using CycleSync.Api.Features.Users;
using CycleSync.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery, resilience.
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddCycleSyncDatabase(builder.Configuration);
builder.Services.AddCycleSyncAuth(builder.Configuration);

var app = builder.Build();

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
