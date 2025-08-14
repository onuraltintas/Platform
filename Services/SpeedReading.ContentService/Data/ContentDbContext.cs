using Microsoft.EntityFrameworkCore;

namespace SpeedReading.ContentService.Data;

public class ContentDbContext : DbContext
{
    public ContentDbContext(DbContextOptions<ContentDbContext> options) : base(options) { }

    public DbSet<TextEntity> Texts => Set<TextEntity>();
    public DbSet<ExerciseTypeEntity> ExerciseTypes => Set<ExerciseTypeEntity>();
    public DbSet<ExerciseEntity> Exercises => Set<ExerciseEntity>();
    public DbSet<ComprehensionQuestionEntity> Questions => Set<ComprehensionQuestionEntity>();
    public DbSet<ReadingLevelEntity> ReadingLevels => Set<ReadingLevelEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TextEntity>().HasKey(x => x.TextId);
        modelBuilder.Entity<ExerciseTypeEntity>().HasKey(x => x.ExerciseTypeId);
        modelBuilder.Entity<ExerciseEntity>().HasKey(x => x.ExerciseId);
        modelBuilder.Entity<ComprehensionQuestionEntity>().HasKey(x => x.QuestionId);
        modelBuilder.Entity<ReadingLevelEntity>().HasKey(x => x.LevelId);
        modelBuilder.Entity<ReadingLevelEntity>()
            .Property(x => x.TargetComprehension)
            .HasPrecision(5, 2);

			modelBuilder.Entity<ExerciseEntity>()
            .HasOne<ExerciseTypeEntity>()
            .WithMany()
            .HasForeignKey(x => x.ExerciseTypeId)
            .OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<TextEntity>()
				.HasOne<ReadingLevelEntity>()
				.WithMany()
				.HasForeignKey(x => x.LevelId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<ExerciseEntity>()
				.HasOne<ReadingLevelEntity>()
				.WithMany()
				.HasForeignKey(x => x.LevelId)
				.OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComprehensionQuestionEntity>()
            .HasOne<TextEntity>()
            .WithMany()
            .HasForeignKey(x => x.TextId)
            .OnDelete(DeleteBehavior.Cascade);

			modelBuilder.Entity<ComprehensionQuestionEntity>()
				.HasOne<ReadingLevelEntity>()
				.WithMany()
				.HasForeignKey(x => x.LevelId)
				.OnDelete(DeleteBehavior.Restrict);
    }
}

public class TextEntity
{
    public Guid TextId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
		public string DifficultyLevel { get; set; } = string.Empty;
		public Guid? LevelId { get; set; }
    public int? WordCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
}

public class ExerciseTypeEntity
{
    public Guid ExerciseTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ExerciseEntity
{
    public Guid ExerciseId { get; set; }
    public Guid ExerciseTypeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
		public string DifficultyLevel { get; set; } = string.Empty; // Temel/Orta/İleri/Uzman
		public Guid? LevelId { get; set; }
    public string? ContentJson { get; set; }
    public int? DurationMinutes { get; set; }
}

public class ComprehensionQuestionEntity
{
    public Guid QuestionId { get; set; }
    public Guid TextId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionType { get; set; } // MultipleChoice, TrueFalse, OpenEnded
    public string? CorrectAnswer { get; set; }
    public string? OptionsJson { get; set; }
		public Guid? LevelId { get; set; }
}

public class ReadingLevelEntity
{
    public Guid LevelId { get; set; }
    public string LevelName { get; set; } = string.Empty; // Başlangıç/Orta/İleri/Uzman
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public int? MinWPM { get; set; }
    public int? MaxWPM { get; set; }
    public decimal? TargetComprehension { get; set; }
}

