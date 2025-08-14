using Microsoft.EntityFrameworkCore;
using AutoMapper;
using EgitimPlatform.Services.IdentityService.Data;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Services.IdentityService.Models.Entities;
using EgitimPlatform.Shared.Errors.Models;
using EgitimPlatform.Shared.Errors.Common;
using EgitimPlatform.Shared.Errors.Exceptions;
using EgitimPlatform.Shared.Security.Services;
using EgitimPlatform.Shared.Logging.Services;
using System.Diagnostics.CodeAnalysis;

namespace EgitimPlatform.Services.IdentityService.Services;

[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Service layer loglayıp ServiceResult döndürür; global exception handler genel durumları ele alır")]
[SuppressMessage("Style", "SA1124:Do not use regions", Justification = "Büyük servis sınıfında bölümler okunabilirliği artırıyor")]
[SuppressMessage("Style", "SA1101:Prefix local calls with this", Justification = "Ekip stili gereği this prefix zorunlu değil")]
[SuppressMessage("Style", "SA1413:Use trailing comma in multi-line initializers", Justification = "Minör stil; fonksiyonelliği etkilemiyor, kademeli ele alınacak")]
public class AdminService : IAdminService
{
    private readonly IdentityDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly IMapper _mapper;
    private readonly IStructuredLogger _logger;

    public AdminService(
        IdentityDbContext context,
        IPasswordService passwordService,
        IMapper mapper,
        IStructuredLogger logger)
    {
        _context = context;
        _passwordService = passwordService;
        _mapper = mapper;
        _logger = logger;
    }

    #region Role Management

    public async Task<ServiceResult<List<RoleDto>>> GetRolesAsync(string? search = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Name.Contains(search) || (r.Description != null && r.Description.Contains(search)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(r => r.IsActive == isActive.Value);
            }

            query = query.OrderBy(r => r.Name);

            var totalCount = await query.CountAsync();
            var roles = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var roleDtos = roles.Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                Permissions = r.RolePermissions.Select(rp => rp.Permission.Name).ToList()
            }).ToList();

            return ServiceResult<List<RoleDto>>.Success(roleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting roles", ex, new { search, isActive, page, pageSize });
            return ServiceResult<List<RoleDto>>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve roles");
        }
    }

    public async Task<ServiceResult<RoleDto?>> GetRoleByIdAsync(string roleId)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return ServiceResult<RoleDto?>.Failure(ErrorCodes.NOT_FOUND, "Role not found");
            }

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).ToList()
            };

            return ServiceResult<RoleDto?>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting role by id", ex, new { roleId });
            return ServiceResult<RoleDto?>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve role");
        }
    }

    public async Task<ServiceResult<RoleDto>> CreateRoleAsync(CreateRoleRequest request)
    {
        try
        {
            // Check if role name already exists
            var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Name);
            if (existingRole != null)
            {
                return ServiceResult<RoleDto>.Failure(ErrorCodes.CONFLICT, "Role with this name already exists");
            }

            var role = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Roles.Add(role);

            // Add role permissions
            if (request.PermissionIds.Any())
            {
                var permissions = await _context.Permissions
                    .Where(p => request.PermissionIds.Contains(p.Id))
                    .ToListAsync();

                foreach (var permission in permissions)
                {
                    var rolePermission = new RolePermission
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    };
                    _context.RolePermissions.Add(rolePermission);
                }
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                Permissions = (request.PermissionIds ?? new List<string>()).ToList()
            };

            _logger.LogInformation("Role created successfully", new { RoleId = role.Id, RoleName = role.Name });
            return ServiceResult<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating role", ex, request);
            return ServiceResult<RoleDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to create role");
        }
    }

    public async Task<ServiceResult<RoleDto>> UpdateRoleAsync(string roleId, UpdateRoleRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<RoleDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId).ConfigureAwait(false);

            if (role == null)
            {
                return ServiceResult<RoleDto>.Failure(ErrorCodes.NOT_FOUND, "Role not found");
            }

            // Check if new name already exists (excluding current role)
            if (role.Name != request.Name)
            {
                var existingRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Name && r.Id != roleId).ConfigureAwait(false);
                if (existingRole != null)
                {
                    return ServiceResult<RoleDto>.Failure(ErrorCodes.CONFLICT, "Role with this name already exists");
                }
            }

            role.Name = request.Name;
            role.Description = request.Description;
            role.IsActive = request.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            // Update role permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            if (request.PermissionIds != null && request.PermissionIds.Count > 0)
            {
                var permissions = await _context.Permissions
                    .Where(p => request.PermissionIds.Contains(p.Id))
                    .ToListAsync().ConfigureAwait(false);

                foreach (var permission in permissions)
                {
                    var rolePermission = new RolePermission
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoleId = role.Id,
                        PermissionId = permission.Id
                    };
                    _context.RolePermissions.Add(rolePermission);
                }
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);

            var roleDto = new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsActive = role.IsActive,
                CreatedAt = role.CreatedAt,
                Permissions = (request.PermissionIds ?? new List<string>()).ToList()
            };

            _logger.LogInformation("Role updated successfully", new { RoleId = role.Id, RoleName = role.Name });
            return ServiceResult<RoleDto>.Success(roleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating role", ex, new { roleId, request });
            return ServiceResult<RoleDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to update role");
        }
    }

    public async Task<ServiceResult<bool>> DeleteRoleAsync(string roleId)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "Role not found");
            }

            // Check if role is in use
            if (role.UserRoles.Any())
            {
                return ServiceResult<bool>.Failure(ErrorCodes.CONFLICT, "Cannot delete role that is assigned to users");
            }

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Role deleted successfully", new { RoleId = roleId });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting role", ex, new { roleId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to delete role");
        }
    }

    #endregion

    #region Category Management

    public async Task<ServiceResult<List<CategoryDto>>> GetCategoriesAsync(string? search = null, string? type = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Name.Contains(search) || (c.Description != null && c.Description.Contains(search)));
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(c => c.Type == type);
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            query = query.OrderBy(c => c.Name);

            var categories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var categoryDtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                Type = c.Type,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            }).ToList();

            return ServiceResult<List<CategoryDto>>.Success(categoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting categories", ex, new { search, type, isActive, page, pageSize });
            return ServiceResult<List<CategoryDto>>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve categories");
        }
    }

    public async Task<ServiceResult<CategoryDto?>> GetCategoryByIdAsync(string categoryId)
    {
        try
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
            {
                return ServiceResult<CategoryDto?>.Failure(ErrorCodes.NOT_FOUND, "Category not found");
            }

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Type = category.Type,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            return ServiceResult<CategoryDto?>.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting category by id", ex, new { categoryId });
            return ServiceResult<CategoryDto?>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve category");
        }
    }

    public async Task<ServiceResult<CategoryDto>> CreateCategoryAsync(CreateCategoryRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<CategoryDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            // Check if category name already exists
            var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == request.Name).ConfigureAwait(false);
            if (existingCategory != null)
            {
                return ServiceResult<CategoryDto>.Failure(ErrorCodes.CONFLICT, "Category with this name already exists");
            }

            var category = new Category
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Type = category.Type,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            _logger.LogInformation("Category created successfully", new { CategoryId = category.Id, CategoryName = category.Name });
            return ServiceResult<CategoryDto>.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating category", ex, request);
            return ServiceResult<CategoryDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to create category");
        }
    }

    public async Task<ServiceResult<CategoryDto>> UpdateCategoryAsync(string categoryId, UpdateCategoryRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<CategoryDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == categoryId).ConfigureAwait(false);

            if (category == null)
            {
                return ServiceResult<CategoryDto>.Failure(ErrorCodes.NOT_FOUND, "Category not found");
            }

            // Check if new name already exists (excluding current category)
            if (category.Name != request.Name)
            {
                var existingCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == request.Name && c.Id != categoryId).ConfigureAwait(false);
                if (existingCategory != null)
                {
                    return ServiceResult<CategoryDto>.Failure(ErrorCodes.CONFLICT, "Category with this name already exists");
                }
            }

            category.Name = request.Name;
            category.Description = request.Description;
            category.Type = request.Type;
            category.IsActive = request.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            var categoryDto = new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Type = category.Type,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt
            };

            _logger.LogInformation("Category updated successfully", new { CategoryId = category.Id, CategoryName = category.Name });
            return ServiceResult<CategoryDto>.Success(categoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating category", ex, new { categoryId, request });
            return ServiceResult<CategoryDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to update category");
        }
    }

    public async Task<ServiceResult<bool>> DeleteCategoryAsync(string categoryId)
    {
        try
        {
            var category = await _context.Categories
                .Include(c => c.UserCategories)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "Category not found");
            }

            // Check if category is in use
            if (category.UserCategories.Any())
            {
                return ServiceResult<bool>.Failure(ErrorCodes.CONFLICT, "Cannot delete category that is assigned to users");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Category deleted successfully", new { CategoryId = categoryId });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting category", ex, new { categoryId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to delete category");
        }
    }

    #endregion

    #region Permission Management

    public async Task<ServiceResult<List<PermissionDto>>> GetPermissionsAsync(string? search = null, string? group = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.Permissions.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search) || (p.Description != null && p.Description.Contains(search)));
            }

            if (!string.IsNullOrEmpty(group))
            {
                query = query.Where(p => p.Group == group);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            query = query.OrderBy(p => p.Group).ThenBy(p => p.Name);

            var permissions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var permissionDtos = permissions.Select(p => new PermissionDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Group = p.Group,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            }).ToList();

            return ServiceResult<List<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting permissions", ex, new { search, group, isActive, page, pageSize });
            return ServiceResult<List<PermissionDto>>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve permissions");
        }
    }

    public async Task<ServiceResult<PermissionDto?>> GetPermissionByIdAsync(string permissionId)
    {
        try
        {
            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == permissionId).ConfigureAwait(false);

            if (permission == null)
            {
                return ServiceResult<PermissionDto?>.Failure(ErrorCodes.NOT_FOUND, "Permission not found");
            }

            var permissionDto = new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Group = permission.Group,
                IsActive = permission.IsActive,
                CreatedAt = permission.CreatedAt
            };

            return ServiceResult<PermissionDto?>.Success(permissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting permission by id", ex, new { permissionId });
            return ServiceResult<PermissionDto?>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve permission");
        }
    }

    public async Task<ServiceResult<PermissionDto>> CreatePermissionAsync(CreatePermissionRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<PermissionDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            // Check if permission name already exists
            var existingPermission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == request.Name).ConfigureAwait(false);
            if (existingPermission != null)
            {
                return ServiceResult<PermissionDto>.Failure(ErrorCodes.CONFLICT, "Permission with this name already exists");
            }

            var permission = new Permission
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                Group = request.Group,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Permissions.Add(permission);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            var permissionDto = new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Group = permission.Group,
                IsActive = permission.IsActive,
                CreatedAt = permission.CreatedAt
            };

            _logger.LogInformation("Permission created successfully", new { PermissionId = permission.Id, PermissionName = permission.Name });
            return ServiceResult<PermissionDto>.Success(permissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating permission", ex, request);
            return ServiceResult<PermissionDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to create permission");
        }
    }

    public async Task<ServiceResult<PermissionDto>> UpdatePermissionAsync(string permissionId, UpdatePermissionRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<PermissionDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == permissionId).ConfigureAwait(false);

            if (permission == null)
            {
                return ServiceResult<PermissionDto>.Failure(ErrorCodes.NOT_FOUND, "Permission not found");
            }

            // Check if new name already exists (excluding current permission)
            if (permission.Name != request.Name)
            {
                var existingPermission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == request.Name && p.Id != permissionId).ConfigureAwait(false);
                if (existingPermission != null)
                {
                    return ServiceResult<PermissionDto>.Failure(ErrorCodes.CONFLICT, "Permission with this name already exists");
                }
            }

            permission.Name = request.Name;
            permission.Description = request.Description;
            permission.Group = request.Group;
            permission.IsActive = request.IsActive;
            permission.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            var permissionDto = new PermissionDto
            {
                Id = permission.Id,
                Name = permission.Name,
                Description = permission.Description,
                Group = permission.Group,
                IsActive = permission.IsActive,
                CreatedAt = permission.CreatedAt
            };

            _logger.LogInformation("Permission updated successfully", new { PermissionId = permission.Id, PermissionName = permission.Name });
            return ServiceResult<PermissionDto>.Success(permissionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating permission", ex, new { permissionId, request });
            return ServiceResult<PermissionDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to update permission");
        }
    }

    public async Task<ServiceResult<bool>> DeletePermissionAsync(string permissionId)
    {
        try
        {
            var permission = await _context.Permissions
                .Include(p => p.RolePermissions)
                .FirstOrDefaultAsync(p => p.Id == permissionId);

            if (permission == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "Permission not found");
            }

            // Check if permission is in use
            if (permission.RolePermissions.Any())
            {
                return ServiceResult<bool>.Failure(ErrorCodes.CONFLICT, "Cannot delete permission that is assigned to roles");
            }

            _context.Permissions.Remove(permission);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Permission deleted successfully", new { PermissionId = permissionId });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting permission", ex, new { permissionId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to delete permission");
        }
    }

    #endregion

    #region User Role Management

    public async Task<ServiceResult<List<UserRoleDto>>> GetUserRolesAsync(string? userId = null, string? roleId = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(ur => ur.UserId == userId);
            }

            if (!string.IsNullOrEmpty(roleId))
            {
                query = query.Where(ur => ur.RoleId == roleId);
            }

            if (isActive.HasValue)
            {
                query = query.Where(ur => ur.IsActive == isActive.Value);
            }

            query = query.OrderBy(ur => ur.User.UserName).ThenBy(ur => ur.Role.Name);

            var userRoles = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userRoleDtos = userRoles.Select(ur => new UserRoleDto
            {
                Id = ur.Id,
                UserId = ur.UserId,
                UserName = ur.User.UserName,
                RoleId = ur.RoleId,
                RoleName = ur.Role.Name,
                AssignedAt = ur.AssignedAt,
                ExpiresAt = ur.ExpiresAt,
                IsActive = ur.IsActive,
                AssignedBy = ur.AssignedBy,
                Notes = ur.Notes
            }).ToList();

            return ServiceResult<List<UserRoleDto>>.Success(userRoleDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting user roles", ex, new { userId, roleId, isActive, page, pageSize });
            return ServiceResult<List<UserRoleDto>>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve user roles");
        }
    }

    public async Task<ServiceResult<UserRoleDto>> AssignRoleToUserAsync(AssignRoleToUserRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<UserRoleDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            // Check if user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId).ConfigureAwait(false);
            if (user == null)
            {
                return ServiceResult<UserRoleDto>.Failure(ErrorCodes.NOT_FOUND, "User not found");
            }

            // Check if role exists
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == request.RoleId).ConfigureAwait(false);
            if (role == null)
            {
                return ServiceResult<UserRoleDto>.Failure(ErrorCodes.NOT_FOUND, "Role not found");
            }

            // Check if user already has this role
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == request.UserId && ur.RoleId == request.RoleId)
                .ConfigureAwait(false);

            if (existingUserRole != null)
            {
                return ServiceResult<UserRoleDto>.Failure(ErrorCodes.CONFLICT, "User already has this role assigned");
            }

            var userRole = new UserRole
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                RoleId = request.RoleId,
                AssignedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                IsActive = true,
                AssignedBy = "Admin", // TODO: Get from context
                Notes = request.Notes
            };

            _context.UserRoles.Add(userRole);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            var userRoleDto = new UserRoleDto
            {
                Id = userRole.Id,
                UserId = userRole.UserId,
                UserName = user.UserName,
                RoleId = userRole.RoleId,
                RoleName = role.Name,
                AssignedAt = userRole.AssignedAt,
                ExpiresAt = userRole.ExpiresAt,
                IsActive = userRole.IsActive,
                AssignedBy = userRole.AssignedBy,
                Notes = userRole.Notes
            };

            _logger.LogInformation("Role assigned to user successfully", new { UserRoleId = userRole.Id, UserId = request.UserId, RoleId = request.RoleId });
            return ServiceResult<UserRoleDto>.Success(userRoleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error assigning role to user", ex, request);
            return ServiceResult<UserRoleDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to assign role to user");
        }
    }

    public async Task<ServiceResult<bool>> RemoveRoleFromUserAsync(string userId, string roleId)
    {
        try
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "User role assignment not found");
            }

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Role removed from user successfully", new { UserId = userId, RoleId = roleId });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error removing role from user", ex, new { userId, roleId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to remove role from user");
        }
    }

    public async Task<ServiceResult<UserRoleDto>> UpdateUserRoleAsync(string userId, string roleId, UpdateUserRoleRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<UserRoleDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var userRole = await _context.UserRoles
                .Include(ur => ur.User)
                .Include(ur => ur.Role)
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId)
                .ConfigureAwait(false);

            if (userRole == null)
            {
                return ServiceResult<UserRoleDto>.Failure(ErrorCodes.NOT_FOUND, "User role assignment not found");
            }

            userRole.ExpiresAt = request.ExpiresAt;
            userRole.IsActive = request.IsActive;
            userRole.Notes = request.Notes;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            var userRoleDto = new UserRoleDto
            {
                Id = userRole.Id,
                UserId = userRole.UserId,
                UserName = userRole.User.UserName,
                RoleId = userRole.RoleId,
                RoleName = userRole.Role.Name,
                AssignedAt = userRole.AssignedAt,
                ExpiresAt = userRole.ExpiresAt,
                IsActive = userRole.IsActive,
                AssignedBy = userRole.AssignedBy,
                Notes = userRole.Notes
            };

            _logger.LogInformation("User role updated successfully", new { UserId = userId, RoleId = roleId });
            return ServiceResult<UserRoleDto>.Success(userRoleDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating user role", ex, new { userId, roleId, request });
            return ServiceResult<UserRoleDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to update user role");
        }
    }

    #endregion

    #region User Category Management

    public async Task<ServiceResult<List<UserCategoryDto>>> GetUserCategoriesAsync(string? userId = null, string? categoryId = null, bool? isActive = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.UserCategories
                .Include(uc => uc.User)
                .Include(uc => uc.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(uc => uc.UserId == userId);
            }

            if (!string.IsNullOrEmpty(categoryId))
            {
                query = query.Where(uc => uc.CategoryId == categoryId);
            }

            if (isActive.HasValue)
            {
                query = query.Where(uc => uc.IsActive == isActive.Value);
            }

            query = query.OrderBy(uc => uc.User.UserName).ThenBy(uc => uc.Category.Name);

            var userCategories = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userCategoryDtos = userCategories.Select(uc => new UserCategoryDto
            {
                Id = uc.Id,
                UserId = uc.UserId,
                UserName = uc.User.UserName,
                CategoryId = uc.CategoryId,
                CategoryName = uc.Category.Name,
                AssignedAt = uc.AssignedAt,
                ExpiresAt = uc.ExpiresAt,
                IsActive = uc.IsActive,
                AssignedBy = uc.AssignedBy,
                Notes = uc.Notes
            }).ToList();

            return ServiceResult<List<UserCategoryDto>>.Success(userCategoryDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting user categories", ex, new { userId, categoryId, isActive, page, pageSize });
            return ServiceResult<List<UserCategoryDto>>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve user categories");
        }
    }

    public async Task<ServiceResult<UserCategoryDto>> AssignCategoryToUserAsync(AssignCategoryToUserRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<UserCategoryDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            // Check if user exists
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
            {
                return ServiceResult<UserCategoryDto>.Failure(ErrorCodes.NOT_FOUND, "User not found");
            }

            // Check if category exists
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId);
            if (category == null)
            {
                return ServiceResult<UserCategoryDto>.Failure(ErrorCodes.NOT_FOUND, "Category not found");
            }

            // Check if user already has this category
            var existingUserCategory = await _context.UserCategories
                .FirstOrDefaultAsync(uc => uc.UserId == request.UserId && uc.CategoryId == request.CategoryId);

            if (existingUserCategory != null)
            {
                return ServiceResult<UserCategoryDto>.Failure(ErrorCodes.CONFLICT, "User already has this category assigned");
            }

            var userCategory = new UserCategory
            {
                Id = Guid.NewGuid().ToString(),
                UserId = request.UserId,
                CategoryId = request.CategoryId,
                AssignedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                IsActive = true,
                AssignedBy = "Admin", // TODO: Get from context
                Notes = request.Notes
            };

            _context.UserCategories.Add(userCategory);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            var userCategoryDto = new UserCategoryDto
            {
                Id = userCategory.Id,
                UserId = userCategory.UserId,
                UserName = user.UserName,
                CategoryId = userCategory.CategoryId,
                CategoryName = category.Name,
                AssignedAt = userCategory.AssignedAt,
                ExpiresAt = userCategory.ExpiresAt,
                IsActive = userCategory.IsActive,
                AssignedBy = userCategory.AssignedBy,
                Notes = userCategory.Notes
            };

            _logger.LogInformation("Category assigned to user successfully", new { UserCategoryId = userCategory.Id, UserId = request.UserId, CategoryId = request.CategoryId });
            return ServiceResult<UserCategoryDto>.Success(userCategoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error assigning category to user", ex, request);
            return ServiceResult<UserCategoryDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to assign category to user");
        }
    }

    public async Task<ServiceResult<bool>> RemoveCategoryFromUserAsync(string userId, string categoryId)
    {
        try
        {
            var userCategory = await _context.UserCategories
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CategoryId == categoryId)
                .ConfigureAwait(false);

            if (userCategory == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "User category assignment not found");
            }

            _context.UserCategories.Remove(userCategory);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Category removed from user successfully", new { UserId = userId, CategoryId = categoryId });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error removing category from user", ex, new { userId, categoryId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to remove category from user");
        }
    }

    public async Task<ServiceResult<UserCategoryDto>> UpdateUserCategoryAsync(string userId, string categoryId, UpdateUserCategoryRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<UserCategoryDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var userCategory = await _context.UserCategories
                .Include(uc => uc.User)
                .Include(uc => uc.Category)
                .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CategoryId == categoryId);

            if (userCategory == null)
            {
                return ServiceResult<UserCategoryDto>.Failure(ErrorCodes.NOT_FOUND, "User category assignment not found");
            }

            userCategory.ExpiresAt = request.ExpiresAt;
            userCategory.IsActive = request.IsActive;
            userCategory.Notes = request.Notes;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            var userCategoryDto = new UserCategoryDto
            {
                Id = userCategory.Id,
                UserId = userCategory.UserId,
                UserName = userCategory.User.UserName,
                CategoryId = userCategory.CategoryId,
                CategoryName = userCategory.Category.Name,
                AssignedAt = userCategory.AssignedAt,
                ExpiresAt = userCategory.ExpiresAt,
                IsActive = userCategory.IsActive,
                AssignedBy = userCategory.AssignedBy,
                Notes = userCategory.Notes
            };

            _logger.LogInformation("User category updated successfully", new { UserId = userId, CategoryId = categoryId });
            return ServiceResult<UserCategoryDto>.Success(userCategoryDto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating user category", ex, new { userId, categoryId, request });
            return ServiceResult<UserCategoryDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to update user category");
        }
    }

    #endregion

    #region Role Permission Management

    public async Task<ServiceResult<List<string>>> GetRolePermissionsAsync(string roleId)
    {
        try
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                return ServiceResult<List<string>>.Failure(ErrorCodes.NOT_FOUND, "Role not found");
            }

            var permissions = role.RolePermissions
                .Select(rp => rp.Permission.Name)
                .OrderBy(p => p)
                .ToList();

            return ServiceResult<List<string>>.Success(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting role permissions", ex, new { roleId });
            return ServiceResult<List<string>>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve role permissions");
        }
    }

    public async Task<ServiceResult<bool>> AssignPermissionToRoleAsync(string roleId, string permissionId)
    {
        try
        {
            // Check if role exists
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId).ConfigureAwait(false);
            if (role == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "Role not found");
            }

            // Check if permission exists
            var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.Id == permissionId).ConfigureAwait(false);
            if (permission == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "Permission not found");
            }

            // Check if role already has this permission
            var existingRolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId)
                .ConfigureAwait(false);

            if (existingRolePermission != null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.CONFLICT, "Role already has this permission assigned");
            }

            var rolePermission = new RolePermission
            {
                Id = Guid.NewGuid().ToString(),
                RoleId = roleId,
                PermissionId = permissionId
            };

            _context.RolePermissions.Add(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission assigned to role successfully", new { RoleId = roleId, PermissionId = permissionId });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error assigning permission to role", ex, new { roleId, permissionId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to assign permission to role");
        }
    }

    public async Task<ServiceResult<bool>> RemovePermissionFromRoleAsync(string roleId, string permissionId)
    {
        try
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "Role permission assignment not found");
            }

            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Permission removed from role successfully", new { RoleId = roleId, PermissionId = permissionId });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error removing permission from role", ex, new { roleId, permissionId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to remove permission from role");
        }
    }

    public async Task<ServiceResult<bool>> UpdateRolePermissionsAsync(string roleId, List<string> permissionIds)
    {
        try
        {
            if (permissionIds == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.BAD_REQUEST, "PermissionIds cannot be null");
            }
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId).ConfigureAwait(false);

            if (role == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "Role not found");
            }

            // Remove existing permissions
            _context.RolePermissions.RemoveRange(role.RolePermissions);

            // Add new permissions
            if (permissionIds.Count > 0)
            {
                var permissions = await _context.Permissions
                    .Where(p => permissionIds.Contains(p.Id))
                    .ToListAsync().ConfigureAwait(false);

                foreach (var permission in permissions)
                {
                    var rolePermission = new RolePermission
                    {
                        Id = Guid.NewGuid().ToString(),
                        RoleId = roleId,
                        PermissionId = permission.Id
                    };
                    _context.RolePermissions.Add(rolePermission);
                }
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Role permissions updated successfully", new { RoleId = roleId, PermissionCount = permissionIds.Count });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating role permissions", ex, new { roleId, permissionIds });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to update role permissions");
        }
    }

    #endregion

    #region User Management

    public async Task<ServiceResult<PagedUsersResponse>> GetUsersAsync(GetUsersRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<PagedUsersResponse>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var query = _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var term = request.Search.Trim();
                query = query.Where(u =>
                    u.UserName.Contains(term) ||
                    u.Email.Contains(term) ||
                    u.FirstName.Contains(term) ||
                    u.LastName.Contains(term));
            }

            if (request.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == request.IsActive.Value);
            }

            if (request.IsEmailConfirmed.HasValue)
            {
                query = query.Where(u => u.IsEmailConfirmed == request.IsEmailConfirmed.Value);
            }

            if (!string.IsNullOrEmpty(request.RoleId))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == request.RoleId));
            }

            if (!string.IsNullOrEmpty(request.CategoryId))
            {
                query = query.Where(u => u.UserCategories.Any(uc => uc.CategoryId == request.CategoryId));
            }

            query = query.OrderBy(u => u.UserName);

            var totalCount = await query.CountAsync();
            var users = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var userDtos = _mapper.Map<List<UserDto>>(users);

            // Manuel PhoneNumber mapping - AutoMapper sorununu bypass et
            for (int i = 0; i < users.Count; i++)
            {
                userDtos[i].PhoneNumber = users[i].PhoneNumber;
            }

            var firstPhone = userDtos.FirstOrDefault()?.PhoneNumber ?? string.Empty;
            _logger.LogInformation("Mapped {UserCount} users. First user PhoneNumber: '{PhoneNumber}'",
                userDtos.Count,
                firstPhone);

            // Fill permissions based on roles
            var roleIds = users.SelectMany(u => u.UserRoles.Select(ur => ur.RoleId)).Distinct().ToList();
            var rolePermissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.IsActive && roleIds.Contains(rp.RoleId))
                .ToListAsync();

            var roleIdToPerms = rolePermissions
                .GroupBy(rp => rp.RoleId)
                .ToDictionary(g => g.Key, g => g.Select(rp => rp.Permission.Name).Distinct().ToList());

            var roleIdToName = await _context.Roles
                .Where(r => roleIds.Contains(r.Id))
                .ToDictionaryAsync(r => r.Id, r => r.Name);

            foreach (var dto in userDtos)
            {
                // dto.Roles already filled with names by mapping
                var userRoleIds = users.First(u => u.Id == dto.Id).UserRoles.Select(ur => ur.RoleId).ToList();
                var perms = userRoleIds
                    .SelectMany(rid => roleIdToPerms.ContainsKey(rid) ? roleIdToPerms[rid] : Enumerable.Empty<string>())
                    .Distinct()
                    .ToList();
                dto.Permissions = perms;
                // Ensure roles as names (already by mapping), categories already mapped by names
            }

            return ServiceResult<PagedUsersResponse>.Success(new PagedUsersResponse
            {
                Users = userDtos,
                TotalCount = totalCount,
                CurrentPage = request.Page,
                PageSize = request.PageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting users", ex, request);
            return ServiceResult<PagedUsersResponse>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve users");
        }
    }

    public async Task<ServiceResult<UserDto?>> GetUserByIdAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<UserDto?>.Failure(ErrorCodes.NOT_FOUND, "User not found");
            }

            var dto = _mapper.Map<UserDto>(user);

            // Manuel PhoneNumber mapping - AutoMapper sorununu bypass et
            dto.PhoneNumber = user.PhoneNumber;

            _logger.LogWarning("DEBUG GetUserByIdAsync: user.PhoneNumber = '{UserPhone}', dto.PhoneNumber = '{DtoPhone}', UserName = '{UserName}'",
                user.PhoneNumber ?? "NULL", dto.PhoneNumber ?? "NULL", user.UserName);
            _logger.LogInformation("GetUserByIdAsync: Mapped user '{UserName}' with PhoneNumber: '{PhoneNumber}' (Manual: '{ManualPhone}')",
                user.UserName ?? string.Empty, dto.PhoneNumber ?? string.Empty, user.PhoneNumber ?? string.Empty);

            var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
            var permissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.IsActive && roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();
            dto.Permissions = permissions;

            return ServiceResult<UserDto?>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting user by id", ex, new { userId });
            return ServiceResult<UserDto?>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve user");
        }
    }

    public async Task<ServiceResult<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        // 1. Transaction'ı başlat
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (request == null)
            {
                return ServiceResult<UserDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            // Request'i detaylı logla
            _logger.LogInformation("CreateUserAsync called with request: {@Request}", new
            {
                request.UserName,
                request.Email,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.IsActive,
                request.IsEmailConfirmed,
                RoleIds = request.RoleIds ?? new List<string>(),
                CategoryIds = request.CategoryIds ?? new List<string>(),
            });

            // 2. Kullanıcı adı ve e-posta kontrolü
            if (await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                return ServiceResult<UserDto>.Failure("Kullanıcı adı zaten mevcut", ErrorCodes.CONFLICT);
            }
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return ServiceResult<UserDto>.Failure("E-posta adresi zaten mevcut", ErrorCodes.CONFLICT);
            }

            // 3. Yeni kullanıcı oluşturma
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = _passwordService.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                IsActive = request.IsActive,
                IsEmailConfirmed = request.IsEmailConfirmed,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync().ConfigureAwait(false); // Kullanıcıyı hemen kaydet ki ID oluşsun

            // 4. Rolleri ve Kategorileri işle (ayrı metotlar)
            await AssignRolesToUserInternalAsync(user, request.RoleIds ?? new List<string>()).ConfigureAwait(false);
            await AssignCategoriesToUserInternalAsync(user, request.CategoryIds ?? new List<string>()).ConfigureAwait(false);

            await _context.SaveChangesAsync().ConfigureAwait(false); // Rol ve Kategori atamalarını kaydet

            // 5. Başarılı sonuç için DTO hazırla (commit'ten önce mapping tamamlanmalı)
            var createdUser = await _context.Users
                .AsNoTracking() // Takip etmeye gerek yok, sadece okuma
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .FirstAsync(u => u.Id == user.Id);

            var dto = _mapper.Map<UserDto>(createdUser);

            // Permissions'ları ayrıca yükle
            var roleIds = createdUser.UserRoles.Select(ur => ur.RoleId).ToList();
            dto.Permissions = await GetPermissionsForRolesAsync(roleIds);

            // 6. Transaction'ı onayla (mapping ve permissions başarılı ise)
            await transaction.CommitAsync();

            _logger.LogInformation("User created successfully", new { user.Id, user.UserName, user.Email });
            return ServiceResult<UserDto>.Success(dto);
        }
        catch (Exception ex)
        {
            // Hata durumunda Transaction'ı geri al
            await transaction.RollbackAsync().ConfigureAwait(false);

            _logger.LogError("Error creating user, transaction rolled back. Error: {ErrorMessage} | StackTrace: {StackTrace}",
                ex.Message ?? string.Empty, ex.StackTrace ?? string.Empty);

            if (ex is BusinessException brex)
            {
                return ServiceResult<UserDto>.Failure(ErrorCodes.BAD_REQUEST, brex.Message);
            }
            // AutoMapper hatası olup olmadığını kontrol et
            if (ex is AutoMapperMappingException)
            {
                _logger.LogError("AutoMapper mapping failed during user creation: {@Exception}", ex);
                return ServiceResult<UserDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Kullanıcı oluşturuldu ancak sonuç döndürülürken bir haritalama hatası oluştu.");
            }

            return ServiceResult<UserDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Kullanıcı oluşturulurken beklenmedik bir hata oluştu.");
        }
    }

    private async Task AssignRolesToUserInternalAsync(User user, List<string> roleIds)
    {
        if (roleIds == null || !roleIds.Any()) return;

        var distinctRoleIds = roleIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var existingRoles = await _context.Roles
            .Where(r => distinctRoleIds.Contains(r.Id))
            .ToListAsync();

        var missingRoleIds = distinctRoleIds.Except(existingRoles.Select(r => r.Id)).ToList();
        if (missingRoleIds.Any())
        {
            // Bu bir exception fırlatmalı çünkü transaction içinde yakalanmalı
            throw BusinessException.BadRequest($"Geçersiz rol(ler) bulundu: {string.Join(", ", missingRoleIds)}");
        }

        foreach (var role in existingRoles)
        {
            _context.UserRoles.Add(new UserRole
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                RoleId = role.Id,
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            });
        }
    }

    private async Task AssignCategoriesToUserInternalAsync(User user, List<string> categoryIds)
    {
        if (categoryIds == null || !categoryIds.Any()) return;

        var distinctCategoryIds = categoryIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        var existingCategories = await _context.Categories
            .Where(c => distinctCategoryIds.Contains(c.Id))
            .ToListAsync();

        var missingCategoryIds = distinctCategoryIds.Except(existingCategories.Select(c => c.Id)).ToList();
        if (missingCategoryIds.Any())
        {
            throw BusinessException.BadRequest($"Geçersiz kategori(ler) bulundu: {string.Join(", ", missingCategoryIds)}");
        }

        foreach (var category in existingCategories)
        {
            _context.UserCategories.Add(new UserCategory
            {
                Id = Guid.NewGuid().ToString(),
                UserId = user.Id,
                CategoryId = category.Id,
                IsActive = true,
                AssignedAt = DateTime.UtcNow
            });
        }
    }

    private async Task<List<string>> GetPermissionsForRolesAsync(List<string> roleIds)
    {
        if (roleIds == null || !roleIds.Any())
        {
            return new List<string>();
        }

        return await _context.RolePermissions
            .AsNoTracking()
            .Include(rp => rp.Permission)
            .Where(rp => rp.IsActive && roleIds.Contains(rp.RoleId))
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync();
    }

    public async Task<ServiceResult<UserDto>> UpdateUserAsync(string userId, UpdateUserRequest request)
    {
        try
        {
            if (request == null)
            {
                return ServiceResult<UserDto>.Failure(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            await using var transaction = await _context.Database.BeginTransactionAsync();
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<UserDto>.Failure(ErrorCodes.NOT_FOUND, "User not found");
            }

            if (!string.Equals(user.UserName, request.UserName, StringComparison.OrdinalIgnoreCase) &&
                await _context.Users.AnyAsync(u => u.UserName == request.UserName))
            {
                return ServiceResult<UserDto>.Failure("Kullanıcı adı zaten mevcut", ErrorCodes.CONFLICT);
            }
            if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase) &&
                await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                return ServiceResult<UserDto>.Failure("E-posta adresi zaten mevcut", ErrorCodes.CONFLICT);
            }

            _logger.LogInformation("Updating user with PhoneNumber: '{PhoneNumber}' (was: '{OldPhoneNumber}')", request.PhoneNumber ?? string.Empty, user.PhoneNumber ?? string.Empty);

            user.UserName = request.UserName;
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.IsActive = request.IsActive;
            user.IsEmailConfirmed = request.IsEmailConfirmed;
            user.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("User updated. PhoneNumber is now: '{PhoneNumber}'", user.PhoneNumber ?? string.Empty);

            await _context.SaveChangesAsync();

            // Sync roles if provided (treat empty list as clear all)
            if (request.RoleIds != null)
            {
                var requestedRoleIds = request.RoleIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToList();

                var validRoleIds = await _context.Roles
                    .Where(r => requestedRoleIds.Contains(r.Id))
                    .Select(r => r.Id)
                    .ToListAsync();

                var missingRoleIds = requestedRoleIds.Except(validRoleIds).ToList();
                if (missingRoleIds.Any())
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<UserDto>.Failure(ErrorCodes.BAD_REQUEST,
                        $"Geçersiz rol(ler): {string.Join(", ", missingRoleIds)}");
                }

                var currentRoleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
                var toRemoveRoleIds = currentRoleIds.Except(validRoleIds).ToList();
                var toAddRoleIds = validRoleIds.Except(currentRoleIds).ToList();

                if (toRemoveRoleIds.Any())
                {
                    var removeUserRoles = _context.UserRoles.Where(ur => ur.UserId == user.Id && toRemoveRoleIds.Contains(ur.RoleId));
                    _context.UserRoles.RemoveRange(removeUserRoles);
                }
                foreach (var addId in toAddRoleIds)
                {
                    _context.UserRoles.Add(new UserRole
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        RoleId = addId,
                        IsActive = true,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }

            // Sync categories if provided (treat empty list as clear all)
            if (request.CategoryIds != null)
            {
                var requestedCategoryIds = request.CategoryIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct()
                    .ToList();

                var validCategoryIds = await _context.Categories
                    .Where(c => requestedCategoryIds.Contains(c.Id))
                    .Select(c => c.Id)
                    .ToListAsync();

                var missingCategoryIds = requestedCategoryIds.Except(validCategoryIds).ToList();
                if (missingCategoryIds.Any())
                {
                    await transaction.RollbackAsync();
                    return ServiceResult<UserDto>.Failure(ErrorCodes.BAD_REQUEST,
                        $"Geçersiz kategori(ler): {string.Join(", ", missingCategoryIds)}");
                }

                var currentCategoryIds = user.UserCategories.Select(uc => uc.CategoryId).ToList();
                var toRemoveCategoryIds = currentCategoryIds.Except(validCategoryIds).ToList();
                var toAddCategoryIds = validCategoryIds.Except(currentCategoryIds).ToList();

                if (toRemoveCategoryIds.Any())
                {
                    var removeUserCategories = _context.UserCategories.Where(uc => uc.UserId == user.Id && toRemoveCategoryIds.Contains(uc.CategoryId));
                    _context.UserCategories.RemoveRange(removeUserCategories);
                }
                foreach (var addId in toAddCategoryIds)
                {
                    _context.UserCategories.Add(new UserCategory
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = user.Id,
                        CategoryId = addId,
                        IsActive = true,
                        AssignedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // Reload with latest relations
            var updatedUser = await _context.Users
                .AsNoTracking()
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .FirstAsync(u => u.Id == user.Id);
            var dto = _mapper.Map<UserDto>(updatedUser);

            var roleIds = updatedUser.UserRoles.Select(ur => ur.RoleId).ToList();
            var permissions = await _context.RolePermissions
                .Include(rp => rp.Permission)
                .Where(rp => rp.IsActive && roleIds.Contains(rp.RoleId))
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .ToListAsync();
            dto.Permissions = permissions;

            _logger.LogInformation("User updated successfully", new { user.Id, user.UserName, user.Email });
            await transaction.CommitAsync();
            return ServiceResult<UserDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error updating user", ex, new { userId, request });
            return ServiceResult<UserDto>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to update user");
        }
    }

    public async Task<ServiceResult<bool>> DeactivateUserAsync(string userId)
    {
        try
        {
            var auth = new AuthService(_context, _passwordService, null!, _mapper, _logger, null!);
            var result = await auth.DeactivateUserAsync(userId);
            return result.Success ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.Error?.Message ?? "Failed to deactivate user");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deactivating user", ex, new { userId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to deactivate user");
        }
    }

    public async Task<ServiceResult<bool>> ActivateUserAsync(string userId)
    {
        try
        {
            var auth = new AuthService(_context, _passwordService, null!, _mapper, _logger, null!);
            var result = await auth.ActivateUserAsync(userId);
            return result.Success ? ServiceResult<bool>.Success(true) : ServiceResult<bool>.Failure(result.Error?.Message ?? "Failed to activate user");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error activating user", ex, new { userId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to activate user");
        }
    }

    public async Task<ServiceResult<bool>> DeleteUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .Include(u => u.UserCategories)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "User not found");
            }

            // Remove related refresh tokens
            var tokens = await _context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync();
            _context.RefreshTokens.RemoveRange(tokens);

            // Remove role assignments
            _context.UserRoles.RemoveRange(user.UserRoles);

            // Remove category assignments
            _context.UserCategories.RemoveRange(user.UserCategories);

            // Finally remove user
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User deleted successfully", new { UserId = userId, UserName = user.UserName });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error deleting user", ex, new { userId });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to delete user");
        }
    }

    #endregion

    #region Refresh Token Management

    public async Task<ServiceResult<List<RefreshTokenDto>>> GetActiveRefreshTokensAsync(string? userId = null, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.RefreshTokens
                .Include(rt => rt.User)
                .Where(rt => !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(rt => rt.UserId == userId);
            }

            query = query.OrderByDescending(rt => rt.CreatedAt);

            var refreshTokens = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var refreshTokenDtos = refreshTokens.Select(rt => new RefreshTokenDto
            {
                Id = rt.Id,
                Token = rt.Token[..8] + "...", // Only show first 8 characters for security
                UserId = rt.UserId,
                UserName = rt.User.UserName,
                ExpiresAt = rt.ExpiresAt,
                IsRevoked = rt.IsRevoked,
                RevokedAt = rt.RevokedAt,
                DeviceId = rt.DeviceId,
                IpAddress = rt.IpAddress,
                UserAgent = rt.UserAgent,
                CreatedAt = rt.CreatedAt,
                IsExpired = rt.ExpiresAt <= DateTime.UtcNow,
                IsActive = !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow
            }).ToList();

            return ServiceResult<List<RefreshTokenDto>>.Success(refreshTokenDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting active refresh tokens", ex, new { userId, page, pageSize });
            return ServiceResult<List<RefreshTokenDto>>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to retrieve refresh tokens");
        }
    }

    public async Task<ServiceResult<bool>> RevokeRefreshTokenAsync(string tokenId, string reason)
    {
        try
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Id == tokenId);

            if (token == null)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.NOT_FOUND, "Refresh token not found");
            }

            if (token.IsRevoked)
            {
                return ServiceResult<bool>.Failure(ErrorCodes.CONFLICT, "Token is already revoked");
            }

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedReason = reason;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Refresh token revoked successfully", new { TokenId = tokenId, Reason = reason });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error revoking refresh token", ex, new { tokenId, reason });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to revoke refresh token");
        }
    }

    public async Task<ServiceResult<bool>> RevokeAllUserTokensAsync(string userId, string reason)
    {
        try
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                token.RevokedReason = reason;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("All user tokens revoked successfully", new { UserId = userId, TokenCount = tokens.Count, Reason = reason });
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error revoking all user tokens", ex, new { userId, reason });
            return ServiceResult<bool>.Failure(ErrorCodes.INTERNAL_SERVER_ERROR, "Failed to revoke user tokens");
        }
    }

    #endregion
}