using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.MigrationService;

/// <summary>
/// Applies any pending EF Core migrations on startup, then signals the host to stop.
/// Aspire waits for this to complete before starting the API (WaitForCompletion).
/// </summary>
public sealed class MigrationWorker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime lifetime,
    ILogger<MigrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CycleSyncDbContext>();

            logger.LogInformation("Applying database migrations...");
            await dbContext.Database.MigrateAsync(stoppingToken);
            logger.LogInformation("Database migrations applied.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed.");
            throw;
        }

        lifetime.StopApplication();
    }
}
