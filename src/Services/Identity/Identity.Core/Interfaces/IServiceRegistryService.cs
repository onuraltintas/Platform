using Identity.Core.DTOs;
using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;

namespace Identity.Core.Interfaces;

public interface IServiceRegistryService
{
    Task<Result<ServiceDto>> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<Result<ServiceDto>> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ServiceDto>>> GetServicesAsync(int page = 1, int pageSize = 10, string? search = null, ServiceType? type = null, ServiceStatus? status = null, CancellationToken cancellationToken = default);
    Task<Result<ServiceDto>> RegisterServiceAsync(ServiceRegistrationRequest request, CancellationToken cancellationToken = default);
    Task<Result<ServiceDto>> CreateServiceAsync(CreateServiceRequest request, string registeredBy, CancellationToken cancellationToken = default);
    Task<Result<ServiceDto>> UpdateServiceAsync(Guid serviceId, UpdateServiceRequest request, string modifiedBy, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteServiceAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateHealthStatusAsync(Guid serviceId, ServiceStatus status, string? statusMessage = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateHealthStatusAsync(ServiceHealthCheckRequest request, CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ServiceDto>>> GetActiveServicesAsync(CancellationToken cancellationToken = default);
    Task<Result<IEnumerable<ServiceDto>>> GetGroupServicesAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<Result<bool>> GrantServiceAccessToGroupAsync(Guid groupId, Guid serviceId, string grantedBy, DateTime? expiresAt = null, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeServiceAccessFromGroupAsync(Guid groupId, Guid serviceId, CancellationToken cancellationToken = default);
    Task<Result<bool>> ValidateRegistrationKeyAsync(string registrationKey, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsServiceNameAvailableAsync(string name, Guid? excludeServiceId = null, CancellationToken cancellationToken = default);
}

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<Service?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<PagedResult<Service>> GetServicesAsync(int page, int pageSize, string? search = null, ServiceType? type = null, ServiceStatus? status = null, CancellationToken cancellationToken = default);
    Task<Service> CreateAsync(Service service, CancellationToken cancellationToken = default);
    Task<Service> UpdateAsync(Service service, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<bool> IsNameTakenAsync(string name, Guid? excludeServiceId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Service>> GetActiveServicesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Service>> GetGroupServicesAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<int> GetPermissionCountAsync(Guid serviceId, CancellationToken cancellationToken = default);
    Task<int> GetGroupCountAsync(Guid serviceId, CancellationToken cancellationToken = default);
}