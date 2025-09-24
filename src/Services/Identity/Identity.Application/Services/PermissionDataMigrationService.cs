using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

/// <summary>
/// Data migration service to normalize legacy permission codes/patterns
/// Example: SpeedReading.Texts.* -> SpeedReading.ReadingTexts.*
/// </summary>
public class PermissionDataMigrationService
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<PermissionDataMigrationService> _logger;

    public PermissionDataMigrationService(IdentityDbContext context, ILogger<PermissionDataMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        const string legacyPrefix = "SpeedReading.Texts.";
        const string newPrefix = "SpeedReading.ReadingTexts.";

        try
        {
            // 1) Update Permission codes
            var legacyPermissions = await _context.Permissions
                .Where(p => p.Code.StartsWith(legacyPrefix))
                .ToListAsync(cancellationToken);

            foreach (var perm in legacyPermissions)
            {
                var updatedCode = perm.Code.Replace(legacyPrefix, newPrefix);

                var existsNew = await _context.Permissions.AnyAsync(p => p.Code == updatedCode, cancellationToken);
                if (existsNew)
                {
                    perm.IsActive = false;
                    perm.LastModifiedAt = DateTime.UtcNow;
                    perm.LastModifiedBy = "DataMigration";
                }
                else
                {
                    perm.Code = updatedCode;
                    perm.LastModifiedAt = DateTime.UtcNow;
                    perm.LastModifiedBy = "DataMigration";
                }
            }

            // 2) Update wildcard patterns in RolePermissions
            var legacyRolePatterns = await _context.RolePermissions
                .Where(rp => rp.IsWildcard && rp.PermissionPattern != null && rp.PermissionPattern.StartsWith(legacyPrefix))
                .ToListAsync(cancellationToken);

            foreach (var rp in legacyRolePatterns)
            {
                rp.PermissionPattern = rp.PermissionPattern!.Replace(legacyPrefix, newPrefix);
                rp.GrantedAt = rp.GrantedAt == default ? DateTime.UtcNow : rp.GrantedAt;
                rp.IsActive = true;
            }

            // 3) Update wildcard patterns in UserPermissions
            var legacyUserPatterns = await _context.UserPermissions
                .Where(up => up.IsWildcard && up.PermissionPattern != null && up.PermissionPattern.StartsWith(legacyPrefix))
                .ToListAsync(cancellationToken);

            foreach (var up in legacyUserPatterns)
            {
                up.PermissionPattern = up.PermissionPattern!.Replace(legacyPrefix, newPrefix);
                up.IsActive = true;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Permission data migration completed: {Legacy} -> {New}", legacyPrefix, newPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Permission data migration encountered an error");
        }
    }
}

