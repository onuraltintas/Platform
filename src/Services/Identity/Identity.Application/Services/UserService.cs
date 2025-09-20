using Identity.Core.Interfaces;
using Identity.Core.DTOs;
using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using AutoMapper;

namespace Identity.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;

    public UserService(
        UserManager<ApplicationUser> userManager,
        IMapper mapper,
        ILogger<UserService> logger)
    {
        _userManager = userManager;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<UserDto>> GetByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<UserDto>.Failure("Kullanıcı bulunamadı");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by id {UserId}", userId);
            return Result<UserDto>.Failure("Kullanıcı getirilemedi");
        }
    }

    public async Task<Result<UserDto>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result<UserDto>.Failure("Kullanıcı bulunamadı");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by email {Email}", email);
            return Result<UserDto>.Failure("Kullanıcı getirilemedi");
        }
    }

    public async Task<Result<UserDto>> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return Result<UserDto>.Failure("Kullanıcı bulunamadı");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by username {UserName}", userName);
            return Result<UserDto>.Failure("Kullanıcı getirilemedi");
        }
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersAsync(int page = 1, int pageSize = 10, string? search = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // This is a simplified implementation - in real app you'd use proper pagination
            var users = _userManager.Users.Take(pageSize).ToList();
            var userDtos = _mapper.Map<List<UserDto>>(users);
            
            var pagedResult = new PagedResult<UserDto>(userDtos, users.Count, page, pageSize);
            return Result<PagedResult<UserDto>>.Success(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return Result<PagedResult<UserDto>>.Failure("Kullanıcılar getirilemedi");
        }
    }

    public async Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailConfirmed = request.EmailConfirmed,
                DefaultGroupId = request.DefaultGroupId
            };

            var result = await _userManager.CreateAsync(user, request.Password!);
            if (!result.Succeeded)
            {
                return Result<UserDto>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user {Email}", request.Email);
            return Result<UserDto>.Failure("Kullanıcı oluşturulamadı");
        }
    }

    public async Task<Result<UserDto>> UpdateAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<UserDto>.Failure("Kullanıcı bulunamadı");
            }

            user.FirstName = request.FirstName ?? user.FirstName;
            user.LastName = request.LastName ?? user.LastName;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return Result<UserDto>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            var userDto = _mapper.Map<UserDto>(user);
            return Result<UserDto>.Success(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return Result<UserDto>.Failure("Kullanıcı güncellenemedi");
        }
    }

    public async Task<Result<bool>> DeleteAsync(string userId, string? deletedBy = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure("Kullanıcı bulunamadı");
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                return Result<bool>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return Result<bool>.Failure("Kullanıcı silinemedi");
        }
    }

    public async Task<Result<bool>> LockAsync(string userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure("Kullanıcı bulunamadı");
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
            if (!result.Succeeded)
            {
                return Result<bool>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user {UserId}", userId);
            return Result<bool>.Failure("Kullanıcı kilitlenemedi");
        }
    }

    public async Task<Result<bool>> UnlockAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure("Kullanıcı bulunamadı");
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!result.Succeeded)
            {
                return Result<bool>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {UserId}", userId);
            return Result<bool>.Failure("Kullanıcı kilidi açılamadı");
        }
    }

    public async Task<Result<bool>> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure("Kullanıcı bulunamadı");
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded)
            {
                return Result<bool>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserId}", userId);
            return Result<bool>.Failure("Şifre değiştirilemedi");
        }
    }

    public async Task<Result<bool>> ResetPasswordAsync(string userId, string token, string newPassword, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure("Kullanıcı bulunamadı");
            }

            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
            {
                return Result<bool>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
            return Result<bool>.Failure("Şifre sıfırlanamadı");
        }
    }

    public async Task<Result<string>> GeneratePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return Result<string>.Failure("Kullanıcı bulunamadı");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            return Result<string>.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for {Email}", email);
            return Result<string>.Failure("Token oluşturulamadı");
        }
    }

    public async Task<Result<bool>> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure("Kullanıcı bulunamadı");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                return Result<bool>.Failure(string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming email for user {UserId}", userId);
            return Result<bool>.Failure("E-posta doğrulanamadı");
        }
    }

    public async Task<Result<string>> GenerateEmailConfirmationTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<string>.Failure("Kullanıcı bulunamadı");
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            return Result<string>.Success(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating email confirmation token for user {UserId}", userId);
            return Result<string>.Failure("Token oluşturulamadı");
        }
    }

    public async Task<Result<bool>> IsEmailConfirmedAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<bool>.Failure("Kullanıcı bulunamadı");
            }

            return Result<bool>.Success(user.EmailConfirmed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking email confirmation for user {UserId}", userId);
            return Result<bool>.Failure("E-posta durumu kontrol edilemedi");
        }
    }

    public async Task<Result<IEnumerable<UserGroupDto>>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation - would need proper repository
            return Result<IEnumerable<UserGroupDto>>.Success(new List<UserGroupDto>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user groups for user {UserId}", userId);
            return Result<IEnumerable<UserGroupDto>>.Failure("Kullanıcı grupları getirilemedi");
        }
    }

    public async Task<Result<bool>> IsUserInGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Placeholder implementation - would need proper repository
            return Result<bool>.Success(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} is in group {GroupId}", userId, groupId);
            return Result<bool>.Failure("Grup üyeliği kontrol edilemedi");
        }
    }
}