using CycleSync.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace CycleSync.Infrastructure.Persistence;

public sealed class CycleSyncDbContext(DbContextOptions<CycleSyncDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CycleSyncDbContext).Assembly);
    }
}
