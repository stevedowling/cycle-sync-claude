using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CycleSync.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef migrations</c> build the model at design time without a live database.
/// The connection string is a placeholder — migrations only need the provider and model.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CycleSyncDbContext>
{
    public CycleSyncDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<CycleSyncDbContext>()
            .UseSqlServer("Server=(localdb);Database=cyclesync;Trusted_Connection=True;")
            .Options;

        return new CycleSyncDbContext(options);
    }
}
