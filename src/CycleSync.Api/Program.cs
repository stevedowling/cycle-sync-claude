var builder = WebApplication.CreateBuilder(args);

// Aspire service defaults: OpenTelemetry, health checks, service discovery, resilience.
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// --- API surface (Phase 0 walking skeleton) ---
var api = app.MapGroup("/api");

// A trivial liveness probe the SPA shell calls to prove end-to-end connectivity.
api.MapGet("/ping", () => Results.Ok(new PingResponse("CycleSync", "ok")))
   .WithName("Ping");

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
