using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EventType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(a => a.UserName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        builder.Property(a => a.SessionId)
            .HasMaxLength(128);

        builder.Property(a => a.Metadata)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(a => a.OldValues)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(a => a.NewValues)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(a => a.Severity)
            .HasDefaultValue(0);

        builder.Property(a => a.Category)
            .HasDefaultValue(0);

        builder.Property(a => a.IsSecurityEvent)
            .HasDefaultValue(false);

        builder.Property(a => a.RiskLevel)
            .HasMaxLength(20);

        builder.Property(a => a.Timestamp)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(a => a.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Tags)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100);

        builder.Property(a => a.IsSuccessful)
            .HasDefaultValue(true);

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(1000);

        // Indexes
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditEvents_UserId");

        builder.HasIndex(a => a.EventType)
            .HasDatabaseName("IX_AuditEvents_EventType");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditEvents_Timestamp");

        builder.HasIndex(a => a.IsSecurityEvent)
            .HasDatabaseName("IX_AuditEvents_IsSecurityEvent");

        builder.HasIndex(a => a.Severity)
            .HasDatabaseName("IX_AuditEvents_Severity");

        builder.HasIndex(a => a.EntityType)
            .HasDatabaseName("IX_AuditEvents_EntityType");

        builder.HasIndex(a => a.EntityId)
            .HasDatabaseName("IX_AuditEvents_EntityId");

        builder.HasIndex(a => a.CorrelationId)
            .HasDatabaseName("IX_AuditEvents_CorrelationId");

        builder.HasIndex(a => a.GroupId)
            .HasDatabaseName("IX_AuditEvents_GroupId");

        builder.HasIndex(a => new { a.EntityType, a.EntityId })
            .HasDatabaseName("IX_AuditEvents_Entity_Composite");

        builder.HasIndex(a => new { a.UserId, a.Timestamp })
            .HasDatabaseName("IX_AuditEvents_User_Time");

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Group)
            .WithMany()
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(a => a.Attachments)
            .WithOne(att => att.AuditEvent)
            .HasForeignKey(att => att.AuditEventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AuditEventAttachmentConfiguration : IEntityTypeConfiguration<AuditEventAttachment>
{
    public void Configure(EntityTypeBuilder<AuditEventAttachment> builder)
    {
        builder.ToTable("AuditEventAttachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AuditEventId)
            .IsRequired();

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Size)
            .IsRequired();

        builder.Property(a => a.FilePath)
            .HasMaxLength(1000);

        builder.Property(a => a.Metadata)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(a => a.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(a => a.AuditEventId)
            .HasDatabaseName("IX_AuditEventAttachments_AuditEventId");

        builder.HasIndex(a => a.CreatedAt)
            .HasDatabaseName("IX_AuditEventAttachments_CreatedAt");
    }
}