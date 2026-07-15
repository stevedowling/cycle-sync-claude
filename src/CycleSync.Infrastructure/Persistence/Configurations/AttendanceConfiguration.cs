using CycleSync.Domain.OffCycles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CycleSync.Infrastructure.Persistence.Configurations;

public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        // Composite (OffCycleId, UserId) key — one status per user per off-cycle.
        builder.ToTable("Attendances");
        builder.HasKey(a => new { a.OffCycleId, a.UserId });

        builder.Property(a => a.Status).HasConversion<byte>().IsRequired();
        builder.Property(a => a.UpdatedAt).IsRequired();
    }
}
