using Identity.Core.DTOs;
using Identity.Core.Entities;
using Identity.Core.Constants;
using Identity.Infrastructure.Data;
using Identity.Application.Authorization.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/permissions")]
[RequirePermission(PermissionConstants.Identity.Permissions.Read)]
public class PermissionsController : ControllerBase
{
    private readonly IdentityDbContext _db;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(IdentityDbContext db, ILogger<PermissionsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetPermissions([FromQuery] GetPermissionsRequest request)
    {
        try
        {
            var query = _db.Permissions.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(p => p.Name.Contains(search) ||
                                         (p.DisplayName != null && p.DisplayName.Contains(search)) ||
                                         (p.Description != null && p.Description.Contains(search)) ||
                                         p.Resource.Contains(search) ||
                                         p.Action.Contains(search));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(p => p.IsActive == request.IsActive.Value);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderBy(p => p.Name)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(p => new PermissionDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    DisplayName = p.DisplayName,
                    Description = p.Description,
                    Resource = p.Resource,
                    Action = p.Action,
                    ServiceId = p.ServiceId,
                    ServiceName = _db.Services.Where(s => s.Id == p.ServiceId).Select(s => s.Name).FirstOrDefault() ?? string.Empty,
                    Type = p.Type,
                    Priority = p.Priority,
                    IsActive = p.IsActive,
                    CreatedAt = p.CreatedAt,
                    LastModifiedAt = p.LastModifiedAt,
                    RoleCount = _db.RolePermissions.Count(rp => rp.PermissionId == p.Id),
                    UserCount = 0
                })
                .ToListAsync();

            return Ok(new { data = items, totalCount, currentPage = request.Page, pageSize = request.PageSize });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{permissionId:guid}")]
    public async Task<IActionResult> GetPermission(Guid permissionId)
    {
        try
        {
            var p = await _db.Permissions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == permissionId);
            if (p == null) return NotFound("Permission not found");

            var dto = new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                Resource = p.Resource,
                Action = p.Action,
                ServiceId = p.ServiceId,
                ServiceName = await _db.Services.Where(s => s.Id == p.ServiceId).Select(s => s.Name).FirstOrDefaultAsync() ?? string.Empty,
                Type = p.Type,
                Priority = p.Priority,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                LastModifiedAt = p.LastModifiedAt,
                RoleCount = await _db.RolePermissions.CountAsync(rp => rp.PermissionId == p.Id),
                UserCount = 0
            };
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission {PermissionId}", permissionId);
            return StatusCode(500, "Internal server error");
        }
    }

    public class AdminCreatePermissionBody
    {
        [Required(ErrorMessage = "Permission name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Permission name must be between 3 and 200 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\.\-_]+$", ErrorMessage = "Permission name can only contain letters, numbers, dots, hyphens, and underscores")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }

    public class AdminUpdatePermissionBody
    {
        [Required(ErrorMessage = "Permission name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Permission name must be between 3 and 200 characters")]
        [RegularExpression(@"^[a-zA-Z0-9\.\-_]+$", ErrorMessage = "Permission name can only contain letters, numbers, dots, hyphens, and underscores")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> CreatePermission([FromBody] AdminCreatePermissionBody request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name)) return BadRequest(new { error = "Name is required" });

            var serviceId = await _db.Services.OrderBy(s => s.Name).Select(s => s.Id).FirstOrDefaultAsync();
            if (serviceId == Guid.Empty)
            {
                var svc = new Service { Id = Guid.NewGuid(), Name = "Core", DisplayName = "Core", Endpoint = "/", Type = ServiceType.Internal, RegisteredAt = DateTime.UtcNow, Status = ServiceStatus.Healthy, IsActive = true };
                _db.Services.Add(svc);
                await _db.SaveChangesAsync();
                serviceId = svc.Id;
            }

            var exists = await _db.Permissions.AnyAsync(p => p.ServiceId == serviceId && p.Name == request.Name);
            if (exists) return BadRequest(new { error = "Permission with same name already exists for the service" });

            var permission = new Permission
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.Name,
                Description = request.Description,
                Resource = "generic",
                Action = "custom",
                ServiceId = serviceId,
                Type = PermissionType.Custom,
                Priority = 0,
                IsActive = request.IsActive ?? true,
                CreatedAt = DateTime.UtcNow
            };

