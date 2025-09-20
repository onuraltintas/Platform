using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        // Table
        builder.ToTable("RolePermissions");
        
        // Primary Key (exclude GroupId to allow nullable + SetNull)
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });
        
        // Properties
        builder.Property(rp => rp.GrantedBy).HasMaxLength(450);
        builder.Property(rp => rp.Conditions).HasMaxLength(2000);
        
        // Relationships
        builder.HasOne(rp => rp.Role)
            .WithMany(r => r.RolePermissions)
            .HasForeignKey(rp => rp.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(rp => rp.Permission)
            .WithMany(p => p.RolePermissions)
            .HasForeignKey(rp => rp.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(rp => rp.Group)
            .WithMany()
            .HasForeignKey(rp => rp.GroupId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes
        builder.HasIndex(rp => rp.PermissionId);
        builder.HasIndex(rp => rp.GroupId);
        builder.HasIndex(rp => rp.GrantedAt);
        // Ensure uniqueness across Role-Permission-Group if business requires
        builder.HasIndex(rp => new { rp.RoleId, rp.PermissionId, rp.GroupId }).IsUnique();
    }
}