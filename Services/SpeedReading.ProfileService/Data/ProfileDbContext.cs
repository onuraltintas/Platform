using Microsoft.EntityFrameworkCore;

namespace SpeedReading.ProfileService.Data;

public class ProfileDbContext : DbContext
{
    public ProfileDbContext(DbContextOptions<ProfileDbContext> options) : base(options) { }

    public DbSet<ReadingProfile> Profiles => Set<ReadingProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ReadingProfile>().HasKey(x => x.Id);
    }
}

public class ReadingProfile
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CurrentReadingLevelId { get; set; }
    public string? Goals { get; set; }
    public string? LearningStyle { get; set; }
    public string? AccessibilityNeeds { get; set; }
    public string? PreferencesJson { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

