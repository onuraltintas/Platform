using EgitimPlatform.Services.UserService.Models.DTOs;

namespace EgitimPlatform.Services.UserService.Services;

public interface IUserSettingsService
{
    Task<UserSettingsDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserSettingsDto> UpsertAsync(Guid userId, UpdateUserSettingsRequest request, CancellationToken cancellationToken = default);
}

