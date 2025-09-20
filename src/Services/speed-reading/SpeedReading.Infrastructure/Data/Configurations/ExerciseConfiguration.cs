using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpeedReading.Domain.Entities;
using SpeedReading.Domain.Enums;

namespace SpeedReading.Infrastructure.Data.Configurations;

public class ExerciseConfiguration : IEntityTypeConfiguration<Exercise>
{
    public void Configure(EntityTypeBuilder<Exercise> builder)
    {
        builder.ToTable("Exercises");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Instructions)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ExerciseStatus.Draft);

        builder.Property(e => e.TargetEducationLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.DifficultyLevel)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.TimeLimit)
            .IsRequired()
            .HasDefaultValue(30);

        builder.Property(e => e.MaxScore)
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(e => e.PassingScore)
            .IsRequired()
            .HasDefaultValue(60);

        builder.Property(e => e.IsTimeLimited)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.IsRandomized)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ShowResults)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.AllowRetry)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.MaxRetries)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(e => e.Tags)
            .HasMaxLength(500)
            .HasDefaultValue(string.Empty);

        builder.Property(e => e.Metadata)
            .HasMaxLength(4000)
            .HasDefaultValue(string.Empty);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.PublishedAt)
            .IsRequired(false);

        builder.Property(e => e.CreatedBy)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Foreign Key to ReadingText
        builder.Property(e => e.ReadingTextId)
            .IsRequired(false);

        builder.HasOne(e => e.ReadingText)
            .WithMany()
            .HasForeignKey(e => e.ReadingTextId)
            .OnDelete(DeleteBehavior.SetNull);

        // Relationships
        builder.HasMany(e => e.Questions)
            .WithOne(q => q.Exercise)
            .HasForeignKey(q => q.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attempts)
            .WithOne(a => a.Exercise)
            .HasForeignKey(a => a.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(e => e.Type);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.TargetEducationLevel);
        builder.HasIndex(e => e.DifficultyLevel);
        builder.HasIndex(e => e.CreatedBy);
        builder.HasIndex(e => e.ReadingTextId);
        builder.HasIndex(e => new { e.Status, e.IsActive });
        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.UpdatedAt);
    }
}

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Text)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(q => q.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(q => q.Points)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(q => q.OrderIndex)
            .IsRequired();

        builder.Property(q => q.IsRequired)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(q => q.HelpText)
            .HasMaxLength(500);

        builder.Property(q => q.ImageUrl)
            .HasMaxLength(500);

        builder.Property(q => q.Metadata)
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        // Foreign Key
        builder.Property(q => q.ExerciseId)
            .IsRequired();

        // Relationships
        builder.HasOne(q => q.Exercise)
            .WithMany(e => e.Questions)
            .HasForeignKey(q => q.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Options)
            .WithOne(o => o.Question)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(q => q.Answers)
            .WithOne(a => a.Question)
            .HasForeignKey(a => a.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(q => q.ExerciseId);
        builder.HasIndex(q => q.Type);
        builder.HasIndex(q => new { q.ExerciseId, q.OrderIndex });
    }
}

public class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> builder)
    {
        builder.ToTable("QuestionOptions");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Text)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(o => o.IsCorrect)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(o => o.OrderIndex)
            .IsRequired();

        builder.Property(o => o.Explanation)
            .HasMaxLength(1000);

        builder.Property(o => o.ImageUrl)
            .HasMaxLength(500);

        builder.Property(o => o.MatchingValue)
            .HasMaxLength(200);

        // Foreign Key
        builder.Property(o => o.QuestionId)
            .IsRequired();

        // Relationships
        builder.HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(o => o.QuestionId);
        builder.HasIndex(o => new { o.QuestionId, o.OrderIndex });
        builder.HasIndex(o => new { o.QuestionId, o.IsCorrect });
    }
}

public class ExerciseAttemptConfiguration : IEntityTypeConfiguration<ExerciseAttempt>
{
    public void Configure(EntityTypeBuilder<ExerciseAttempt> builder)
    {
        builder.ToTable("ExerciseAttempts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ExerciseId)
            .IsRequired();

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(AttemptStatus.InProgress);

        builder.Property(a => a.StartedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.CompletedAt)
            .IsRequired(false);

        builder.Property(a => a.ExpiresAt)
            .IsRequired(false);

        builder.Property(a => a.TotalScore)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.MaxPossibleScore)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.ScorePercentage)
            .IsRequired()
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(0);

        builder.Property(a => a.IsPassed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.QuestionsAnswered)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.TotalQuestions)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.Notes)
            .HasMaxLength(1000);

        builder.Property(a => a.Metadata)
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        // Relationships
        builder.HasOne(a => a.Exercise)
            .WithMany(e => e.Attempts)
            .HasForeignKey(a => a.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Answers)
            .WithOne(qa => qa.Attempt)
            .HasForeignKey(qa => qa.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.ExerciseId);
        builder.HasIndex(a => a.UserId);
        builder.HasIndex(a => a.Status);
        builder.HasIndex(a => new { a.UserId, a.ExerciseId });
        builder.HasIndex(a => new { a.Status, a.ExpiresAt });
        builder.HasIndex(a => a.StartedAt);
        builder.HasIndex(a => a.CompletedAt);
        builder.HasIndex(a => new { a.UserId, a.Status });
        builder.HasIndex(a => new { a.ExerciseId, a.Status });
    }
}

public class QuestionAnswerConfiguration : IEntityTypeConfiguration<QuestionAnswer>
{
    public void Configure(EntityTypeBuilder<QuestionAnswer> builder)
    {
        builder.ToTable("QuestionAnswers");

        builder.HasKey(qa => qa.Id);

        builder.Property(qa => qa.AttemptId)
            .IsRequired();

        builder.Property(qa => qa.QuestionId)
            .IsRequired();

        builder.Property(qa => qa.UserAnswer)
            .HasMaxLength(4000)
            .HasDefaultValue(string.Empty);

        builder.Property(qa => qa.IsCorrect)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(qa => qa.PointsEarned)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qa => qa.AnsweredAt)
            .IsRequired(false);

        builder.Property(qa => qa.Feedback)
            .HasMaxLength(1000);

        builder.Property(qa => qa.Metadata)
            .HasMaxLength(2000)
            .HasDefaultValue(string.Empty);

        // Relationships
        builder.HasOne(qa => qa.Attempt)
            .WithMany(a => a.Answers)
            .HasForeignKey(qa => qa.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(qa => qa.Question)
            .WithMany(q => q.Answers)
            .HasForeignKey(qa => qa.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(qa => qa.AttemptId);
        builder.HasIndex(qa => qa.QuestionId);
        builder.HasIndex(qa => new { qa.AttemptId, qa.QuestionId }).IsUnique();
        builder.HasIndex(qa => qa.IsCorrect);
        builder.HasIndex(qa => qa.AnsweredAt);
    }
}