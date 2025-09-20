using Identity.Core.DTOs;
using Identity.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Enterprise.Shared.Authorization.Attributes;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/permission-discovery")]
[Authorize(Policy = "permission:permissions.write")]
public class PermissionDiscoveryController : ControllerBase
{
    private readonly IPermissionDiscoveryService _permissionDiscoveryService;
    private readonly ILogger<PermissionDiscoveryController> _logger;

    public PermissionDiscoveryController(
        IPermissionDiscoveryService permissionDiscoveryService,
        ILogger<PermissionDiscoveryController> logger)
    {
        _permissionDiscoveryService = permissionDiscoveryService;
        _logger = logger;
    }

    /// <summary>
    /// Discovers all permissions from registered service providers
    /// </summary>
    [HttpPost("discover")]
    public async Task<IActionResult> DiscoverPermissions()
    {
        try
        {
            var discoveredPermissions = await _permissionDiscoveryService.DiscoverAllPermissionsAsync();

            _logger.LogInformation("Discovered {Count} permissions from all services", discoveredPermissions.Count());

            return Ok(new
            {
                Success = true,
                Message = $"Successfully discovered {discoveredPermissions.Count()} permissions",
                Data = new
                {
                    TotalPermissions = discoveredPermissions.Count(),
                    PermissionsByService = discoveredPermissions
                        .GroupBy(p => p.ServiceName)
                        .Select(g => new
                        {
                            ServiceName = g.Key,
                            Count = g.Count(),
                            Permissions = g.ToList()
                        })
                        .ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover permissions");
            return StatusCode(500, new
            {
                Success = false,
                Message = "Failed to discover permissions",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Gets permissions for a specific service
    /// </summary>
    [HttpGet("services/{serviceName}/permissions")]
    [Authorize(Policy = "permission:permissions.read")]
    public async Task<IActionResult> GetServicePermissions(string serviceName)
    {
        try
        {
            var permissions = await _permissionDiscoveryService.GetServicePermissionsAsync(serviceName);

            return Ok(new
            {
                Success = true,
                Data = new
                {
                    ServiceName = serviceName,
                    Count = permissions.Count(),
                    Permissions = permissions.ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get permissions for service {ServiceName}", serviceName);
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Failed to get permissions for service {serviceName}",
                Error = ex.Message
            });
        }
    }

    /// <summary>
    /// Manually registers a service permission provider
    /// </summary>
    [HttpPost("services/{serviceName}/register")]
    public async Task<IActionResult> RegisterServiceProvider(string serviceName, [FromBody] ServicePermissionRegistrationRequest request)
    {
        try
        {
            // This would need a custom implementation in PermissionDiscoveryService
            // For now, return a placeholder response

            _logger.LogInformation("Service permission provider registration requested for {ServiceName}", serviceName);

            return Ok(new
            {
                Success = true,
                Message = $"Service {serviceName} provider registration initiated",
                Data = new { ServiceName = serviceName, Status = "Pending" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register service provider for {ServiceName}", serviceName);
            return StatusCode(500, new
            {
                Success = false,
                Message = $"Failed to register service provider for {serviceName}",
                Error = ex.Message
            });
        }
    }
}

public class ServicePermissionRegistrationRequest
{
    public string ServiceName { get; set; } = string.Empty;
    public string ServiceDisplayName { get; set; } = string.Empty;
    public string ServiceEndpoint { get; set; } = string.Empty;
    public List<string> Resources { get; set; } = new();
    public List<string> Actions { get; set; } = new();
}