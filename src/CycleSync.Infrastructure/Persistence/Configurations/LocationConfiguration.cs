using CycleSync.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CycleSync.Infrastructure.Persistence.Configurations;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Country).HasMaxLength(100).IsRequired();
        builder.Property(l => l.AzureMapsId).HasMaxLength(200);
        builder.Property(l => l.CreatedAt).IsRequired();

        // Coordinates are an owned value object mapped to inline columns.
        builder.OwnsOne(l => l.Coordinates, coords =>
        {
            coords.Property(c => c.Latitude).HasColumnName("Latitude").IsRequired();
            coords.Property(c => c.Longitude).HasColumnName("Longitude").IsRequired();
        });
        builder.Navigation(l => l.Coordinates).IsRequired();

        // De-duplication: prefer the external id, fall back to (name, country).
        builder.HasIndex(l => l.AzureMapsId).IsUnique().HasFilter("[AzureMapsId] IS NOT NULL");
        builder.HasIndex(l => new { l.Name, l.Country }).IsUnique();
    }
}
