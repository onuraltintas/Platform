using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class GroupServiceConfiguration : IEntityTypeConfiguration<GroupService>
{
    public void Configure(EntityTypeBuilder<GroupService> builder)
    {
        // Table
        builder.ToTable("GroupServices");
        
        // Composite Primary Key
        builder.HasKey(gs => new { gs.GroupId, gs.ServiceId });
        
        // Properties
        builder.Property(gs => gs.GrantedBy).HasMaxLength(450);
        builder.Property(gs => gs.Notes).HasMaxLength(1000);
        
        // Relationships
        builder.HasOne(gs => gs.Group)
            .WithMany(g => g.GroupServices)
            .HasForeignKey(gs => gs.GroupId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(gs => gs.Service)
            .WithMany(s => s.GroupServices)
            .HasForeignKey(gs => gs.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(gs => gs.ServiceId);
        builder.HasIndex(gs => gs.GrantedAt);
        builder.HasIndex(gs => gs.ExpiresAt);
        builder.HasIndex(gs => gs.IsActive);
    }
}