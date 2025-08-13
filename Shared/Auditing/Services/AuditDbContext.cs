using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Shared.Auditing.Models;

namespace EgitimPlatform.Shared.Auditing.Services;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options)
    {
    }

    public DbSet<AuditEntry> AuditEntries { get; set; }
    public DbSet<UserAuditEntry> UserAuditEntries { get; set; }
    public DbSet<ApiAuditEntry> ApiAuditEntries { get; set; }
    public DbSet<PerformanceAuditEntry> PerformanceAuditEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure schema
        modelBuilder.HasDefaultSchema("audit");

        // Configure AuditEntry
        modelBuilder.Entity<AuditEntry>(entity =>
        {
            entity.ToTable("AuditEntries");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Action)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("IX_AuditEntries_Entity");
            
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_AuditEntries_Timestamp");
            
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_AuditEntries_UserId");

            entity.HasIndex(e => e.Action)
                .HasDatabaseName("IX_AuditEntries_Action");

            // Configure JSON columns for complex types
            entity.Property(e => e.Metadata)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );
        });

        // Configure UserAuditEntry
        modelBuilder.Entity<UserAuditEntry>(entity =>
        {
            entity.ToTable("UserAuditEntries");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Action)
                .HasConversion<string>()
                .HasMaxLength(50);

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_UserAuditEntries_UserId");
            
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_UserAuditEntries_Timestamp");
            
            entity.HasIndex(e => e.Action)
                .HasDatabaseName("IX_UserAuditEntries_Action");

            entity.HasIndex(e => e.SessionId)
                .HasDatabaseName("IX_UserAuditEntries_SessionId");

            // Configure JSON columns for complex types
            entity.Property(e => e.AdditionalData)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );
        });

        // Configure ApiAuditEntry
        modelBuilder.Entity<ApiAuditEntry>(entity =>
        {
            entity.ToTable("ApiAuditEntries");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Path)
                .HasDatabaseName("IX_ApiAuditEntries_Path");
            
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_ApiAuditEntries_Timestamp");
            
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_ApiAuditEntries_UserId");

            entity.HasIndex(e => e.StatusCode)
                .HasDatabaseName("IX_ApiAuditEntries_StatusCode");

            entity.HasIndex(e => e.Duration)
                .HasDatabaseName("IX_ApiAuditEntries_Duration");

            entity.HasIndex(e => e.CorrelationId)
                .HasDatabaseName("IX_ApiAuditEntries_CorrelationId");

            // Configure JSON columns for complex types
            entity.Property(e => e.RequestHeaders)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
                );

            entity.Property(e => e.ResponseHeaders)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
                );
        });

        // Configure PerformanceAuditEntry
        modelBuilder.Entity<PerformanceAuditEntry>(entity =>
        {
            entity.ToTable("PerformanceAuditEntries");
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.Operation)
                .HasDatabaseName("IX_PerformanceAuditEntries_Operation");
            
            entity.HasIndex(e => e.StartTime)
                .HasDatabaseName("IX_PerformanceAuditEntries_StartTime");
            
            entity.HasIndex(e => e.Duration)
                .HasDatabaseName("IX_PerformanceAuditEntries_Duration");
            
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_PerformanceAuditEntries_UserId");

            entity.HasIndex(e => e.IsSuccessful)
                .HasDatabaseName("IX_PerformanceAuditEntries_IsSuccessful");

            // Configure JSON columns for complex types
            entity.Property(e => e.Parameters)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );

            entity.Property(e => e.Results)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                );
        });
    }
}