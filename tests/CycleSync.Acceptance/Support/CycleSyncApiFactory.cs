using CycleSync.Api.Auth;
using CycleSync.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CycleSync.Acceptance.Support;

/// <summary>
/// Boots the real CycleSync API in-process for API-level acceptance scenarios.
///
/// - Development environment: ServiceDefaults health endpoints and dev auth config are active.
/// - SQL Server is replaced with a private in-memory SQLite database (no container required).
/// - Google token validation is replaced with a deterministic offline fake.
/// </summary>
public sealed class CycleSyncApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private bool _schemaCreated;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Logging:LogLevel:Microsoft.EntityFrameworkCore.Database.Command"] = "Warning",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Swap SQL Server for a kept-open in-memory SQLite connection. Remove every
            // DbContext/options registration (including EF Core's additive options-configuration
            // entries) so only the SQLite provider remains.
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<CycleSyncDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(CycleSyncDbContext) ||
                (d.ServiceType.FullName?.Contains("IDbContextOptionsConfiguration", StringComparison.Ordinal) ?? false))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }

            _connection.Open();
            services.AddDbContext<CycleSyncDbContext>(options => options.UseSqlite(_connection));

            // Offline Google validation.
            services.RemoveAll<IGoogleTokenValidator>();
            services.AddSingleton<IGoogleTokenValidator, FakeGoogleTokenValidator>();
        });
    }

    /// <summary>Creates the SQLite schema once from the EF model.</summary>
    public void EnsureSchemaCreated()
    {
        if (_schemaCreated)
        {
            return;
        }

        using var scope = Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<CycleSyncDbContext>().Database.EnsureCreated();
        _schemaCreated = true;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
