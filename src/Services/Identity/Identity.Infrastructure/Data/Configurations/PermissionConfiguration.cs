using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        // Table
        builder.ToTable("Permissions");
        
        // Primary Key
        builder.HasKey(p => p.Id);
        
        // Properties
        builder.Property(p => p.Id).ValueGeneratedOnAdd();
        builder.Property(p => p.Name).HasMaxLength(200).IsRequired();
        builder.Property(p => p.DisplayName).HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Resource).HasMaxLength(200).IsRequired();
        builder.Property(p => p.Action).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Type).IsRequired();
        
        // Relationships
        builder.HasOne(p => p.Service)
            .WithMany(s => s.Permissions)
            .HasForeignKey(p => p.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Indexes
        builder.HasIndex(p => new { p.ServiceId, p.Name }).IsUnique();
        builder.HasIndex(p => p.Resource);
        builder.HasIndex(p => p.Action);
        builder.HasIndex(p => p.Type);
        builder.HasIndex(p => p.IsActive);
    }
}