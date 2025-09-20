using Identity.Core.DTOs;

namespace Identity.Core.Interfaces;

public interface IServicePermissionProvider
{
    string ServiceName { get; }
    IEnumerable<PermissionDto> GetPermissions();
    IEnumerable<string> GetResources();
    IEnumerable<string> GetActions();
}