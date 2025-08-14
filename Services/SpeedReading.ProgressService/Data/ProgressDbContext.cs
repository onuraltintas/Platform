using Microsoft.EntityFrameworkCore;

namespace SpeedReading.ProgressService.Data;

public class ProgressDbContext : DbContext
{
    public ProgressDbContext(DbContextOptions<ProgressDbContext> options) : base(options) { }

    public DbSet<UserReadingSession> Sessions => Set<UserReadingSession>();
    public DbSet<UserExerciseAttempt> Attempts => Set<UserExerciseAttempt>();
    public DbSet<QuestionResponse> Responses => Set<QuestionResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserReadingSession>().HasKey(x => x.SessionId);
        modelBuilder.Entity<UserExerciseAttempt>().HasKey(x => x.AttemptId);
        modelBuilder.Entity<QuestionResponse>().HasKey(x => x.ResponseId);

        modelBuilder.Entity<UserReadingSession>()
            .Property(x => x.ComprehensionScore)
            .HasPrecision(5, 2);

        modelBuilder.Entity<UserExerciseAttempt>()
            .Property(x => x.Score)
            .HasPrecision(5, 2);

        modelBuilder.Entity<QuestionResponse>()
            .HasOne<UserExerciseAttempt>()
            .WithMany()
            .HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserReadingSession
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid? TextId { get; set; }
    public DateTime SessionStartDate { get; set; }
    public DateTime? SessionEndDate { get; set; }
    public int? DurationSeconds { get; set; }
    public int? WPM { get; set; }
    public decimal? ComprehensionScore { get; set; }
    public string? EyeTrackingMetricsJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UserExerciseAttempt
{
    public Guid AttemptId { get; set; }
    public Guid UserId { get; set; }
    public Guid ExerciseId { get; set; }
    public DateTime AttemptDate { get; set; }
    public int DurationSeconds { get; set; }
    public decimal? Score { get; set; }
    public int? WPM { get; set; }
    public string? EyeTrackingMetricsJson { get; set; }
    public string? Feedback { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class QuestionResponse
{
    public Guid ResponseId { get; set; }
    public Guid AttemptId { get; set; }
    public Guid QuestionId { get; set; }
    public string GivenAnswer { get; set; } = string.Empty;
    public bool? IsCorrect { get; set; }
    public int? ResponseTimeMs { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

