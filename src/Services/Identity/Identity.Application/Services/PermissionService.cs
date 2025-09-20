using Identity.Core.Interfaces;
using Identity.Core.DTOs;
using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;
using Microsoft.Extensions.Logging;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Identity.Infrastructure.UnitOfWork;

namespace Identity.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionService> _logger;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public PermissionService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<PermissionService> logger,
        IMemoryCache cache)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _cache = cache;
    }

    public async Task<Result<PermissionDto>> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"permission:{permissionId}";

            if (_cache.TryGetValue<PermissionDto>(cacheKey, out var cachedPermission))
            {
                _logger.LogDebug("Permission {PermissionId} retrieved from cache", permissionId);
                return Result<PermissionDto>.Success(cachedPermission!);
            }

            var permission = await _unitOfWork.Permissions.GetByIdAsync(permissionId, cancellationToken);

            if (permission == null)
            {
                return Result<PermissionDto>.Failure("İzin bulunamadı");
            }

            var dto = _mapper.Map<PermissionDto>(permission);
            dto.RoleCount = await _unitOfWork.Permissions.GetRoleCountAsync(permissionId, cancellationToken);
            dto.UserCount = await _unitOfWork.Permissions.GetUserCountAsync(permissionId, cancellationToken);

            _cache.Set(cacheKey, dto, _cacheExpiration);

            return Result<PermissionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission by id {PermissionId}", permissionId);
            return Result<PermissionDto>.Failure("İzin getirilemedi");
        }
    }

    public async Task<Result<PermissionDto>> GetByNameAsync(string name, Guid serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"permission:{serviceId}:{name}";

            if (_cache.TryGetValue<PermissionDto>(cacheKey, out var cachedPermission))
            {
                _logger.LogDebug("Permission {Name} for service {ServiceId} retrieved from cache", name, serviceId);
                return Result<PermissionDto>.Success(cachedPermission!);
            }

            var permission = await _unitOfWork.Permissions.GetByNameAsync(name, serviceId, cancellationToken);

            if (permission == null)
            {
                return Result<PermissionDto>.Failure("İzin bulunamadı");
            }

            var dto = _mapper.Map<PermissionDto>(permission);
            dto.RoleCount = await _unitOfWork.Permissions.GetRoleCountAsync(permission.Id, cancellationToken);
            dto.UserCount = await _unitOfWork.Permissions.GetUserCountAsync(permission.Id, cancellationToken);

            _cache.Set(cacheKey, dto, _cacheExpiration);

            return Result<PermissionDto>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission by name {Name}", name);
            return Result<PermissionDto>.Failure("İzin getirilemedi");
        }
    }

    public Task<Result<PagedResult<PermissionDto>>> GetPermissionsAsync(int page = 1, int pageSize = 10, string? search = null, Guid? serviceId = null, PermissionType? type = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            var emptyResult = new PagedResult<PermissionDto>(new List<PermissionDto>(), 0, page, pageSize);
            return Task.FromResult(Result<PagedResult<PermissionDto>>.Success(emptyResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permissions");
            return Task.FromResult(Result<PagedResult<PermissionDto>>.Failure("İzinler getirilemedi"));
        }
    }

    public Task<Result<IEnumerable<PermissionDto>>> GetServicePermissionsAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            return Task.FromResult(Result<IEnumerable<PermissionDto>>.Success(new List<PermissionDto>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service permissions for {ServiceId}", serviceId);
            return Task.FromResult(Result<IEnumerable<PermissionDto>>.Failure("Servis izinleri getirilemedi"));
        }
    }

    public Task<Result<PermissionDto>> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            return Task.FromResult(Result<PermissionDto>.Failure("İzin oluşturulamadı"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission {Name}", request.Name);
            return Task.FromResult(Result<PermissionDto>.Failure("İzin oluşturulamadı"));
        }
    }

    public Task<Result<PermissionDto>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            return Task.FromResult(Result<PermissionDto>.Failure("İzin güncellenemedi"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionId}", permissionId);
            return Task.FromResult(Result<PermissionDto>.Failure("İzin güncellenemedi"));
        }
    }

    public Task<Result<bool>> DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission {PermissionId}", permissionId);
            return Task.FromResult(Result<bool>.Failure("İzin silinemedi"));
        }
    }

    public async Task<Result<PermissionCheckResponse>> CheckPermissionAsync(PermissionCheckRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"perm:check:{request.UserId}:{request.Permission}:{request.GroupId}";

            if (_cache.TryGetValue<PermissionCheckResponse>(cacheKey, out var cachedResponse))
            {
                _logger.LogDebug("Permission check for user {UserId} retrieved from cache", request.UserId);
                return Result<PermissionCheckResponse>.Success(cachedResponse!);
            }

            var userPermissions = await _unitOfWork.Permissions.GetUserPermissionsAsync(
                request.UserId,
                request.GroupId,
                cancellationToken);

            var hasPermission = userPermissions.Any(p =>
                p.Name == request.Permission &&
                (string.IsNullOrEmpty(request.Resource) || p.Resource == request.Resource));

            var response = new PermissionCheckResponse
            {
                IsAllowed = hasPermission,
                Reason = hasPermission ? "Permission granted" : "Permission denied",
                CheckedAt = DateTime.UtcNow
            };

            var shortCacheExpiration = TimeSpan.FromMinutes(2);
            _cache.Set(cacheKey, response, shortCacheExpiration);

            _logger.LogInformation(
                "Permission check for user {UserId} on {Permission}: {Result}",
                request.UserId, request.Permission, hasPermission ? "Allowed" : "Denied");

            return Result<PermissionCheckResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}", request.UserId);
            return Result<PermissionCheckResponse>.Failure("İzin kontrol edilemedi");
        }
    }

    public async Task<Result<BulkPermissionCheckResponse>> CheckPermissionsAsync(BulkPermissionCheckRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var userPermissions = await _unitOfWork.Permissions.GetUserPermissionsAsync(
                request.UserId,
                request.GroupId,
                cancellationToken);

            var userPermissionNames = userPermissions.Select(p => p.Name).ToHashSet();

            var results = request.Permissions.ToDictionary(
                permission => permission,
                permission => userPermissionNames.Contains(permission)
            );

            var response = new BulkPermissionCheckResponse
            {
                Results = results,
                CheckedAt = DateTime.UtcNow
            };

            _logger.LogInformation(
                "Bulk permission check for user {UserId}: {Granted}/{Total} permissions granted",
                request.UserId, results.Count(r => r.Value), results.Count);

            return Result<BulkPermissionCheckResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permissions for user {UserId}", request.UserId);
            return Result<BulkPermissionCheckResponse>.Failure("İzinler kontrol edilemedi");
        }
    }

    public async Task<Result<IEnumerable<PermissionDto>>> GetUserPermissionsAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"perm:user:{userId}:{groupId}";

            if (_cache.TryGetValue<IEnumerable<PermissionDto>>(cacheKey, out var cachedPermissions))
            {
                _logger.LogDebug("User permissions for {UserId} retrieved from cache", userId);
                return Result<IEnumerable<PermissionDto>>.Success(cachedPermissions!);
            }

            var permissions = await _unitOfWork.Permissions.GetUserPermissionsAsync(
                userId,
                groupId,
                cancellationToken);

            var permissionDtos = permissions.Select(p =>
            {
                var dto = _mapper.Map<PermissionDto>(p);
                return dto;
            }).ToList();

            _cache.Set(cacheKey, permissionDtos, _cacheExpiration);

            _logger.LogInformation(
                "Retrieved {Count} permissions for user {UserId}",
                permissionDtos.Count, userId);

            return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for {UserId}", userId);
            return Result<IEnumerable<PermissionDto>>.Failure("Kullanıcı izinleri getirilemedi");
        }
    }

    public async Task<Result<IEnumerable<PermissionDto>>> GetRolePermissionsAsync(string roleId, Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"perm:role:{roleId}:{groupId}";

            if (_cache.TryGetValue<IEnumerable<PermissionDto>>(cacheKey, out var cachedPermissions))
            {
                _logger.LogDebug("Role permissions for {RoleId} retrieved from cache", roleId);
                return Result<IEnumerable<PermissionDto>>.Success(cachedPermissions!);
            }

            var permissions = await _unitOfWork.Permissions.GetRolePermissionsAsync(
                roleId,
                groupId,
                cancellationToken);

            var permissionDtos = permissions.Select(p =>
            {
                var dto = _mapper.Map<PermissionDto>(p);
                return dto;
            }).ToList();

            var longCacheExpiration = TimeSpan.FromMinutes(10);
            _cache.Set(cacheKey, permissionDtos, longCacheExpiration);

            _logger.LogInformation(
                "Retrieved {Count} permissions for role {RoleId}",
                permissionDtos.Count, roleId);

            return Result<IEnumerable<PermissionDto>>.Success(permissionDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role permissions for {RoleId}", roleId);
            return Result<IEnumerable<PermissionDto>>.Failure("Rol izinleri getirilemedi");
        }
    }

    public Task<Result<bool>> AssignPermissionToRoleAsync(string roleId, Guid permissionId, Guid? groupId = null, string? conditions = null, DateTime? validFrom = null, DateTime? validUntil = null, string? grantedBy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);
            return Task.FromResult(Result<bool>.Failure("İzin role atanamadı"));
        }
    }

    public Task<Result<bool>> RemovePermissionFromRoleAsync(string roleId, Guid permissionId, Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission {PermissionId} from role {RoleId}", permissionId, roleId);
            return Task.FromResult(Result<bool>.Failure("İzin rolden kaldırılamadı"));
        }
    }

    public Task<Result<PermissionMatrixDto>> GetPermissionMatrixAsync(Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            var matrix = new PermissionMatrixDto
            {
                Services = new List<ServicePermissionGroup>(),
                Roles = new List<RolePermissionGroup>(),
                GeneratedAt = DateTime.UtcNow
            };
            return Task.FromResult(Result<PermissionMatrixDto>.Success(matrix));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting permission matrix for group {GroupId}", groupId);
            return Task.FromResult(Result<PermissionMatrixDto>.Failure("İzin matrisi getirilemedi"));
        }
    }

    public Task<Result<IEnumerable<PermissionDto>>> GetEffectivePermissionsAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation
            return Task.FromResult(Result<IEnumerable<PermissionDto>>.Success(new List<PermissionDto>()));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting effective permissions for user {UserId}", userId);
            return Task.FromResult(Result<IEnumerable<PermissionDto>>.Failure("Etkin izinler getirilemedi"));
        }
    }

    public async Task<Result<bool>> HasPermissionAsync(string userId, string permission, Guid? groupId = null, string? resource = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"perm:has:{userId}:{permission}:{groupId}:{resource}";

            if (_cache.TryGetValue<bool>(cacheKey, out var cachedResult))
            {
                _logger.LogDebug("HasPermission check for user {UserId} retrieved from cache", userId);
                return Result<bool>.Success(cachedResult);
            }

            var userPermissions = await _unitOfWork.Permissions.GetUserPermissionsAsync(
                userId,
                groupId,
                cancellationToken);

            var hasPermission = userPermissions.Any(p =>
                p.Name == permission &&
                p.IsActive &&
                (string.IsNullOrEmpty(resource) || p.Resource == resource));

            var shortCacheExpiration = TimeSpan.FromMinutes(2);
            _cache.Set(cacheKey, hasPermission, shortCacheExpiration);

            _logger.LogDebug(
                "HasPermission check for user {UserId} on {Permission}: {Result}",
                userId, permission, hasPermission);

            return Result<bool>.Success(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} has permission {Permission}", userId, permission);
            return Result<bool>.Failure("İzin kontrol edilemedi");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetUserPermissionNamesAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"perm:names:{userId}:{groupId}";

            if (_cache.TryGetValue<IEnumerable<string>>(cacheKey, out var cachedNames))
            {
                _logger.LogDebug("User permission names for {UserId} retrieved from cache", userId);
                return Result<IEnumerable<string>>.Success(cachedNames!);
            }

            var userPermissions = await _unitOfWork.Permissions.GetUserPermissionsAsync(
                userId,
                groupId,
                cancellationToken);

            var permissionNames = userPermissions
                .Where(p => p.IsActive)
                .Select(p => p.Name)
                .Distinct()
                .ToList();

            _cache.Set(cacheKey, permissionNames, _cacheExpiration);

            _logger.LogInformation(
                "Retrieved {Count} permission names for user {UserId}",
                permissionNames.Count, userId);

            return Result<IEnumerable<string>>.Success(permissionNames);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permission names for {UserId}", userId);
            return Result<IEnumerable<string>>.Failure("Kullanıcı izin isimleri getirilemedi");
        }
    }
}