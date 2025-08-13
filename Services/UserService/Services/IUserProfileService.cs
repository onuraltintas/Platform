using EgitimPlatform.Services.UserService.Models.DTOs;

namespace EgitimPlatform.Services.UserService.Services;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> CreateAsync(CreateUserProfileRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateAsync(Guid userId, UpdateUserProfileRequest request, CancellationToken cancellationToken = default);
}

