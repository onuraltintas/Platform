using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class UserGroupConfiguration : IEntityTypeConfiguration<UserGroup>
{
    public void Configure(EntityTypeBuilder<UserGroup> builder)
    {
        // Table
        builder.ToTable("UserGroups");
        
        // Composite Primary Key
        builder.HasKey(ug => new { ug.UserId, ug.GroupId });
        
        // Properties
        builder.Property(ug => ug.Role).IsRequired();
        builder.Property(ug => ug.InvitedBy).HasMaxLength(450);
        builder.Property(ug => ug.SuspensionReason).HasMaxLength(500);
        
        // Relationships
        builder.HasOne(ug => ug.User)
            .WithMany(u => u.UserGroups)
            .HasForeignKey(ug => ug.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(ug => ug.Group)
            .WithMany(g => g.UserGroups)
            .HasForeignKey(ug => ug.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Indexes
        builder.HasIndex(ug => ug.GroupId);
        builder.HasIndex(ug => ug.JoinedAt);
        builder.HasIndex(ug => ug.IsActive);
    }
}