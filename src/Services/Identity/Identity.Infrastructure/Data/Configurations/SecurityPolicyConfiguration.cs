using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class SecurityPolicyConfiguration : IEntityTypeConfiguration<SecurityPolicy>
{
    public void Configure(EntityTypeBuilder<SecurityPolicy> builder)
    {
        builder.ToTable("SecurityPolicies");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.PolicyType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Rules)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(p => p.Conditions)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(p => p.MinimumTrustScore)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(50.0m);

        builder.Property(p => p.IsActive)
            .HasDefaultValue(true);

        builder.Property(p => p.IsEnforced)
            .HasDefaultValue(true);

        builder.Property(p => p.Priority)
            .HasDefaultValue(100);

        builder.Property(p => p.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(450);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(450);

        // Indexes
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("IX_SecurityPolicies_Name");

        builder.HasIndex(p => p.Category)
            .HasDatabaseName("IX_SecurityPolicies_Category");

        builder.HasIndex(p => p.PolicyType)
            .HasDatabaseName("IX_SecurityPolicies_PolicyType");

        builder.HasIndex(p => p.IsActive)
            .HasDatabaseName("IX_SecurityPolicies_IsActive");

        builder.HasIndex(p => p.Priority)
            .HasDatabaseName("IX_SecurityPolicies_Priority");

        builder.HasIndex(p => p.GroupId)
            .HasDatabaseName("IX_SecurityPolicies_GroupId");

        builder.HasIndex(p => p.CreatedAt)
            .HasDatabaseName("IX_SecurityPolicies_CreatedAt");

        // Relationships
        builder.HasOne(p => p.Group)
            .WithMany()
            .HasForeignKey(p => p.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Violations)
            .WithOne(v => v.SecurityPolicy)
            .HasForeignKey(v => v.SecurityPolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class PolicyViolationConfiguration : IEntityTypeConfiguration<PolicyViolation>
{
    public void Configure(EntityTypeBuilder<PolicyViolation> builder)
    {
        builder.ToTable("PolicyViolations");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.SecurityPolicyId)
            .IsRequired();

        builder.Property(v => v.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(v => v.ViolationType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(v => v.Severity)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(v => v.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(v => v.ViolationData)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(v => v.Status)
            .IsRequired()
            .HasMaxLength(50)
            .HasDefaultValue("Open");

        builder.Property(v => v.DetectedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(v => v.ResolvedBy)
            .HasMaxLength(450);

        builder.Property(v => v.AcknowledgedBy)
            .HasMaxLength(450);

        builder.Property(v => v.Resolution)
            .HasMaxLength(1000);

        builder.Property(v => v.DeviceId)
            .HasMaxLength(255);

        builder.Property(v => v.IpAddress)
            .HasMaxLength(45);

        builder.Property(v => v.UserAgent)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(v => v.SecurityPolicyId)
            .HasDatabaseName("IX_PolicyViolations_SecurityPolicyId");

        builder.HasIndex(v => v.UserId)
            .HasDatabaseName("IX_PolicyViolations_UserId");

        builder.HasIndex(v => v.ViolationType)
            .HasDatabaseName("IX_PolicyViolations_ViolationType");

        builder.HasIndex(v => v.Severity)
            .HasDatabaseName("IX_PolicyViolations_Severity");

        builder.HasIndex(v => v.DetectedAt)
            .HasDatabaseName("IX_PolicyViolations_DetectedAt");

        builder.HasIndex(v => v.Status)
            .HasDatabaseName("IX_PolicyViolations_Status");

        builder.HasIndex(v => new { v.UserId, v.DetectedAt })
            .HasDatabaseName("IX_PolicyViolations_User_Time");

        // Relationships
        builder.HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.SecurityPolicy)
            .WithMany(p => p.Violations)
            .HasForeignKey(v => v.SecurityPolicyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}