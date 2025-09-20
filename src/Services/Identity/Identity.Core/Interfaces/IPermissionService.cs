using Identity.Core.DTOs;
using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;

namespace Identity.Core.Interfaces;

public interface IPermissionService
{
    Task<Result<PermissionDto>> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<Result<PermissionDto>> GetByNameAsync(string name, Guid serviceId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<PermissionDto>>> GetPermissionsAsync(int page = 1, int pageSize = 10, string? search = null, Guid? serviceId = null, PermissionType? type = null, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<PermissionDto>>> GetServicePermissionsAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<Result<PermissionDto>> CreatePermissionAsync(CreatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<Result<PermissionDto>> UpdatePermissionAsync(Guid permissionId, UpdatePermissionRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeletePermissionAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<Result<PermissionCheckResponse>> CheckPermissionAsync(PermissionCheckRequest request, CancellationToken cancellationToken = default);
    Task<Result<BulkPermissionCheckResponse>> CheckPermissionsAsync(BulkPermissionCheckRequest request, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<PermissionDto>>> GetUserPermissionsAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<PermissionDto>>> GetRolePermissionsAsync(string roleId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> AssignPermissionToRoleAsync(string roleId, Guid permissionId, Guid? groupId = null, string? conditions = null, DateTime? validFrom = null, DateTime? validUntil = null, string? grantedBy = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> RemovePermissionFromRoleAsync(string roleId, Guid permissionId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<Result<PermissionMatrixDto>> GetPermissionMatrixAsync(Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<PermissionDto>>> GetEffectivePermissionsAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> HasPermissionAsync(string userId, string permission, Guid? groupId = null, string? resource = null, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<string>>> GetUserPermissionNamesAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default);
}

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<Permission?> GetByNameAsync(string name, Guid serviceId, CancellationToken cancellationToken = default);
    Task<PagedResult<Permission>> GetPermissionsAsync(int page, int pageSize, string? search = null, Guid? serviceId = null, PermissionType? type = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetServicePermissionsAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<Permission> CreateAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<Permission> UpdateAsync(Permission permission, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<bool> IsNameTakenAsync(string name, Guid serviceId, Guid? excludePermissionId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetUserPermissionsAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Permission>> GetRolePermissionsAsync(string roleId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<RolePermission?> GetRolePermissionAsync(string roleId, Guid permissionId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<RolePermission> AssignPermissionToRoleAsync(RolePermission rolePermission, CancellationToken cancellationToken = default);
    Task<bool> RemovePermissionFromRoleAsync(string roleId, Guid permissionId, Guid? groupId = null, CancellationToken cancellationToken = default);
    Task<int> GetRoleCountAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<int> GetUserCountAsync(Guid permissionId, CancellationToken cancellationToken = default);
}