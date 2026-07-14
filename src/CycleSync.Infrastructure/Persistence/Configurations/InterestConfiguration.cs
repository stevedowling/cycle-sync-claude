using CycleSync.Domain.Interests;
using CycleSync.Domain.Locations;
using CycleSync.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CycleSync.Infrastructure.Persistence.Configurations;

public sealed class InterestConfiguration : IEntityTypeConfiguration<Interest>
{
    public void Configure(EntityTypeBuilder<Interest> builder)
    {
        builder.ToTable("Interests");

        // Composite key (UserId, LocationId) enforces idempotency at the database level: a user can
        // have at most one interest row per location.
        builder.HasKey(i => new { i.UserId, i.LocationId });

        builder.Property(i => i.CreatedAt).IsRequired();

        // Index for the count/consensus-sort queries that group by location.
        builder.HasIndex(i => i.LocationId);

        // FKs to the permanent principals — restrict, never cascade (locations and users are not
        // deleted, and multiple cascade paths are disallowed on SQL Server anyway).
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(i => i.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
