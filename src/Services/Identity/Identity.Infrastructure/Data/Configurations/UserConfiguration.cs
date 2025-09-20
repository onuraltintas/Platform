using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // Indexes
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.UserName).IsUnique();
        builder.HasIndex(u => u.IsDeleted);
        builder.HasIndex(u => u.CreatedAt);
        builder.HasIndex(u => u.DefaultGroupId);
        
        // Properties
        builder.Property(u => u.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(100).IsRequired();
        builder.Property(u => u.ProfilePictureUrl).HasMaxLength(500);
        builder.Property(u => u.Address).HasMaxLength(500);
        builder.Property(u => u.City).HasMaxLength(100);
        builder.Property(u => u.Country).HasMaxLength(100);
        builder.Property(u => u.PostalCode).HasMaxLength(20);
        builder.Property(u => u.About).HasMaxLength(1000);
        builder.Property(u => u.LastLoginIp).HasMaxLength(45);
        builder.Property(u => u.LastLoginDevice).HasMaxLength(500);
        builder.Property(u => u.TwoFactorSecretKey).HasMaxLength(500);
        builder.Property(u => u.DeletedBy).HasMaxLength(450);
        
        // Query Filters
        builder.HasQueryFilter(u => !u.IsDeleted);
    }
}