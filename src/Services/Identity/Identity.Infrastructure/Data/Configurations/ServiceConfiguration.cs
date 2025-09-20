using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Identity.Infrastructure.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        // Table
        builder.ToTable("Services");
        
        // Primary Key
        builder.HasKey(s => s.Id);
        
        // Properties
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.Name).HasMaxLength(200).IsRequired();
        builder.Property(s => s.DisplayName).HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(1000);
        builder.Property(s => s.Endpoint).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Version).HasMaxLength(50).IsRequired();
        builder.Property(s => s.Type).IsRequired();
        builder.Property(s => s.HealthCheckEndpoint).HasMaxLength(500);
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.StatusMessage).HasMaxLength(500);
        builder.Property(s => s.ApiKey).HasMaxLength(500);
        builder.Property(s => s.RegisteredBy).HasMaxLength(450);
        
        // JSON column for metadata
        builder.Property(s => s.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>())
            .HasColumnType("text");
        
        // Indexes
        builder.HasIndex(s => s.Name).IsUnique();
        builder.HasIndex(s => s.Type);
        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => s.IsActive);
    }
}