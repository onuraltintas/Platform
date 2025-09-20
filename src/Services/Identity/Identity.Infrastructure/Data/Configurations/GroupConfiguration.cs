using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        // Table
        builder.ToTable("Groups");
        
        // Primary Key
        builder.HasKey(g => g.Id);
        
        // Properties
        builder.Property(g => g.Id).ValueGeneratedOnAdd();
        builder.Property(g => g.Name).HasMaxLength(200).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(1000);
        builder.Property(g => g.Type).IsRequired();
        builder.Property(g => g.LogoUrl).HasMaxLength(500);
        builder.Property(g => g.Website).HasMaxLength(500);
        builder.Property(g => g.ContactEmail).HasMaxLength(256);
        builder.Property(g => g.ContactPhone).HasMaxLength(50);
        builder.Property(g => g.SubscriptionPlan).HasMaxLength(100);
        builder.Property(g => g.CreatedBy).HasMaxLength(450);
        builder.Property(g => g.LastModifiedBy).HasMaxLength(450);
        builder.Property(g => g.DeletedBy).HasMaxLength(450);
        
        // Indexes
        builder.HasIndex(g => g.Name);
        builder.HasIndex(g => g.Type);
        builder.HasIndex(g => g.IsDeleted);
        builder.HasIndex(g => g.CreatedAt);
        
        // Query Filters
        builder.HasQueryFilter(g => !g.IsDeleted);
    }
}