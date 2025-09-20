using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;
using System.Text.Json;

namespace SpeedReading.Infrastructure.Data.Configurations;

public class ReadingTextConfiguration : IEntityTypeConfiguration<ReadingText>
{
    public void Configure(EntityTypeBuilder<ReadingText> builder)
    {
        builder.ToTable("ReadingTexts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(x => x.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnType("NVARCHAR(MAX)")
            .IsRequired();

        builder.Property(x => x.Summary)
            .HasMaxLength(1000);

        builder.Property(x => x.Category)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Difficulty)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.TargetEducationLevel)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValueSql("1")
            .IsRequired();

        builder.Property(x => x.Source)
            .HasConversion<int>()
            .HasDefaultValueSql("1")
            .IsRequired();

        builder.Property(x => x.MinGradeLevel);
        builder.Property(x => x.MaxGradeLevel);

        // TextMetadata Value Object
        builder.OwnsOne(x => x.Metadata, metadata =>
        {
            metadata.Property(m => m.Author)
                .HasColumnName("Author")
                .HasMaxLength(200)
                .IsRequired();

            metadata.Property(m => m.Publisher)
                .HasColumnName("Publisher")
                .HasMaxLength(200);

            metadata.Property(m => m.PublishDate)
                .HasColumnName("PublishDate")
                .HasColumnType("DATE");

            metadata.Property(m => m.Source)
                .HasColumnName("MetadataSource")
                .HasMaxLength(500);

            metadata.Property(m => m.Language)
                .HasColumnName("Language")
                .HasMaxLength(10)
                .HasDefaultValue("tr-TR");

            metadata.Property(m => m.Tags)
                .HasColumnName("Tags")
                .HasMaxLength(1000)
                .HasConversion(
                    v => v == null ? null : JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrEmpty(v) ? null : JsonSerializer.Deserialize<string[]>(v, (JsonSerializerOptions?)null));

            metadata.Property(m => m.Copyright)
                .HasColumnName("Copyright")
                .HasMaxLength(500);

            metadata.Property(m => m.Isbn)
                .HasColumnName("ISBN")
                .HasMaxLength(20);
        });

        // TextStatistics Value Object
        builder.OwnsOne(x => x.Statistics, stats =>
        {
            stats.Property(s => s.WordCount)
                .HasColumnName("WordCount")
                .IsRequired();

            stats.Property(s => s.CharacterCount)
                .HasColumnName("CharacterCount")
                .IsRequired();

            stats.Property(s => s.SentenceCount)
                .HasColumnName("SentenceCount")
                .IsRequired();

            stats.Property(s => s.ParagraphCount)
                .HasColumnName("ParagraphCount")
                .IsRequired();

            stats.Property(s => s.AverageWordLength)
                .HasColumnName("AverageWordLength")
                .HasColumnType("FLOAT")
                .IsRequired();

            stats.Property(s => s.AverageSentenceLength)
                .HasColumnName("AverageSentenceLength")
                .HasColumnType("FLOAT")
                .IsRequired();

            stats.Property(s => s.UniqueWordCount)
                .HasColumnName("UniqueWordCount")
                .IsRequired();

            stats.Property(s => s.LexicalDiversity)
                .HasColumnName("LexicalDiversity")
                .HasColumnType("FLOAT")
                .IsRequired();

            stats.Property(s => s.EstimatedReadingTime)
                .HasColumnName("EstimatedReadingTime")
                .IsRequired();
        });

        builder.Property(x => x.DifficultyScore)
            .HasColumnType("FLOAT")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.PopularityScore)
            .HasColumnType("FLOAT")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.ReadCount)
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.AverageComprehensionScore)
            .HasColumnType("FLOAT")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.AverageReadingSpeed)
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

        builder.Property(x => x.PublishedAt)
            .HasColumnType("DATETIME2");

        builder.Property(x => x.CreatedBy);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .IsRequired();

        // Indexes
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Difficulty);
        builder.HasIndex(x => x.TargetEducationLevel);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PopularityScore);
        builder.HasIndex(x => x.PublishedAt);
        builder.HasIndex(x => new { x.IsActive, x.Status });

    }
}