using Microsoft.EntityFrameworkCore;
using SpeedReading.Domain.Entities;

namespace SpeedReading.Infrastructure.Data;

public class SpeedReadingDbContext : DbContext
{
    public SpeedReadingDbContext(DbContextOptions<SpeedReadingDbContext> options) : base(options)
    {
    }

    public DbSet<UserReadingProfile> UserReadingProfiles => Set<UserReadingProfile>();
    public DbSet<City> Cities => Set<City>();
    public DbSet<District> Districts => Set<District>();
    public DbSet<EducationLevel> EducationLevels => Set<EducationLevel>();
    public DbSet<ReadingText> ReadingTexts => Set<ReadingText>();
    
    // Phase 4: Exercise and Comprehension Test entities
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<ExerciseAttempt> ExerciseAttempts => Set<ExerciseAttempt>();
    public DbSet<QuestionAnswer> QuestionAnswers => Set<QuestionAnswer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("speedreading");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SpeedReadingDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
}