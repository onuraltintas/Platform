using System.ComponentModel.DataAnnotations;
using Enterprise.Shared.Permissions;
using Identity.Core.Entities;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/permissions")] 
public class PermissionsSyncController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<PermissionsSyncController> _logger;

    public PermissionsSyncController(IdentityDbContext db, ILogger<PermissionsSyncController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public class SyncRequest
    {
        [Required]
        public PermissionManifest Manifest { get; set; } = new();
        public bool AutoAssignNewToAdmin { get; set; } = true;
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromBody] SyncRequest request)
    {
        try
        {
            var providedKey = Request.Headers["X-Permissions-Sync-Key"].FirstOrDefault();
            var expectedKey = Environment.GetEnvironmentVariable("PERMISSIONS_SYNC_KEY");
            if (string.IsNullOrWhiteSpace(expectedKey) || !string.Equals(providedKey, expectedKey))
            {
                return Unauthorized("Invalid sync key");
            }

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var serviceName = request.Manifest.Service.Name.Trim();
            if (string.IsNullOrWhiteSpace(serviceName)) return BadRequest("Service name is required");

            // Upsert Service
            var service = await _db.Services.FirstOrDefaultAsync(s => s.Name == serviceName);
            if (service == null)
            {
                service = new Service
                {
                    Id = Guid.NewGuid(),
                    Name = serviceName,
                    DisplayName = request.Manifest.Service.DisplayName ?? serviceName,
                    Endpoint = request.Manifest.Service.Endpoint ?? "/",
                    Version = request.Manifest.Service.Version ?? "1.0.0",
                    Type = ServiceType.Internal,
                    RegisteredAt = DateTime.UtcNow,
                    Status = ServiceStatus.Healthy,
                    IsActive = true
                };
                _db.Services.Add(service);
                await _db.SaveChangesAsync();
            }

            // Existing permissions for service
            var existing = await _db.Permissions
                .Where(p => p.ServiceId == service.Id)
                .ToListAsync();
            var existingByName = existing.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;
            var createdPermissions = new List<Permission>();

            foreach (var m in request.Manifest.Permissions)
            {
                if (existingByName.TryGetValue(m.Name, out var perm))
                {
                    // Update fields
                    perm.DisplayName = m.DisplayName ?? m.Name;
                    perm.Description = m.Description;
                    perm.Resource = m.Resource;
                    perm.Action = m.Action;
                    perm.Priority = m.Priority;
                    perm.IsActive = m.IsActive;
                    perm.LastModifiedAt = now;
                }
                else
                {
                    var newPerm = new Permission
                    {
                        Id = Guid.NewGuid(),
                        Name = m.Name,
                        DisplayName = m.DisplayName ?? m.Name,
                        Description = m.Description,
                        Resource = m.Resource,
                        Action = m.Action,
                        ServiceId = service.Id,
                        Type = PermissionType.Custom,
                        Priority = m.Priority,
                        IsActive = m.IsActive,
                        CreatedAt = now
                    };
                    createdPermissions.Add(newPerm);
                    _db.Permissions.Add(newPerm);
                }
            }

            await _db.SaveChangesAsync();

            // Auto-assign to Admin role if requested
            if (request.AutoAssignNewToAdmin && createdPermissions.Count > 0)
            {
                var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
                if (adminRole != null)
                {
                    var existingLinks = await _db.RolePermissions
                        .Where(rp => rp.RoleId == adminRole.Id)
                        .Select(rp => rp.PermissionId)
                        .ToListAsync();
                    var linkSet = existingLinks.ToHashSet();
                    foreach (var p in createdPermissions)
                    {
                        if (!linkSet.Contains(p.Id))
                        {
                            _db.RolePermissions.Add(new RolePermission
                            {
                                RoleId = adminRole.Id,
                                PermissionId = p.Id,
                                GrantedAt = now,
                                GrantedBy = User?.Identity?.Name ?? "sync"
                            });
                        }
                    }
                    await _db.SaveChangesAsync();
                }
            }

            return Ok(new
            {
                service = service.Name,
                created = createdPermissions.Count,
                updated = request.Manifest.Permissions.Count - createdPermissions.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing permissions");
            return StatusCode(500, "Internal server error");
        }
    }
}

