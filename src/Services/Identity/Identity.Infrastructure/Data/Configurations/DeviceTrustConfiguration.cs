using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class DeviceTrustConfiguration : IEntityTypeConfiguration<DeviceTrust>
{
    public void Configure(EntityTypeBuilder<DeviceTrust> builder)
    {
        builder.ToTable("DeviceTrusts");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeviceId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(d => d.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(d => d.DeviceType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.OperatingSystem)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.Browser)
            .HasMaxLength(100);

        builder.Property(d => d.UserAgent)
            .HasMaxLength(500);

        builder.Property(d => d.DeviceName)
            .HasMaxLength(255);

        builder.Property(d => d.CertificateFingerprint)
            .HasMaxLength(128);

        builder.Property(d => d.DeviceFingerprint)
            .HasMaxLength(64);

        builder.Property(d => d.TrustScore)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(50.0m);

        builder.Property(d => d.CompliancePolicies)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(d => d.SecurityFeatures)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(d => d.AdditionalInfo)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(d => d.IsActive)
            .HasDefaultValue(true);

        builder.Property(d => d.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(d => d.DeviceId)
            .HasDatabaseName("IX_DeviceTrusts_DeviceId");

        builder.HasIndex(d => d.UserId)
            .HasDatabaseName("IX_DeviceTrusts_UserId");

        builder.HasIndex(d => new { d.UserId, d.DeviceId })
            .IsUnique()
            .HasDatabaseName("IX_DeviceTrusts_User_Device");

        builder.HasIndex(d => d.IsTrusted)
            .HasDatabaseName("IX_DeviceTrusts_IsTrusted");

        builder.HasIndex(d => d.LastSeen)
            .HasDatabaseName("IX_DeviceTrusts_LastSeen");

        // Relationships
        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Activities)
            .WithOne(a => a.DeviceTrust)
            .HasForeignKey(a => a.DeviceTrustId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class DeviceActivityConfiguration : IEntityTypeConfiguration<DeviceActivity>
{
    public void Configure(EntityTypeBuilder<DeviceActivity> builder)
    {
        builder.ToTable("DeviceActivities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.DeviceTrustId)
            .IsRequired();

        builder.Property(a => a.ActivityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(a => a.Location)
            .HasMaxLength(100);

        builder.Property(a => a.ActivityData)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(a => a.IsSuccessful)
            .HasDefaultValue(true);

        builder.Property(a => a.ErrorMessage)
            .HasMaxLength(255);

        builder.Property(a => a.OccurredAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(a => a.DeviceTrustId)
            .HasDatabaseName("IX_DeviceActivities_DeviceTrustId");

        builder.HasIndex(a => a.ActivityType)
            .HasDatabaseName("IX_DeviceActivities_ActivityType");

        builder.HasIndex(a => a.OccurredAt)
            .HasDatabaseName("IX_DeviceActivities_OccurredAt");

        builder.HasIndex(a => a.IpAddress)
            .HasDatabaseName("IX_DeviceActivities_IpAddress");
    }
}