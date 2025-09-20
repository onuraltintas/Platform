using Microsoft.EntityFrameworkCore;
using User.Core.Entities;

namespace User.Infrastructure.Data;

/// <summary>
/// Database context for User Service
/// </summary>
public class UserDbContext : DbContext
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options">DbContext options</param>
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// User profiles
    /// </summary>
    public DbSet<UserProfile> UserProfiles { get; set; } = null!;

    /// <summary>
    /// User preferences
    /// </summary>
    public DbSet<UserPreferences> UserPreferences { get; set; } = null!;

    /// <summary>
    /// User addresses
    /// </summary>
    public DbSet<UserAddress> UserAddresses { get; set; } = null!;

    /// <summary>
    /// User activities
    /// </summary>
    public DbSet<UserActivity> UserActivities { get; set; } = null!;

    /// <summary>
    /// User documents
    /// </summary>
    public DbSet<UserDocument> UserDocuments { get; set; } = null!;

    /// <summary>
    /// GDPR requests
    /// </summary>
    public DbSet<GdprRequest> GdprRequests { get; set; } = null!;

    /// <summary>
    /// Email verifications
    /// </summary>
    public DbSet<EmailVerification> EmailVerifications { get; set; } = null!;

    /// <summary>
    /// Configure entity relationships and constraints
    /// </summary>
    /// <param name="modelBuilder">Model builder</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UserProfile entity
        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);

            // Relationships
            entity.HasOne(e => e.Preferences)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey<UserPreferences>(e => e.UserId)
                  .HasPrincipalKey<UserProfile>(e => e.UserId);

            entity.HasMany(e => e.Addresses)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey(e => e.UserId)
                  .HasPrincipalKey(e => e.UserId);

            entity.HasMany(e => e.Activities)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey(e => e.UserId)
                  .HasPrincipalKey(e => e.UserId);

            entity.HasMany(e => e.Documents)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey(e => e.UserId)
                  .HasPrincipalKey(e => e.UserId);

            entity.HasMany(e => e.GdprRequests)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey(e => e.UserId)
                  .HasPrincipalKey(e => e.UserId);

            entity.HasMany(e => e.EmailVerifications)
                  .WithOne(e => e.UserProfile)
                  .HasForeignKey(e => e.UserId)
                  .HasPrincipalKey(e => e.UserId);
        });

        // Configure UserPreferences entity
        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ProfileVisibility).HasMaxLength(20).HasDefaultValue("public");
            entity.Property(e => e.Theme).HasMaxLength(20).HasDefaultValue("system");
            entity.Property(e => e.DateFormat).HasMaxLength(50).HasDefaultValue("MM/dd/yyyy");
            entity.Property(e => e.TimeFormat).HasMaxLength(10).HasDefaultValue("12h");
            entity.Property(e => e.CustomPreferences).HasColumnType("text");
        });

        // Configure UserAddress entity
        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.AddressType).HasMaxLength(50);
            entity.Property(e => e.AddressLine1).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AddressLine2).HasMaxLength(200);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);
        });

        // Configure UserActivity entity
        modelBuilder.Entity<UserActivity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.DeviceInfo).HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.Metadata).HasColumnType("text");
            entity.Property(e => e.SessionId).HasMaxLength(100);
        });

        // Configure UserDocument entity
        modelBuilder.Entity<UserDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Category });
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.StoredFileName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(10);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.BucketName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ObjectKey).IsRequired().HasMaxLength(500);
            entity.Property(e => e.PublicUrl).HasMaxLength(1000);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(e => e.Checksum).HasMaxLength(100);
            entity.Property(e => e.VirusScanStatus).HasMaxLength(50);
            entity.Property(e => e.VirusScanResult).HasMaxLength(200);
        });

        // Configure GdprRequest entity
        modelBuilder.Entity<GdprRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.RequestType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ProcessedBy).HasMaxLength(450);
            entity.Property(e => e.ResultData).HasColumnType("text");
            entity.Property(e => e.VerificationToken).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ProcessorNotes).HasMaxLength(2000);
        });

        // Configure EmailVerification entity
        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Email });
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.Property(e => e.RequestIpAddress).HasMaxLength(45);
            entity.Property(e => e.VerificationIpAddress).HasMaxLength(45);
            entity.Property(e => e.VerificationType).IsRequired().HasMaxLength(50);
        });
    }

    /// <summary>
    /// Save changes with audit trail
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of affected records</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Enterprise.Shared.Common.Entities.BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (Enterprise.Shared.Common.Entities.BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
                entity.UpdatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}