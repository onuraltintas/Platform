using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Services.FeatureFlagService.Models.Entities;
using System.Text.Json;

namespace EgitimPlatform.Services.FeatureFlagService.Data;

public class FeatureFlagDbContext : DbContext
{
    public FeatureFlagDbContext(DbContextOptions<FeatureFlagDbContext> options) : base(options)
    {
    }

    public DbSet<FeatureFlag> FeatureFlags { get; set; }
    public DbSet<FeatureFlagAssignment> FeatureFlagAssignments { get; set; }
    public DbSet<FeatureFlagEvent> FeatureFlagEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // FeatureFlag configuration
        modelBuilder.Entity<FeatureFlag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => new { e.Environment, e.ApplicationId });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            // JSON column configurations
            entity.Property(e => e.TargetAudiences)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.Property(e => e.ExcludedAudiences)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

            entity.Property(e => e.Conditions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

            entity.Property(e => e.Variations)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());
        });

        // FeatureFlagAssignment configuration
        modelBuilder.Entity<FeatureFlagAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.FeatureFlagId }).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AssignedAt);

            entity.Property(e => e.Context)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

            entity.HasOne(e => e.FeatureFlag)
                .WithMany(f => f.Assignments)
                .HasForeignKey(e => e.FeatureFlagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FeatureFlagEvent configuration
        modelBuilder.Entity<FeatureFlagEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.FeatureFlagId, e.OccurredAt });
            entity.HasIndex(e => new { e.UserId, e.OccurredAt });
            entity.HasIndex(e => e.EventType);

            entity.Property(e => e.Properties)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

            entity.HasOne(e => e.FeatureFlag)
                .WithMany(f => f.Events)
                .HasForeignKey(e => e.FeatureFlagId)
                .OnDelete(DeleteBehavior.Cascade);
        });


        // Seed data
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Default feature flags
        modelBuilder.Entity<FeatureFlag>().HasData(
            new FeatureFlag
            {
                Id = "ff-default-1",
                Name = "New Dashboard",
                Key = "new_dashboard",
                Description = "Enable the new dashboard interface",
                Type = FeatureFlagType.Boolean,
                IsEnabled = true,
                Status = FeatureFlagStatus.Active,
                RolloutPercentage = 50,
                DefaultVariation = "enabled",
                Environment = "production",
                ApplicationId = "main-app",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            },
            new FeatureFlag
            {
                Id = "ff-default-2",
                Name = "Premium Features",
                Key = "premium_features",
                Description = "Enable premium features for subscribers",
                Type = FeatureFlagType.Boolean,
                IsEnabled = true,
                Status = FeatureFlagStatus.Active,
                RolloutPercentage = 100,
                DefaultVariation = "enabled",
                Environment = "production",
                ApplicationId = "main-app",
                CreatedBy = "system",
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}