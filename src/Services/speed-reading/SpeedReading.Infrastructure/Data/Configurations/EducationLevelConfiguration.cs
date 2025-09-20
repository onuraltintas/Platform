using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpeedReading.Domain.Entities;

namespace SpeedReading.Infrastructure.Data.Configurations;

public class EducationLevelConfiguration : IEntityTypeConfiguration<EducationLevel>
{
    public void Configure(EntityTypeBuilder<EducationLevel> builder)
    {
        builder.ToTable("EducationLevels");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.GradeNumber)
            .IsRequired();

        builder.Property(x => x.AgeRange)
            .HasMaxLength(20);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        builder.HasIndex(x => new { x.Category, x.GradeNumber })
            .IsUnique();
    }
}