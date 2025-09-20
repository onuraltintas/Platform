using Gateway.Core.Interfaces;
using Gateway.Core.Models;
using Gateway.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Enterprise.Shared.Common.Models;
using System.Text.Json;

namespace Gateway.Core.Services;

public class ServiceDiscoveryService : IServiceDiscoveryService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceDiscoveryService> _logger;
    private readonly GatewayOptions _options;
    private readonly Dictionary<string, List<ServiceEndpoint>> _serviceRegistry;
    private readonly SemaphoreSlim _registryLock;

    public ServiceDiscoveryService(
        HttpClient httpClient,
        ILogger<ServiceDiscoveryService> logger,
        IOptions<GatewayOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _serviceRegistry = new Dictionary<string, List<ServiceEndpoint>>();
        _registryLock = new SemaphoreSlim(1, 1);
    }

    public async Task<Result<IEnumerable<ServiceEndpoint>>> DiscoverServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _registryLock.WaitAsync(cancellationToken);
            
            var services = new List<ServiceEndpoint>();
            
            // Register configured downstream services
            var downstreamServices = new Dictionary<string, IdentityServiceOptions>
            {
                ["Identity"] = _options.DownstreamServices.Identity,
                ["User"] = _options.DownstreamServices.User,
                ["Notification"] = _options.DownstreamServices.Notification
            };

            foreach (var service in downstreamServices)
            {
                var serviceEndpoint = new ServiceEndpoint
                {
                    ServiceName = service.Key,
                    BaseUrl = service.Value.BaseUrl,
                    HealthEndpoint = service.Value.HealthEndpoint,
                    IsHealthy = true,
                    LastCheck = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string>
                    {
                        ["TimeoutSeconds"] = service.Value.TimeoutSeconds.ToString(),
                        ["RetryCount"] = service.Value.RetryCount.ToString(),
                        ["EnableCircuitBreaker"] = service.Value.EnableCircuitBreaker.ToString(),
                        ["FailureThreshold"] = service.Value.FailureThreshold.ToString(),
                        ["CircuitBreakerTimeoutSeconds"] = service.Value.CircuitBreakerTimeoutSeconds.ToString()
                    }
                };

                services.Add(serviceEndpoint);
            }

            // Update registry
            _serviceRegistry.Clear();
            foreach (var service in services)
            {
                if (!_serviceRegistry.ContainsKey(service.ServiceName))
                {
                    _serviceRegistry[service.ServiceName] = new List<ServiceEndpoint>();
                }
                _serviceRegistry[service.ServiceName].Add(service);
            }

            _logger.LogInformation("Discovered {ServiceCount} services", services.Count);
            return Result<IEnumerable<ServiceEndpoint>>.Success(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering services");
            return Result<IEnumerable<ServiceEndpoint>>.Failure("Service discovery failed");
        }
        finally
        {
            _registryLock.Release();
        }
    }

    public async Task<Result<ServiceEndpoint>> GetServiceEndpointAsync(string serviceName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _registryLock.WaitAsync(cancellationToken);

            if (!_serviceRegistry.ContainsKey(serviceName))
            {
                _logger.LogWarning("Service {ServiceName} not found in registry", serviceName);
                return Result<ServiceEndpoint>.Failure($"Service {serviceName} not found");
            }

            var availableEndpoints = _serviceRegistry[serviceName].Where(e => e.IsHealthy).ToList();
            if (!availableEndpoints.Any())
            {
                _logger.LogWarning("No healthy endpoints available for service {ServiceName}", serviceName);
                return Result<ServiceEndpoint>.Failure($"No healthy endpoints for service {serviceName}");
            }

            // Simple round-robin load balancing
            var selectedEndpoint = availableEndpoints[Random.Shared.Next(availableEndpoints.Count)];
            
            _logger.LogDebug("Selected endpoint {Endpoint} for service {ServiceName}", 
                selectedEndpoint.BaseUrl, serviceName);

            return Result<ServiceEndpoint>.Success(selectedEndpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service endpoint for {ServiceName}", serviceName);
            return Result<ServiceEndpoint>.Failure($"Error getting endpoint for {serviceName}");
        }
        finally
        {
            _registryLock.Release();
        }
    }

    public async Task<Result> RegisterServiceAsync(ServiceEndpoint serviceEndpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            await _registryLock.WaitAsync(cancellationToken);

            if (!_serviceRegistry.ContainsKey(serviceEndpoint.ServiceName))
            {
                _serviceRegistry[serviceEndpoint.ServiceName] = new List<ServiceEndpoint>();
            }

            // Remove existing endpoint with same base URL
            _serviceRegistry[serviceEndpoint.ServiceName].RemoveAll(e => e.BaseUrl == serviceEndpoint.BaseUrl);
            
            // Add new endpoint
            _serviceRegistry[serviceEndpoint.ServiceName].Add(serviceEndpoint);

            _logger.LogInformation("Registered service endpoint {ServiceName} at {BaseUrl}", 
                serviceEndpoint.ServiceName, serviceEndpoint.BaseUrl);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering service endpoint {ServiceName}", serviceEndpoint.ServiceName);
            return Result.Failure($"Error registering service {serviceEndpoint.ServiceName}");
        }
        finally
        {
            _registryLock.Release();
        }
    }

    public async Task<Result> DeregisterServiceAsync(string serviceName, string baseUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            await _registryLock.WaitAsync(cancellationToken);

            if (!_serviceRegistry.ContainsKey(serviceName))
            {
                return Result.Success(); // Already not registered
            }

            _serviceRegistry[serviceName].RemoveAll(e => e.BaseUrl == baseUrl);

            if (!_serviceRegistry[serviceName].Any())
            {
                _serviceRegistry.Remove(serviceName);
            }

            _logger.LogInformation("Deregistered service endpoint {ServiceName} at {BaseUrl}", serviceName, baseUrl);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deregistering service endpoint {ServiceName}", serviceName);
            return Result.Failure($"Error deregistering service {serviceName}");
        }
        finally
        {
            _registryLock.Release();
        }
    }

    public async Task<Result<IEnumerable<ServiceEndpoint>>> GetAllServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _registryLock.WaitAsync(cancellationToken);

            var allServices = _serviceRegistry.Values
                .SelectMany(endpoints => endpoints)
                .ToList();

            return Result<IEnumerable<ServiceEndpoint>>.Success(allServices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all services");
            return Result<IEnumerable<ServiceEndpoint>>.Failure("Error retrieving services");
        }
        finally
        {
            _registryLock.Release();
        }
    }
}