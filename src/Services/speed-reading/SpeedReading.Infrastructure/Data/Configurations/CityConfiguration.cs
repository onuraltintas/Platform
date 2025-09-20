using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpeedReading.Domain.Entities;

namespace SpeedReading.Infrastructure.Data.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PlateCode)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(x => x.Region)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasMany(x => x.Districts)
            .WithOne(x => x.City)
            .HasForeignKey(x => x.CityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PlateCode)
            .IsUnique();

        builder.HasIndex(x => x.Name);
    }
}