            _db.Permissions.Add(permission);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPermission), new { permissionId = permission.Id }, new { id = permission.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{permissionId:guid}")]
    public async Task<IActionResult> UpdatePermission(Guid permissionId, [FromBody] AdminUpdatePermissionBody request)
    {
        try
        {
            var p = await _db.Permissions.FirstOrDefaultAsync(x => x.Id == permissionId);
            if (p == null) return NotFound("Permission not found");

            p.Name = request.Name;
            p.DisplayName = request.Name;
            p.Description = request.Description;
            p.IsActive = request.IsActive;
            p.LastModifiedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { id = p.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionId}", permissionId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{permissionId:guid}")]
    public async Task<IActionResult> DeletePermission(Guid permissionId)
    {
        try
        {
            var p = await _db.Permissions.FirstOrDefaultAsync(x => x.Id == permissionId);
            if (p == null) return NotFound();
            _db.Permissions.Remove(p);
            await _db.SaveChangesAsync();
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission {PermissionId}", permissionId);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets permission matrix showing roles and their permissions
    /// </summary>
    [HttpGet("matrix")]
    [RequirePermission(PermissionConstants.Identity.Permissions.Read)]
    public async Task<IActionResult> GetPermissionMatrix()
    {
        try
        {
            var services = await _db.Services
                .Where(s => s.IsActive)
                .Select(s => new
                {
                    ServiceId = s.Id,
                    ServiceName = s.Name,
                    Permissions = _db.Permissions
                        .Where(p => p.ServiceId == s.Id && p.IsActive)
                        .Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.DisplayName,
                            p.Resource,
                            p.Action,
                            p.Type,
                            p.Priority
                        })
                        .OrderBy(p => p.Resource)
                        .ThenBy(p => p.Priority)
                })
                .ToListAsync();

            var roles = await _db.Roles
                .Where(r => r.IsActive)
                .Select(r => new
                {
                    RoleId = r.Id,
                    RoleName = r.Name,
                    Description = r.Description,
                    IsSystemRole = r.IsSystemRole,
                    GroupId = r.GroupId,
                    GroupName = r.GroupId.HasValue
                        ? _db.Groups.Where(g => g.Id == r.GroupId).Select(g => g.Name).FirstOrDefault()
                        : null,
                    Permissions = _db.RolePermissions
                        .Where(rp => rp.RoleId == r.Id)
                        .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => new
                        {
                            p.Id,
                            p.Name,
                            p.DisplayName,
                            p.Resource,
                            p.Action,
                            p.ServiceId,
                            rp.GrantedAt,
                            rp.GrantedBy
                        })
                        .OrderBy(p => p.Name)
                })
                .ToListAsync();

            var matrix = new
            {
                Services = services,
                Roles = roles,
                GeneratedAt = DateTime.UtcNow,
                Summary = new
                {
                    TotalServices = services.Count,
                    TotalPermissions = services.Sum(s => s.Permissions.Count()),
                    TotalRoles = roles.Count,
                    SystemRoles = roles.Count(r => r.IsSystemRole),
                    CustomRoles = roles.Count(r => !r.IsSystemRole)
                }
            };

            return Ok(matrix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission matrix");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets permissions grouped by service
    /// </summary>
    [HttpGet("by-service")]
    [RequirePermission(PermissionConstants.Identity.Permissions.Read)]
    public async Task<IActionResult> GetPermissionsByService()
    {
        try
        {
            var permissionsByService = await _db.Permissions
                .Where(p => p.IsActive)
                .Join(_db.Services, p => p.ServiceId, s => s.Id, (p, s) => new { Permission = p, Service = s })
                .GroupBy(ps => ps.Service.Name)
                .Select(g => new
                {
                    ServiceName = g.Key,
                    Permissions = g.Select(ps => new
                    {
                        ps.Permission.Id,
                        ps.Permission.Name,
                        ps.Permission.DisplayName,
                        ps.Permission.Resource,
                        ps.Permission.Action,
                        ps.Permission.Type,
                        ps.Permission.Priority
                    }).OrderBy(p => p.Resource).ThenBy(p => p.Action)
                })
                .ToDictionaryAsync(g => g.ServiceName, g => g.Permissions);

            return Ok(permissionsByService);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions by service");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets permissions grouped by resource
    /// </summary>
    [HttpGet("by-resource")]
    [RequirePermission(PermissionConstants.Identity.Permissions.Read)]
    public async Task<IActionResult> GetPermissionsByResource()
    {
        try
        {
            var permissionsByResource = await _db.Permissions
                .Where(p => p.IsActive)
                .GroupBy(p => p.Resource)
                .Select(g => new
                {
                    Resource = g.Key,
                    Permissions = g.Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.DisplayName,
                        p.Action,
                        p.Type,
                        p.Priority,
                        ServiceName = _db.Services.Where(s => s.Id == p.ServiceId).Select(s => s.Name).FirstOrDefault()
                    }).OrderBy(p => p.Action).ThenBy(p => p.Priority)
                })
                .ToDictionaryAsync(g => g.Resource, g => g.Permissions);

            return Ok(permissionsByResource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions by resource");
            return StatusCode(500, "Internal server error");
        }
    }
}

