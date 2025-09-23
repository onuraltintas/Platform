using Identity.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Identity.Infrastructure.Data;

public class IdentityDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    // Groups/Tenants
    public DbSet<Group> Groups { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<GroupInvitation> GroupInvitations { get; set; }
    public DbSet<GroupInvitationUsage> GroupInvitationUsages { get; set; }
    
    // Services & Permissions
    public DbSet<Service> Services { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    public DbSet<UserPermission> UserPermissions { get; set; }
    public DbSet<PermissionAuditLog> PermissionAuditLogs { get; set; }
    public DbSet<GroupService> GroupServices { get; set; }
    
    // Security & Audit
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<UserConsent> UserConsents { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }

    // Zero Trust Architecture
    public DbSet<TrustScore> TrustScores { get; set; }
    public DbSet<TrustScoreHistory> TrustScoreHistory { get; set; }
    public DbSet<DeviceTrust> DeviceTrusts { get; set; }
    public DbSet<DeviceActivity> DeviceActivities { get; set; }
    public DbSet<SecurityPolicy> SecurityPolicies { get; set; }
    public DbSet<PolicyViolation> PolicyViolations { get; set; }

    // Advanced Audit System
    public DbSet<AuditEvent> AuditEvents { get; set; }
    public DbSet<AuditEventAttachment> AuditEventAttachments { get; set; }
    public DbSet<SecurityAlert> SecurityAlerts { get; set; }
    public DbSet<SecurityAlertAction> SecurityAlertActions { get; set; }
    public DbSet<AlertRule> AlertRules { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Set default schema to match existing tables in PostgreSQL
        builder.HasDefaultSchema("public");

        // Rename Identity tables
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

        // Configure ASP.NET Core Identity tables for PostgreSQL - must be after base.OnModelCreating()
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            foreach (var property in entity.GetProperties())
            {
                if (property.ClrType == typeof(string))
                {
                    var columnType = property.GetColumnType();
                    if (columnType != null && columnType.Contains("nvarchar"))
                    {
                        property.SetColumnType(columnType.Replace("nvarchar", "text"));
                    }
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is ApplicationUser && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (ApplicationUser)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.LastModifiedAt = DateTime.UtcNow;
            }
        }
    }
}