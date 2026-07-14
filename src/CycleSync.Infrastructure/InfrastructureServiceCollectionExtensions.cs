using CycleSync.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CycleSync.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public const string ConnectionStringName = "cyclesync";

    /// <summary>
    /// Registers the CycleSync database against SQL Server using the injected connection string.
    /// Tests replace this registration with an in-memory provider.
    /// </summary>
    public static IServiceCollection AddCycleSyncDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        services.AddDbContext<CycleSyncDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        return services;
    }
}
