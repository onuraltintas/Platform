using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class ApplicationRoleConfiguration : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        // Role hierarchy properties
        builder.Property(r => r.ParentRoleId)
            .HasMaxLength(450);

        builder.Property(r => r.HierarchyLevel)
            .HasDefaultValue(0);

        builder.Property(r => r.HierarchyPath)
            .HasMaxLength(4000);

        builder.Property(r => r.InheritPermissions)
            .HasDefaultValue(true);

        builder.Property(r => r.Priority)
            .HasDefaultValue(0);

        builder.Property(r => r.IsSystemRole)
            .HasDefaultValue(false);

        builder.Property(r => r.Description)
            .HasMaxLength(1000);

        builder.Property(r => r.IsActive)
            .HasDefaultValue(true);

        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(r => r.CreatedBy)
            .HasMaxLength(450);

        builder.Property(r => r.LastModifiedBy)
            .HasMaxLength(450);

        builder.Property(r => r.GroupId)
            .IsRequired(false);

        // Indexes for role hierarchy
        builder.HasIndex(r => r.ParentRoleId)
            .HasDatabaseName("IX_Roles_ParentRoleId");

        builder.HasIndex(r => r.HierarchyLevel)
            .HasDatabaseName("IX_Roles_HierarchyLevel");

        builder.HasIndex(r => r.Priority)
            .HasDatabaseName("IX_Roles_Priority");

        builder.HasIndex(r => r.IsActive)
            .HasDatabaseName("IX_Roles_IsActive");

        builder.HasIndex(r => r.GroupId)
            .HasDatabaseName("IX_Roles_GroupId");

        builder.HasIndex(r => r.CreatedAt)
            .HasDatabaseName("IX_Roles_CreatedAt");

        builder.HasIndex(r => new { r.GroupId, r.Name })
            .HasDatabaseName("IX_Roles_Group_Name");

        // Self-referencing relationship for role hierarchy
        builder.HasOne(r => r.ParentRole)
            .WithMany(r => r.ChildRoles)
            .HasForeignKey(r => r.ParentRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Group relationship
        builder.HasOne(r => r.Group)
            .WithMany()
            .HasForeignKey(r => r.GroupId)
            .OnDelete(DeleteBehavior.SetNull);

        // Check constraints to prevent circular references
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Roles_NoSelfReference",
            "\"Id\" != \"ParentRoleId\""));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Roles_ValidHierarchyLevel",
            "\"HierarchyLevel\" >= 0 AND \"HierarchyLevel\" <= 10"));
    }
}