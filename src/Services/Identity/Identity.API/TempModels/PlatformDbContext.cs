using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.TempModels;

public partial class PlatformDbContext : DbContext
{
    public PlatformDbContext()
    {
    }

    public PlatformDbContext(DbContextOptions<PlatformDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    public virtual DbSet<GdprRequest> GdprRequests { get; set; }

    public virtual DbSet<Group> Groups { get; set; }

    public virtual DbSet<GroupInvitation> GroupInvitations { get; set; }

    public virtual DbSet<GroupInvitationUsage> GroupInvitationUsages { get; set; }

    public virtual DbSet<GroupService> GroupServices { get; set; }

    public virtual DbSet<LoginAttempt> LoginAttempts { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RoleClaim> RoleClaims { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    public virtual DbSet<Service> Services { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserActivity> UserActivities { get; set; }

    public virtual DbSet<UserAddress> UserAddresses { get; set; }

    public virtual DbSet<UserClaim> UserClaims { get; set; }

    public virtual DbSet<UserConsent> UserConsents { get; set; }

    public virtual DbSet<UserDevice> UserDevices { get; set; }

    public virtual DbSet<UserDocument> UserDocuments { get; set; }

    public virtual DbSet<UserGroup> UserGroups { get; set; }

    public virtual DbSet<UserLogin> UserLogins { get; set; }

    public virtual DbSet<UserPreference> UserPreferences { get; set; }

    public virtual DbSet<UserProfile> UserProfiles { get; set; }

    public virtual DbSet<UserToken> UserTokens { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("IDENTITY_DB_CONNECTION") ?? "Host=localhost;Database=PlatformDB;Username=platform_user;Password=VForVan_40!");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasIndex(e => e.Token, "IX_EmailVerifications_Token").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.Email }, "IX_EmailVerifications_UserId_Email");

            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.RequestIpAddress).HasMaxLength(45);
            entity.Property(e => e.Token).HasMaxLength(255);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.VerificationIpAddress).HasMaxLength(45);
            entity.Property(e => e.VerificationType).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerifications)
                .HasPrincipalKey(p => p.UserId)
                .HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<GdprRequest>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Status }, "IX_GdprRequests_UserId_Status");

            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.ProcessedBy).HasMaxLength(450);
            entity.Property(e => e.ProcessorNotes).HasMaxLength(2000);
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.RequestType).HasMaxLength(50);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.VerificationToken).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.GdprRequests)
                .HasPrincipalKey(p => p.UserId)
                .HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt, "IX_Groups_CreatedAt");

            entity.HasIndex(e => e.IsDeleted, "IX_Groups_IsDeleted");

            entity.HasIndex(e => e.Name, "IX_Groups_Name");

            entity.HasIndex(e => e.Type, "IX_Groups_Type");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ContactEmail).HasMaxLength(256);
            entity.Property(e => e.ContactPhone).HasMaxLength(50);
            entity.Property(e => e.CreatedBy).HasMaxLength(450);
            entity.Property(e => e.DeletedBy).HasMaxLength(450);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.LastModifiedBy).HasMaxLength(450);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.SubscriptionPlan).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(500);
        });

        modelBuilder.Entity<GroupInvitation>(entity =>
        {
            entity.HasIndex(e => e.GroupId, "IX_GroupInvitations_GroupId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Group).WithMany(p => p.GroupInvitations).HasForeignKey(d => d.GroupId);
        });

        modelBuilder.Entity<GroupInvitationUsage>(entity =>
        {
            entity.HasIndex(e => e.InvitationId, "IX_GroupInvitationUsages_InvitationId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Invitation).WithMany(p => p.GroupInvitationUsages).HasForeignKey(d => d.InvitationId);
        });

        modelBuilder.Entity<GroupService>(entity =>
        {
            entity.HasKey(e => new { e.GroupId, e.ServiceId });

            entity.HasIndex(e => e.ExpiresAt, "IX_GroupServices_ExpiresAt");

            entity.HasIndex(e => e.GrantedAt, "IX_GroupServices_GrantedAt");

            entity.HasIndex(e => e.IsActive, "IX_GroupServices_IsActive");

            entity.HasIndex(e => e.ServiceId, "IX_GroupServices_ServiceId");

            entity.Property(e => e.GrantedBy).HasMaxLength(450);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity.HasOne(d => d.Group).WithMany(p => p.GroupServices).HasForeignKey(d => d.GroupId);

            entity.HasOne(d => d.Service).WithMany(p => p.GroupServices).HasForeignKey(d => d.ServiceId);
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasIndex(e => e.GroupId, "IX_LoginAttempts_GroupId");

            entity.HasIndex(e => e.UserId, "IX_LoginAttempts_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Group).WithMany(p => p.LoginAttempts).HasForeignKey(d => d.GroupId);

            entity.HasOne(d => d.User).WithMany(p => p.LoginAttempts).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasIndex(e => e.Action, "IX_Permissions_Action");

            entity.HasIndex(e => e.IsActive, "IX_Permissions_IsActive");

            entity.HasIndex(e => e.ParentId, "IX_Permissions_ParentId");

            entity.HasIndex(e => e.Resource, "IX_Permissions_Resource");

            entity.HasIndex(e => new { e.ServiceId, e.Name }, "IX_Permissions_ServiceId_Name").IsUnique();

            entity.HasIndex(e => e.Type, "IX_Permissions_Type");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Action).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Resource).HasMaxLength(200);

            entity.HasOne(d => d.Parent).WithMany(p => p.InverseParent).HasForeignKey(d => d.ParentId);

            entity.HasOne(d => d.Service).WithMany(p => p.Permissions).HasForeignKey(d => d.ServiceId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(e => e.GroupId, "IX_RefreshTokens_GroupId");

            entity.HasIndex(e => e.UserId, "IX_RefreshTokens_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Group).WithMany(p => p.RefreshTokens).HasForeignKey(d => d.GroupId);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasIndex(e => e.GroupId, "IX_Roles_GroupId");

            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();

            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.NormalizedName).HasMaxLength(256);

            entity.HasOne(d => d.Group).WithMany(p => p.Roles).HasForeignKey(d => d.GroupId);
        });

        modelBuilder.Entity<RoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_RoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.RoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.HasIndex(e => e.GrantedAt, "IX_RolePermissions_GrantedAt");

            entity.HasIndex(e => e.GroupId, "IX_RolePermissions_GroupId");

            entity.HasIndex(e => e.PermissionId, "IX_RolePermissions_PermissionId");

            entity.HasIndex(e => new { e.RoleId, e.PermissionId, e.GroupId }, "IX_RolePermissions_RoleId_PermissionId_GroupId").IsUnique();

            entity.Property(e => e.Conditions).HasMaxLength(2000);
            entity.Property(e => e.GrantedBy).HasMaxLength(450);

            entity.HasOne(d => d.Group).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions).HasForeignKey(d => d.PermissionId);

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<Service>(entity =>
        {
            entity.HasIndex(e => e.IsActive, "IX_Services_IsActive");

            entity.HasIndex(e => e.Name, "IX_Services_Name").IsUnique();

            entity.HasIndex(e => e.Status, "IX_Services_Status");

            entity.HasIndex(e => e.Type, "IX_Services_Type");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.ApiKey).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DisplayName).HasMaxLength(200);
            entity.Property(e => e.Endpoint).HasMaxLength(500);
            entity.Property(e => e.HealthCheckEndpoint).HasMaxLength(500);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.RegisteredBy).HasMaxLength(450);
            entity.Property(e => e.StatusMessage).HasMaxLength(500);
            entity.Property(e => e.Version).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.CreatedAt, "IX_Users_CreatedAt");

            entity.HasIndex(e => e.DefaultGroupId, "IX_Users_DefaultGroupId");

            entity.HasIndex(e => e.Email, "IX_Users_Email").IsUnique();

            entity.HasIndex(e => e.IsDeleted, "IX_Users_IsDeleted");

            entity.HasIndex(e => e.UserName, "IX_Users_UserName").IsUnique();

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.Property(e => e.About).HasMaxLength(1000);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.DeletedBy).HasMaxLength(450);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastLoginDevice).HasMaxLength(500);
            entity.Property(e => e.LastLoginIp).HasMaxLength(45);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
            entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(e => e.TwoFactorSecretKey).HasMaxLength(500);
            entity.Property(e => e.UserName).HasMaxLength(256);

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "UserRole",
                    r => r.HasOne<Role>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<User>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("UserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_UserRoles_RoleId");
                    });
        });

        modelBuilder.Entity<UserActivity>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "IX_UserActivities_UserId_CreatedAt");

            entity.Property(e => e.ActivityType).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.DeviceInfo).HasMaxLength(200);
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.Location).HasMaxLength(100);
            entity.Property(e => e.SessionId).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.UserActivities)
                .HasPrincipalKey(p => p.UserId)
                .HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserAddress>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserAddresses_UserId");

            entity.Property(e => e.AddressLine1).HasMaxLength(200);
            entity.Property(e => e.AddressLine2).HasMaxLength(200);
            entity.Property(e => e.AddressType).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.PostalCode).HasMaxLength(20);
            entity.Property(e => e.State).HasMaxLength(100);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithMany(p => p.UserAddresses)
                .HasPrincipalKey(p => p.UserId)
                .HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserClaims_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.UserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserConsent>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserConsents_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.UserConsents).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserDevice>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserDevices_UserId");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.User).WithMany(p => p.UserDevices).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserDocument>(entity =>
        {
            entity.HasIndex(e => new { e.UserId, e.Category }, "IX_UserDocuments_UserId_Category");

            entity.Property(e => e.BucketName).HasMaxLength(100);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Checksum).HasMaxLength(100);
            entity.Property(e => e.ContentType).HasMaxLength(100);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.FileExtension).HasMaxLength(10);
            entity.Property(e => e.FileName).HasMaxLength(255);
            entity.Property(e => e.ObjectKey).HasMaxLength(500);
            entity.Property(e => e.PublicUrl).HasMaxLength(1000);
            entity.Property(e => e.StoredFileName).HasMaxLength(255);
            entity.Property(e => e.ThumbnailUrl).HasMaxLength(1000);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.VirusScanResult).HasMaxLength(200);
            entity.Property(e => e.VirusScanStatus).HasMaxLength(50);

            entity.HasOne(d => d.User).WithMany(p => p.UserDocuments)
                .HasPrincipalKey(p => p.UserId)
                .HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserGroup>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.GroupId });

            entity.HasIndex(e => e.GroupId, "IX_UserGroups_GroupId");

            entity.HasIndex(e => e.IsActive, "IX_UserGroups_IsActive");

            entity.HasIndex(e => e.JoinedAt, "IX_UserGroups_JoinedAt");

            entity.Property(e => e.InvitedBy).HasMaxLength(450);
            entity.Property(e => e.SuspensionReason).HasMaxLength(500);

            entity.HasOne(d => d.Group).WithMany(p => p.UserGroups).HasForeignKey(d => d.GroupId);

            entity.HasOne(d => d.User).WithMany(p => p.UserGroups).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_UserLogins_UserId");

            entity.HasOne(d => d.User).WithMany(p => p.UserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<UserPreference>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_UserPreferences_UserId").IsUnique();

            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.DateFormat)
                .HasMaxLength(50)
                .HasDefaultValueSql("'MM/dd/yyyy'::character varying");
            entity.Property(e => e.ProfileVisibility)
                .HasMaxLength(20)
                .HasDefaultValueSql("'public'::character varying");
            entity.Property(e => e.Theme)
                .HasMaxLength(20)
                .HasDefaultValueSql("'system'::character varying");
            entity.Property(e => e.TimeFormat)
                .HasMaxLength(10)
                .HasDefaultValueSql("'12h'::character varying");
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UserId).HasMaxLength(450);

            entity.HasOne(d => d.User).WithOne(p => p.UserPreference)
                .HasPrincipalKey<UserProfile>(p => p.UserId)
                .HasForeignKey<UserPreference>(d => d.UserId);
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId, "AK_UserProfiles_UserId").IsUnique();

            entity.HasIndex(e => e.UserId, "IX_UserProfiles_UserId").IsUnique();

            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.CreatedBy).HasMaxLength(256);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.Property(e => e.UserId).HasMaxLength(450);
        });

        modelBuilder.Entity<UserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.HasOne(d => d.User).WithMany(p => p.UserTokens).HasForeignKey(d => d.UserId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
