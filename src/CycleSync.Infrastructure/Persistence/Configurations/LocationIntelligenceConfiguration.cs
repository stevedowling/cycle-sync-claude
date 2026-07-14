using CycleSync.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CycleSync.Infrastructure.Persistence.Configurations;

public sealed class LocationIntelligenceConfiguration : IEntityTypeConfiguration<LocationIntelligence>
{
    public void Configure(EntityTypeBuilder<LocationIntelligence> builder)
    {
        builder.ToTable("LocationIntelligence");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.LocationId).IsRequired();
        builder.Property(i => i.ClimateSummary);
        builder.Property(i => i.BestTimesToVisit);
        builder.Property(i => i.TravelTips);
        builder.Property(i => i.VisaNotes);
        builder.Property(i => i.Confidence).HasConversion<byte>().IsRequired();
        builder.Property(i => i.GeneratedAt).IsRequired();

        // One current row per location; FK to the permanent location (never cascades — locations
        // are never deleted).
        builder.HasIndex(i => i.LocationId).IsUnique();
        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(i => i.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
