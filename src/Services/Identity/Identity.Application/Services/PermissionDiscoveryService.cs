using Identity.Core.DTOs;
using Identity.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class PermissionDiscoveryService : IPermissionDiscoveryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<PermissionDiscoveryService> _logger;
    private readonly List<IServicePermissionProvider> _registeredProviders = new();

    public PermissionDiscoveryService(
        IServiceProvider serviceProvider,
        IPermissionService permissionService,
        ILogger<PermissionDiscoveryService> logger)
    {
        _serviceProvider = serviceProvider;
        _permissionService = permissionService;
        _logger = logger;
    }

    public async Task<IEnumerable<PermissionDto>> DiscoverAllPermissionsAsync()
    {
        var allPermissions = new List<PermissionDto>();

        // Auto-discover all registered providers
        var providers = _serviceProvider.GetServices<IServicePermissionProvider>();
        foreach (var provider in providers)
        {
            try
            {
                var permissions = provider.GetPermissions();
                allPermissions.AddRange(permissions);
                _logger.LogInformation("Discovered {Count} permissions from service {ServiceName}",
                    permissions.Count(), provider.ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover permissions from service {ServiceName}", provider.ServiceName);
            }
        }

        // Add manually registered providers
        foreach (var provider in _registeredProviders)
        {
            try
            {
                var permissions = provider.GetPermissions();
                allPermissions.AddRange(permissions);
                _logger.LogInformation("Discovered {Count} permissions from manually registered service {ServiceName}",
                    permissions.Count(), provider.ServiceName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to discover permissions from manually registered service {ServiceName}", provider.ServiceName);
            }
        }

        return allPermissions.DistinctBy(p => p.Name);
    }

    public async Task RegisterServicePermissionsAsync(IServicePermissionProvider provider)
    {
        if (!_registeredProviders.Any(p => p.ServiceName == provider.ServiceName))
        {
            _registeredProviders.Add(provider);
            _logger.LogInformation("Registered permission provider for service: {ServiceName}", provider.ServiceName);
        }
    }

    public async Task<IEnumerable<PermissionDto>> GetServicePermissionsAsync(string serviceName)
    {
        // Check DI registered providers
        var providers = _serviceProvider.GetServices<IServicePermissionProvider>();
        var provider = providers.FirstOrDefault(p => p.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));

        if (provider != null)
        {
            return provider.GetPermissions();
        }

        // Check manually registered providers
        var manualProvider = _registeredProviders.FirstOrDefault(p => p.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        if (manualProvider != null)
        {
            return manualProvider.GetPermissions();
        }

        _logger.LogWarning("No permission provider found for service: {ServiceName}", serviceName);
        return Enumerable.Empty<PermissionDto>();
    }
}