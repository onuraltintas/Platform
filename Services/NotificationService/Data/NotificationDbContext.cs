using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Services.NotificationService.Models.Entities;

namespace EgitimPlatform.Services.NotificationService.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options)
    {
    }

    public DbSet<EmailNotification> EmailNotifications { get; set; }
    public DbSet<PushNotification> PushNotifications { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<PushNotificationDevice> PushNotificationDevices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // EmailNotification entity configuration
        modelBuilder.Entity<EmailNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ToEmail);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            
            entity.Property(e => e.Body).HasMaxLength(10000);
            entity.Property(e => e.TemplateData).HasMaxLength(2000);
            entity.Property(e => e.Metadata).HasMaxLength(1000);
        });

        // PushNotification entity configuration
        modelBuilder.Entity<PushNotification>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledAt);
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            
            entity.Property(e => e.Data).HasMaxLength(2000);
        });

        // UserDevice entity configuration
        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PushToken).IsUnique();
            entity.HasIndex(e => e.DeviceType);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.UserId, e.DeviceType });
            entity.HasIndex(e => new { e.UserId, e.IsActive });
            
            entity.Property(e => e.Metadata).HasMaxLength(1000);
        });

        // PushNotificationDevice entity configuration
        modelBuilder.Entity<PushNotificationDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PushNotificationId);
            entity.HasIndex(e => e.UserDeviceId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.PushNotificationId, e.UserDeviceId }).IsUnique();
            
            entity.HasOne(e => e.PushNotification)
                  .WithMany(p => p.PushNotificationDevices)
                  .HasForeignKey(e => e.PushNotificationId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasOne(e => e.UserDevice)
                  .WithMany()
                  .HasForeignKey(e => e.UserDeviceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Seed initial data if needed
        SeedData(modelBuilder);
    }
    
    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Add any initial seed data here if needed
        // For example, default device types, notification templates, etc.
    }
}