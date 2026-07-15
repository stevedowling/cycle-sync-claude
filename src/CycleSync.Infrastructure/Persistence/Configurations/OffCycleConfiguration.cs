using CycleSync.Domain.Locations;
using CycleSync.Domain.OffCycles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CycleSync.Infrastructure.Persistence.Configurations;

public sealed class OffCycleConfiguration : IEntityTypeConfiguration<OffCycle>
{
    public void Configure(EntityTypeBuilder<OffCycle> builder)
    {
        builder.ToTable("OffCycles", t =>
            t.HasCheckConstraint("CK_OffCycles_EndAfterStart", "[EndDate] >= [StartDate]"));
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.LocationId).IsRequired();
        builder.Property(o => o.StartDate).IsRequired();
        builder.Property(o => o.EndDate).IsRequired();
        builder.Property(o => o.CreatedByUserId).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();
        builder.Ignore(o => o.Nights);

        // FK to the permanent location — never cascades (locations are never deleted).
        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(o => o.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Attendances are part of the OffCycle aggregate but mapped as a normal child entity (not an
        // owned type): EF reliably tracks additions to a normal collection as inserts, whereas newly
        // added rows in an owned collection can be mis-detected as updates. See AttendanceConfiguration.
        builder.HasMany(o => o.Attendances)
            .WithOne()
            .HasForeignKey(a => a.OffCycleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Attendances).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
