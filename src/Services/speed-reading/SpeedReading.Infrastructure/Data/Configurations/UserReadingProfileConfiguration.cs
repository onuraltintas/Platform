using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using SpeedReading.Domain.ValueObjects;
using System.Text.Json;

namespace SpeedReading.Infrastructure.Data.Configurations;

public class UserReadingProfileConfiguration : IEntityTypeConfiguration<UserReadingProfile>
{
    public void Configure(EntityTypeBuilder<UserReadingProfile> builder)
    {
        builder.ToTable("UserReadingProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.HasIndex(x => x.UserId)
            .IsUnique();

        // Demographics Value Object
        builder.OwnsOne(x => x.Demographics, demo =>
        {
            demo.Property(d => d.DateOfBirth)
                .HasColumnName("DateOfBirth")
                .HasColumnType("DATE");

            demo.Property(d => d.Gender)
                .HasColumnName("Gender")
                .HasConversion<int>()
                .IsRequired();

            demo.Property(d => d.City)
                .HasColumnName("City")
                .HasMaxLength(100);

            demo.Property(d => d.District)
                .HasColumnName("District")
                .HasMaxLength(100);

            demo.Property(d => d.GradeLevel)
                .HasColumnName("GradeLevel");

            demo.Property(d => d.SchoolType)
                .HasColumnName("SchoolType")
                .HasConversion<int>()
                .IsRequired();
        });

        // ReadingPreferences Value Object
        builder.OwnsOne(x => x.Preferences, pref =>
        {
            pref.Property(p => p.TargetReadingSpeed)
                .HasColumnName("TargetReadingSpeed");

            pref.Property(p => p.PreferredTextTypes)
                .HasColumnName("PreferredTextTypes")
                .HasMaxLength(500)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null));

            pref.Property(p => p.ReadingGoals)
                .HasColumnName("ReadingGoals")
                .HasMaxLength(1000);

            pref.Property(p => p.PreferredLanguage)
                .HasColumnName("PreferredLanguage")
                .HasMaxLength(10)
                .HasDefaultValue("tr-TR")
                .IsRequired();

            pref.Property(p => p.FontSize)
                .HasColumnName("FontSize")
                .HasDefaultValue(14)
                .IsRequired();

            pref.Property(p => p.LineSpacing)
                .HasColumnName("LineSpacing")
                .HasColumnType("FLOAT")
                .HasDefaultValue(1.5f)
                .IsRequired();

            pref.Property(p => p.BackgroundColor)
                .HasColumnName("BackgroundColor")
                .HasMaxLength(7)
                .HasDefaultValue("#FFFFFF")
                .IsRequired();

            pref.Property(p => p.TextColor)
                .HasColumnName("TextColor")
                .HasMaxLength(7)
                .HasDefaultValue("#000000")
                .IsRequired();
        });

        builder.Property(x => x.CurrentLevel)
            .HasConversion<int>()
            .HasDefaultValueSql("1")
            .IsRequired();

        builder.Property(x => x.TotalReadingTime)
            .HasColumnType("TIME")
            .HasDefaultValue(new TimeSpan(0, 0, 0))
            .IsRequired();

        builder.Property(x => x.TotalWordsRead)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.AverageReadingSpeed)
            .HasColumnType("FLOAT")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.AverageComprehension)
            .HasColumnType("FLOAT")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("DATETIME2")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

    }
}