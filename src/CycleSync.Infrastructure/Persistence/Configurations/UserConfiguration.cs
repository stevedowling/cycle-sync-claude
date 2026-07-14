using CycleSync.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CycleSync.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).HasMaxLength(320).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.DisplayName).HasMaxLength(200).IsRequired();
        builder.Property(u => u.PreferredCurrency).HasMaxLength(3);
        builder.Property(u => u.PreferredLanguage).HasMaxLength(20);
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();

        // HomeLocation is an owned value object mapped to inline columns.
        builder.OwnsOne(u => u.HomeLocation, home =>
        {
            home.Property(h => h.Name).HasColumnName("HomeLocationName").HasMaxLength(200);
            home.Property(h => h.Country).HasColumnName("HomeCountry").HasMaxLength(100);
            home.Property(h => h.Latitude).HasColumnName("HomeLatitude");
            home.Property(h => h.Longitude).HasColumnName("HomeLongitude");
        });

        // Passports are part of the User aggregate, stored in their own table.
        builder.OwnsMany(u => u.Passports, passport =>
        {
            passport.ToTable("Passports");
            passport.WithOwner().HasForeignKey("UserId");
            passport.Property<int>("Id");
            passport.HasKey("Id");
            passport.Property(p => p.Country).HasColumnName("Country").HasMaxLength(100).IsRequired();
            passport.HasIndex("UserId", "Country").IsUnique();
        });

        builder.Navigation(u => u.Passports).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
