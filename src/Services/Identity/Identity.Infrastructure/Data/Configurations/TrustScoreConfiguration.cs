using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class TrustScoreConfiguration : IEntityTypeConfiguration<TrustScore>
{
    public void Configure(EntityTypeBuilder<TrustScore> builder)
    {
        builder.ToTable("TrustScores");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.UserId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(t => t.DeviceId)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(t => t.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(t => t.Score)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(t => t.DeviceScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(t => t.NetworkScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(t => t.BehaviorScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(t => t.AuthenticationScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(t => t.LocationScore)
            .HasColumnType("decimal(5,2)");

        builder.Property(t => t.Factors)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(t => t.Risks)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(t => t.Recommendations)
            .HasColumnType("jsonb")
            .HasDefaultValue("[]");

        builder.Property(t => t.CalculatedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(t => t.IsActive)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(t => t.UserId)
            .HasDatabaseName("IX_TrustScores_UserId");

        builder.HasIndex(t => t.DeviceId)
            .HasDatabaseName("IX_TrustScores_DeviceId");

        builder.HasIndex(t => t.IpAddress)
            .HasDatabaseName("IX_TrustScores_IpAddress");

        builder.HasIndex(t => t.CalculatedAt)
            .HasDatabaseName("IX_TrustScores_CalculatedAt");

        builder.HasIndex(t => new { t.UserId, t.DeviceId, t.IpAddress })
            .HasDatabaseName("IX_TrustScores_Composite");

        // Relationships
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.History)
            .WithOne(h => h.TrustScore)
            .HasForeignKey(h => h.TrustScoreId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TrustScoreHistoryConfiguration : IEntityTypeConfiguration<TrustScoreHistory>
{
    public void Configure(EntityTypeBuilder<TrustScoreHistory> builder)
    {
        builder.ToTable("TrustScoreHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.TrustScoreId)
            .IsRequired();

        builder.Property(h => h.PreviousScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(h => h.NewScore)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(h => h.ChangeReason)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(h => h.EventData)
            .HasColumnType("jsonb")
            .HasDefaultValue("{}");

        builder.Property(h => h.ChangedAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes
        builder.HasIndex(h => h.TrustScoreId)
            .HasDatabaseName("IX_TrustScoreHistory_TrustScoreId");

        builder.HasIndex(h => h.ChangedAt)
            .HasDatabaseName("IX_TrustScoreHistory_ChangedAt");
    }
}