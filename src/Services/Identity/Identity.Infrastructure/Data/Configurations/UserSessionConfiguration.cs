using Identity.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Data.Configurations;

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.HasKey(us => us.Id);

        builder.Property(us => us.SessionId)
            .IsRequired()
            .HasMaxLength(450);

        builder.Property(us => us.DeviceInfo)
            .HasMaxLength(1000);

        builder.Property(us => us.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(us => us.UserAgent)
            .HasMaxLength(2000);

        builder.Property(us => us.Location)
            .HasMaxLength(500);

        builder.Property(us => us.TrustFactors)
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(us => us.UserId)
            .HasDatabaseName("IX_UserSessions_UserId");

        builder.HasIndex(us => us.SessionId)
            .IsUnique()
            .HasDatabaseName("IX_UserSessions_SessionId");

        builder.HasIndex(us => us.IsActive)
            .HasDatabaseName("IX_UserSessions_IsActive");

        builder.HasIndex(us => us.StartTime)
            .HasDatabaseName("IX_UserSessions_StartTime");

        builder.HasIndex(us => us.LastActivity)
            .HasDatabaseName("IX_UserSessions_LastActivity");

        builder.HasIndex(us => new { us.UserId, us.IsActive })
            .HasDatabaseName("IX_UserSessions_User_Active");

        // Relationships
        builder.HasOne(us => us.User)
            .WithMany()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}