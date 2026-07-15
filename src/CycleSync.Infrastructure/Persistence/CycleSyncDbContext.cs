using CycleSync.Domain.Locations;
using CycleSync.Domain.OffCycles;
using CycleSync.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Infrastructure.Persistence;

public sealed class CycleSyncDbContext(DbContextOptions<CycleSyncDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<LocationIntelligence> LocationIntelligence => Set<LocationIntelligence>();
    public DbSet<OffCycle> OffCycles => Set<OffCycle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CycleSyncDbContext).Assembly);
    }
}
