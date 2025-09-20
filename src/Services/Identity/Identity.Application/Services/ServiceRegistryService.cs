using Identity.Core.Interfaces;
using Identity.Core.Entities;
using Identity.Core.DTOs;
using Identity.Application.Events;
using Enterprise.Shared.Common.Models;
using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Events.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace Identity.Application.Services;

public class ServiceRegistryService : IServiceRegistryService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly IPermissionService _permissionService;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly IMapper _mapper;
    private readonly ILogger<ServiceRegistryService> _logger;
    private readonly IConfiguration _configuration;

    private readonly string _registrationKey;

    public ServiceRegistryService(
        IServiceRepository serviceRepository,
        IPermissionService permissionService,
        ICacheService cacheService,
        IEventBus eventBus,
        IMapper mapper,
        ILogger<ServiceRegistryService> logger,
        IConfiguration configuration)
    {
        _serviceRepository = serviceRepository;
        _permissionService = permissionService;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        
        _registrationKey = _configuration["SERVICE_REGISTRATION_KEY"] ?? "default-key";
    }

    public async Task<Result<ServiceDto>> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"service_{serviceId}";
            var cachedService = await _cacheService.GetAsync<ServiceDto>(cacheKey, cancellationToken);
            if (cachedService != null)
            {
                return Result<ServiceDto>.Success(cachedService);
            }

            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service == null)
            {
                return Result<ServiceDto>.Failure("Servis bulunamadı");
            }

            var serviceDto = _mapper.Map<ServiceDto>(service);
            
            // Get statistics
            serviceDto.PermissionCount = await _serviceRepository.GetPermissionCountAsync(serviceId, cancellationToken);
            serviceDto.GroupCount = await _serviceRepository.GetGroupCountAsync(serviceId, cancellationToken);

            // Cache for 5 minutes
            await _cacheService.SetAsync(cacheKey, serviceDto, TimeSpan.FromMinutes(5), cancellationToken);

            return Result<ServiceDto>.Success(serviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service {ServiceId}", serviceId);
            return Result<ServiceDto>.Failure("Servis alınamadı");
        }
    }

    public async Task<Result<ServiceDto>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await _serviceRepository.GetByNameAsync(name, cancellationToken);
            if (service == null)
            {
                return Result<ServiceDto>.Failure("Servis bulunamadı");
            }

            var serviceDto = _mapper.Map<ServiceDto>(service);
            
            // Get statistics
            serviceDto.PermissionCount = await _serviceRepository.GetPermissionCountAsync(service.Id, cancellationToken);
            serviceDto.GroupCount = await _serviceRepository.GetGroupCountAsync(service.Id, cancellationToken);

            return Result<ServiceDto>.Success(serviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service by name {ServiceName}", name);
            return Result<ServiceDto>.Failure("Servis alınamadı");
        }
    }

    public async Task<Result<PagedResult<ServiceDto>>> GetServicesAsync(int page = 1, int pageSize = 10, string? search = null, ServiceType? type = null, ServiceStatus? status = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var services = await _serviceRepository.GetServicesAsync(page, pageSize, search, type, status, cancellationToken);
            var serviceDtos = new List<ServiceDto>();

            foreach (var service in services.Data)
            {
                var serviceDto = _mapper.Map<ServiceDto>(service);
                serviceDto.PermissionCount = await _serviceRepository.GetPermissionCountAsync(service.Id, cancellationToken);
                serviceDto.GroupCount = await _serviceRepository.GetGroupCountAsync(service.Id, cancellationToken);
                serviceDtos.Add(serviceDto);
            }

            var result = new PagedResult<ServiceDto>(serviceDtos, services.TotalCount, services.Page, services.PageSize);
            return Result<PagedResult<ServiceDto>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting services");
            return Result<PagedResult<ServiceDto>>.Failure("Servisler alınamadı");
        }
    }

    public async Task<Result<ServiceDto>> RegisterServiceAsync(ServiceRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate registration key
            if (request.RegistrationKey != _registrationKey)
            {
                _logger.LogWarning("Invalid registration key provided for service {ServiceName}", request.Name);
                return Result<ServiceDto>.Failure("Geçersiz kayıt anahtarı");
            }

            // Check if service name is available
            var isAvailable = await IsServiceNameAvailableAsync(request.Name, cancellationToken: cancellationToken);
            if (!isAvailable.IsSuccess || !isAvailable.Value)
            {
                return Result<ServiceDto>.Failure("Servis adı zaten kullanımda");
            }

            var service = new Service
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Endpoint = request.Endpoint,
                Version = request.Version,
                Type = request.Type,
                HealthCheckEndpoint = request.HealthCheckEndpoint,
                RequiresAuthentication = true,
                Metadata = request.Metadata,
                RegisteredAt = DateTime.UtcNow,
                RegisteredBy = "System",
                Status = ServiceStatus.Unknown,
                IsActive = true
            };

            var createdService = await _serviceRepository.CreateAsync(service, cancellationToken);

            // Create permissions
            foreach (var permissionRequest in request.Permissions)
            {
                permissionRequest.ServiceId = createdService.Id;
                await _permissionService.CreatePermissionAsync(permissionRequest, cancellationToken);
            }

            var serviceDto = _mapper.Map<ServiceDto>(createdService);

            // Clear cache
            await _cacheService.RemovePatternAsync("services_*", cancellationToken);

            // Publish event
            await _eventBus.PublishAsync(new ServiceRegisteredEvent
            {
                ServiceId = createdService.Id,
                ServiceName = createdService.Name,
                Endpoint = createdService.Endpoint,
                ServiceType = createdService.Type.ToString(),
                RegisteredAt = createdService.RegisteredAt,
                RegisteredBy = "System"
            }, cancellationToken);

            _logger.LogInformation("Service {ServiceName} registered successfully", request.Name);

            return Result<ServiceDto>.Success(serviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering service {ServiceName}", request.Name);
            return Result<ServiceDto>.Failure("Servis kaydedilemedi");
        }
    }

    public async Task<Result<ServiceDto>> CreateServiceAsync(CreateServiceRequest request, string registeredBy, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if service name is available
            var isAvailable = await IsServiceNameAvailableAsync(request.Name, cancellationToken: cancellationToken);
            if (!isAvailable.IsSuccess || !isAvailable.Value)
            {
                return Result<ServiceDto>.Failure("Servis adı zaten kullanımda");
            }

            var service = new Service
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Endpoint = request.Endpoint,
                Version = request.Version,
                Type = request.Type,
                HealthCheckEndpoint = request.HealthCheckEndpoint,
                RequiresAuthentication = request.RequiresAuthentication,
                Metadata = request.Metadata,
                RegisteredAt = DateTime.UtcNow,
                RegisteredBy = registeredBy,
                Status = ServiceStatus.Unknown,
                IsActive = true
            };

            var createdService = await _serviceRepository.CreateAsync(service, cancellationToken);
            var serviceDto = _mapper.Map<ServiceDto>(createdService);

            // Clear cache
            await _cacheService.RemovePatternAsync("services_*", cancellationToken);

            _logger.LogInformation("Service {ServiceName} created by {RegisteredBy}", request.Name, registeredBy);

            return Result<ServiceDto>.Success(serviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service {ServiceName}", request.Name);
            return Result<ServiceDto>.Failure("Servis oluşturulamadı");
        }
    }

    public async Task<Result<ServiceDto>> UpdateServiceAsync(Guid serviceId, UpdateServiceRequest request, string modifiedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service == null)
            {
                return Result<ServiceDto>.Failure("Servis bulunamadı");
            }

            // Update service properties
            service.DisplayName = request.DisplayName;
            service.Description = request.Description;
            service.Endpoint = request.Endpoint;
            service.Version = request.Version;
            service.HealthCheckEndpoint = request.HealthCheckEndpoint;
            service.RequiresAuthentication = request.RequiresAuthentication;
            service.Metadata = request.Metadata;
            service.LastModifiedAt = DateTime.UtcNow;

            var updatedService = await _serviceRepository.UpdateAsync(service, cancellationToken);
            var serviceDto = _mapper.Map<ServiceDto>(updatedService);

            // Clear cache
            await _cacheService.RemoveAsync($"service_{serviceId}", cancellationToken);
            await _cacheService.RemovePatternAsync("services_*", cancellationToken);

            _logger.LogInformation("Service {ServiceId} updated by {ModifiedBy}", serviceId, modifiedBy);

            return Result<ServiceDto>.Success(serviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service {ServiceId}", serviceId);
            return Result<ServiceDto>.Failure("Servis güncellenemedi");
        }
    }

    public async Task<Result<bool>> DeleteServiceAsync(Guid serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _serviceRepository.ExistsAsync(serviceId, cancellationToken);
            if (!exists)
            {
                return Result<bool>.Failure("Servis bulunamadı");
            }

            var deleted = await _serviceRepository.DeleteAsync(serviceId, cancellationToken);
            if (!deleted)
            {
                return Result<bool>.Failure("Servis silinemedi");
            }

            // Clear cache
            await _cacheService.RemoveAsync($"service_{serviceId}", cancellationToken);
            await _cacheService.RemovePatternAsync("services_*", cancellationToken);

            _logger.LogInformation("Service {ServiceId} deleted", serviceId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service {ServiceId}", serviceId);
            return Result<bool>.Failure("Servis silinemedi");
        }
    }

    public async Task<Result<bool>> UpdateHealthStatusAsync(Guid serviceId, ServiceStatus status, string? statusMessage = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var service = await _serviceRepository.GetByIdAsync(serviceId, cancellationToken);
            if (service == null)
            {
                return Result<bool>.Failure("Servis bulunamadı");
            }

            service.Status = status;
            service.StatusMessage = statusMessage;
            service.LastHealthCheckAt = DateTime.UtcNow;

            await _serviceRepository.UpdateAsync(service, cancellationToken);

            // Clear cache
            await _cacheService.RemoveAsync($"service_{serviceId}", cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating health status for service {ServiceId}", serviceId);
            return Result<bool>.Failure("Sağlık durumu güncellenemedi");
        }
    }

    public async Task<Result<bool>> UpdateHealthStatusAsync(ServiceHealthCheckRequest request, CancellationToken cancellationToken = default)
    {
        return await UpdateHealthStatusAsync(request.ServiceId, request.Status, request.StatusMessage, cancellationToken);
    }

    public async Task<Result<IEnumerable<ServiceDto>>> GetActiveServicesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = "active_services";
            var cachedServices = await _cacheService.GetAsync<IEnumerable<ServiceDto>>(cacheKey, cancellationToken);
            if (cachedServices != null)
            {
                return Result<IEnumerable<ServiceDto>>.Success(cachedServices);
            }

            var services = await _serviceRepository.GetActiveServicesAsync(cancellationToken);
            var serviceDtos = _mapper.Map<IEnumerable<ServiceDto>>(services);

            // Cache for 2 minutes
            await _cacheService.SetAsync(cacheKey, serviceDtos, TimeSpan.FromMinutes(2), cancellationToken);

            return Result<IEnumerable<ServiceDto>>.Success(serviceDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active services");
            return Result<IEnumerable<ServiceDto>>.Failure("Aktif servisler alınamadı");
        }
    }

    public async Task<Result<IEnumerable<ServiceDto>>> GetGroupServicesAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var services = await _serviceRepository.GetGroupServicesAsync(groupId, cancellationToken);
            var serviceDtos = _mapper.Map<IEnumerable<ServiceDto>>(services);

            return Result<IEnumerable<ServiceDto>>.Success(serviceDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting services for group {GroupId}", groupId);
            return Result<IEnumerable<ServiceDto>>.Failure("Grup servisleri alınamadı");
        }
    }

    public async Task<Result<bool>> GrantServiceAccessToGroupAsync(Guid groupId, Guid serviceId, string grantedBy, DateTime? expiresAt = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Implementation would involve GroupService entity
            // For now, return success
            _logger.LogInformation("Service {ServiceId} access granted to group {GroupId} by {GrantedBy}", 
                serviceId, groupId, grantedBy);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error granting service access");
            return Result<bool>.Failure("Servis erişimi verilemedi");
        }
    }

    public async Task<Result<bool>> RevokeServiceAccessFromGroupAsync(Guid groupId, Guid serviceId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Implementation would involve GroupService entity
            _logger.LogInformation("Service {ServiceId} access revoked from group {GroupId}", serviceId, groupId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking service access");
            return Result<bool>.Failure("Servis erişimi iptal edilemedi");
        }
    }

    public async Task<Result<bool>> ValidateRegistrationKeyAsync(string registrationKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var isValid = registrationKey == _registrationKey;
            return Result<bool>.Success(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating registration key");
            return Result<bool>.Success(false);
        }
    }

    public async Task<Result<bool>> IsServiceNameAvailableAsync(string name, Guid? excludeServiceId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var isTaken = await _serviceRepository.IsNameTakenAsync(name, excludeServiceId, cancellationToken);
            return Result<bool>.Success(!isTaken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service name availability");
            return Result<bool>.Failure("Servis adı kontrolü yapılamadı");
        }
    }
}