using Microsoft.AspNetCore.Mvc;
using Gateway.Core.Interfaces;
using Enterprise.Shared.Common.Models;

namespace Gateway.API.Controllers;

/// <summary>
/// Health check controller for Gateway service
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Health")]
public class HealthController : ControllerBase
{
    private readonly IServiceHealthService _healthService;
    private readonly ILogger<HealthController> _logger;

    public HealthController(
        IServiceHealthService healthService,
        ILogger<HealthController> logger)
    {
        _healthService = healthService;
        _logger = logger;
    }

    /// <summary>
    /// Get gateway health status
    /// </summary>
    /// <returns>Gateway health information</returns>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult GetHealth()
    {
        var healthInfo = new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "API Gateway",
            Version = "1.0.0",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
        };

        _logger.LogInformation("Gateway health check requested");
        return Ok(healthInfo);
    }

    /// <summary>
    /// Get detailed health status including downstream services
    /// </summary>
    /// <returns>Detailed health information</returns>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetDetailedHealth()
    {
        try
        {
            var serviceHealthResult = await _healthService.GetAllServiceHealthAsync();
            
            var overallHealthy = true;
            var services = new List<object>();

            if (serviceHealthResult.IsSuccess && serviceHealthResult.Value != null)
            {
                foreach (var service in serviceHealthResult.Value)
                {
                    services.Add(new
                    {
                        Name = service.ServiceName,
                        Status = service.IsHealthy ? "Healthy" : "Unhealthy",
                        Endpoint = service.Endpoint,
                        ResponseTime = service.ResponseTime.TotalMilliseconds,
                        Message = service.Message,
                        CheckedAt = service.CheckedAt
                    });

                    if (!service.IsHealthy)
                        overallHealthy = false;
                }
            }
            else
            {
                overallHealthy = false;
            }

            var healthInfo = new
            {
                Status = overallHealthy ? "Healthy" : "Degraded",
                Timestamp = DateTime.UtcNow,
                Service = "API Gateway",
                Version = "1.0.0",
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development",
                DownstreamServices = services
            };

            _logger.LogInformation("Detailed health check completed. Overall status: {Status}", 
                overallHealthy ? "Healthy" : "Degraded");

            if (overallHealthy)
                return Ok(healthInfo);
            else
                return StatusCode(StatusCodes.Status503ServiceUnavailable, healthInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during detailed health check");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Service = "API Gateway",
                Error = "Health check failed"
            });
        }
    }

    /// <summary>
    /// Check health of a specific service
    /// </summary>
    /// <param name="serviceName">Name of the service to check</param>
    /// <returns>Service health status</returns>
    [HttpGet("service/{serviceName}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public Task<IActionResult> GetServiceHealth(string serviceName)
    {
        try
        {
            // This would need to be implemented based on configured services
            // For now, return a simple response
            var healthInfo = new
            {
                ServiceName = serviceName,
                Status = "Not Implemented",
                Timestamp = DateTime.UtcNow,
                Message = "Service health check not yet implemented"
            };

            return Task.FromResult<IActionResult>(Ok(healthInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking health for service {ServiceName}", serviceName);
            return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                ServiceName = serviceName,
                Status = "Error",
                Timestamp = DateTime.UtcNow,
                Error = "Service health check failed"
            }));
        }
    }
}