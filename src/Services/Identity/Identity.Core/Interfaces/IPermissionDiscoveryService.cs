using Identity.Core.DTOs;

namespace Identity.Core.Interfaces;

public interface IPermissionDiscoveryService
{
    Task<IEnumerable<PermissionDto>> DiscoverAllPermissionsAsync();
    Task RegisterServicePermissionsAsync(IServicePermissionProvider provider);
    Task<IEnumerable<PermissionDto>> GetServicePermissionsAsync(string serviceName);
}