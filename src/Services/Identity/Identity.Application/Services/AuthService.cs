using Identity.Core.Entities;
using Identity.Core.Interfaces;
using Identity.Core.DTOs;
using Identity.Application.Events;
using Enterprise.Shared.Common.Models;
using Enterprise.Shared.Common.Enums;
using Enterprise.Shared.Security.Interfaces;
using Enterprise.Shared.Caching.Interfaces;
using Enterprise.Shared.Events.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using System.Security.Claims;

namespace Identity.Application.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly Identity.Core.Interfaces.ITokenService _tokenService;
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IHashingService _hashingService;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly IMapper _mapper;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _configuration;

    private readonly int _maxFailedAttempts;
    private readonly int _lockoutDuration;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        Identity.Core.Interfaces.ITokenService tokenService,
        IUserService userService,
        IGroupService groupService,
        IGoogleAuthService googleAuthService,
        IHashingService hashingService,
        ICacheService cacheService,
        IEventBus eventBus,
        IMapper mapper,
        ILogger<AuthService> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _userService = userService;
        _groupService = groupService;
        _googleAuthService = googleAuthService;
        _hashingService = hashingService;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
        
        _maxFailedAttempts = int.Parse(_configuration["MAX_FAILED_ATTEMPTS"] ?? "5");
        _lockoutDuration = int.Parse(_configuration["LOCKOUT_DURATION"] ?? "30");
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to find user by email or username
            var user = await _userManager.FindByEmailAsync(request.EmailOrUsername);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(request.EmailOrUsername);
            }

            if (user == null)
            {
                await LogFailedAttemptAsync(request.EmailOrUsername, "User not found", request.IpAddress, request.UserAgent, request.DeviceId);
                return Result<TokenResponse>.Failure("Geçersiz e-posta/kullanıcı adı veya şifre");
            }

            // Check if user is locked out
            if (user.LockedUntil.HasValue && user.LockedUntil > DateTime.UtcNow)
            {
                var remainingMinutes = (user.LockedUntil.Value - DateTime.UtcNow).TotalMinutes;
                return Result<TokenResponse>.Failure($"Hesap kilitli. {Math.Ceiling(remainingMinutes)} dakika sonra tekrar deneyiniz");
            }

            // Check if account is active
            if (!user.IsActive || user.IsDeleted)
            {
                return Result<TokenResponse>.Failure("Hesap devre dışı");
            }

            // Verify password
            var passwordResult = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordResult)
            {
                await HandleFailedLoginAsync(user, "Invalid password", request.IpAddress, request.UserAgent, request.DeviceId);
                return Result<TokenResponse>.Failure("Geçersiz e-posta/kullanıcı adı veya şifre");
            }

            // Check email confirmation if required
            var requireEmailConfirmation = bool.Parse(_configuration["ENABLE_EMAIL_CONFIRMATION"] ?? "true");
            if (requireEmailConfirmation && !user.EmailConfirmed)
            {
                return Result<TokenResponse>.Failure("E-posta adresinizi doğrulamanız gerekiyor");
            }

            // Handle 2FA if enabled
            if (user.TwoFactorEnabled)
            {
                if (string.IsNullOrEmpty(request.TwoFactorCode))
                {
                    // Return special response indicating 2FA required
                    return Result<TokenResponse>.Failure("İki faktörlü kimlik doğrulama kodu gerekli", OperationStatus.Failed);
                }

                // Validate 2FA code (implementation depends on your 2FA provider)
                var is2FAValid = await Validate2FACodeAsync(user, request.TwoFactorCode);
                if (!is2FAValid)
                {
                    await HandleFailedLoginAsync(user, "Invalid 2FA code", request.IpAddress, request.UserAgent, request.DeviceId);
                    return Result<TokenResponse>.Failure("Geçersiz doğrulama kodu");
                }
            }

            // Reset failed login attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;
            user.LastLoginDevice = request.DeviceName;
            await _userManager.UpdateAsync(user);

            // Determine group context
            Guid? groupId = null;
            if (request.GroupId.HasValue)
            {
                var canAccess = await _groupService.CanUserAccessGroupAsync(user.Id, request.GroupId.Value, cancellationToken);
                if (canAccess.IsSuccess && canAccess.Value)
                {
                    groupId = request.GroupId.Value;
                }
            }
            else if (user.DefaultGroupId.HasValue)
            {
                groupId = user.DefaultGroupId.Value;
            }

            // Generate tokens
            var tokenResult = await _tokenService.GenerateTokenAsync(user.Id, groupId, request.DeviceId, cancellationToken);
            if (!tokenResult.IsSuccess)
            {
                return Result<TokenResponse>.Failure("Token oluşturulamadı");
            }

            // Track device if provided
            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                await TrackUserDeviceAsync(user.Id, request.DeviceId, request.DeviceName, request.UserAgent, request.IpAddress);
            }

            // Log successful login
            await LogSuccessfulAttemptAsync(user.Id, request.IpAddress, request.UserAgent, request.DeviceId, groupId);

            // Publish login event async (fire-and-forget pattern for performance)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _eventBus.PublishAsync(new UserLoggedInEvent
                    {
                        UserId = user.Id,
                        Email = user.Email!,
                        IpAddress = request.IpAddress,
                        DeviceId = request.DeviceId,
                        GroupId = groupId,
                        LoginAt = DateTime.UtcNow
                    }, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish UserLoggedInEvent for user {UserId}", user.Id);
                }
            });

            _logger.LogInformation("User {UserId} logged in successfully from {IpAddress}", user.Id, request.IpAddress);

            return Result<TokenResponse>.Success(tokenResult.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {EmailOrUsername}", request.EmailOrUsername);
            return Result<TokenResponse>.Failure("Giriş işlemi sırasında hata oluştu");
        }
    }

    public async Task<Result<TokenResponse>> GoogleLoginAsync(GoogleLoginRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate Google ID token
            var googleUserResult = await _googleAuthService.ValidateIdTokenAsync(request.IdToken);
            if (!googleUserResult.IsSuccess)
            {
                return Result<TokenResponse>.Failure("Geçersiz Google token");
            }

            var googleUser = googleUserResult.Value;
            var user = await _userManager.FindByEmailAsync(googleUser.Email);

            // If user doesn't exist, create new user
            if (user == null)
            {
                user = new ApplicationUser
                {
                    Email = googleUser.Email,
                    UserName = googleUser.Email,
                    FirstName = googleUser.GivenName ?? "",
                    LastName = googleUser.FamilyName ?? "",
                    EmailConfirmed = googleUser.EmailVerified,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    ProfilePictureUrl = googleUser.Picture,
                    DataProcessingConsent = true,
                    ConsentGivenAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    _logger.LogError("Failed to create user via Google login: {Errors}", 
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Result<TokenResponse>.Failure("Hesap oluşturulamadı");
                }

                // Add user to default group if specified
                var defaultGroupName = _configuration["DEFAULT_GROUP_NAME"];
                if (!string.IsNullOrEmpty(defaultGroupName))
                {
                    var defaultGroup = await _groupService.GetByNameAsync(defaultGroupName, cancellationToken);
                    if (defaultGroup.IsSuccess)
                    {
                        await _groupService.AddUserToGroupAsync(defaultGroup.Value.Id, user.Id, UserGroupRole.Member, "System", cancellationToken);
                        user.DefaultGroupId = defaultGroup.Value.Id;
                        await _userManager.UpdateAsync(user);
                    }
                }

                // Publish user registered event
                await _eventBus.PublishAsync(new UserRegisteredEvent
                {
                    UserId = user.Id,
                    Email = user.Email!,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    RegistrationMethod = "Google",
                    IpAddress = request.IpAddress,
                    DeviceId = request.DeviceId
                }, cancellationToken);
            }

            // Check if account is active
            if (!user.IsActive || user.IsDeleted)
            {
                return Result<TokenResponse>.Failure("Hesap devre dışı");
            }

            // Update last login info
            user.LastLoginAt = DateTime.UtcNow;
            user.LastLoginIp = request.IpAddress;
            user.LastLoginDevice = request.DeviceName;
            await _userManager.UpdateAsync(user);

            // Determine group context
            Guid? groupId = null;
            if (request.GroupId.HasValue)
            {
                var canAccess = await _groupService.CanUserAccessGroupAsync(user.Id, request.GroupId.Value, cancellationToken);
                if (canAccess.IsSuccess && canAccess.Value)
                {
                    groupId = request.GroupId.Value;
                }
            }
            else if (user.DefaultGroupId.HasValue)
            {
                groupId = user.DefaultGroupId.Value;
            }

            // Generate tokens
            var tokenResult = await _tokenService.GenerateTokenAsync(user.Id, groupId, request.DeviceId, cancellationToken);
            if (!tokenResult.IsSuccess)
            {
                return Result<TokenResponse>.Failure("Token oluşturulamadı");
            }

            // Track device if provided
            if (!string.IsNullOrEmpty(request.DeviceId))
            {
                await TrackUserDeviceAsync(user.Id, request.DeviceId, request.DeviceName, request.UserAgent, request.IpAddress);
            }

            // Log successful login
            await LogSuccessfulAttemptAsync(user.Id, request.IpAddress, request.UserAgent, request.DeviceId, groupId);

            // Publish login event
            await _eventBus.PublishAsync(new UserLoggedInEvent
            {
                UserId = user.Id,
                Email = user.Email!,
                IpAddress = request.IpAddress,
                DeviceId = request.DeviceId,
                GroupId = groupId,
                LoginAt = DateTime.UtcNow,
                Method = "Google"
            }, cancellationToken);

            _logger.LogInformation("User {UserId} logged in via Google from {IpAddress}", user.Id, request.IpAddress);

            return Result<TokenResponse>.Success(tokenResult.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Google login");
            return Result<TokenResponse>.Failure("Google girişi sırasında hata oluştu");
        }
    }

    public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var isValid = await _tokenService.ValidateRefreshTokenAsync(request.RefreshToken, cancellationToken);
            if (!isValid.IsSuccess || !isValid.Value)
            {
                return Result<RefreshTokenResponse>.Failure("Geçersiz refresh token");
            }

            // Get user from refresh token
            var userId = await GetUserIdFromRefreshTokenAsync(request.RefreshToken);
            if (string.IsNullOrEmpty(userId))
            {
                return Result<RefreshTokenResponse>.Failure("Geçersiz refresh token");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || !user.IsActive || user.IsDeleted)
            {
                return Result<RefreshTokenResponse>.Failure("Kullanıcı bulunamadı veya devre dışı");
            }

            // Get group context from refresh token
            var groupId = await GetGroupIdFromRefreshTokenAsync(request.RefreshToken);

            // Generate new tokens
            var tokenResult = await _tokenService.GenerateTokenAsync(user.Id, groupId, request.DeviceId, cancellationToken);
            if (!tokenResult.IsSuccess)
            {
                return Result<RefreshTokenResponse>.Failure("Token yenilenemedi");
            }

            // Revoke old refresh token
            await RevokeRefreshTokenAsync(request.RefreshToken, "Replaced by new token");

            _logger.LogInformation("Token refreshed for user {UserId}", user.Id);

            return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse
            {
                AccessToken = tokenResult.Value.AccessToken,
                RefreshToken = tokenResult.Value.RefreshToken,
                ExpiresAt = tokenResult.Value.ExpiresAt,
                ExpiresIn = tokenResult.Value.ExpiresIn,
                Permissions = tokenResult.Value.Permissions
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return Result<RefreshTokenResponse>.Failure("Token yenileme sırasında hata oluştu");
        }
    }

    public async Task<Result<bool>> LogoutAsync(string userId, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            // Revoke all refresh tokens for user (and optionally specific device)
            await RevokeUserRefreshTokensAsync(userId, deviceId);

            // Remove from cache
            var cacheKey = $"user_permissions_{userId}";
            await _cacheService.RemoveAsync(cacheKey, cancellationToken);

            // Publish logout event
            await _eventBus.PublishAsync(new UserLoggedOutEvent
            {
                UserId = userId,
                DeviceId = deviceId,
                LogoutAt = DateTime.UtcNow
            }, cancellationToken);

            _logger.LogInformation("User {UserId} logged out", userId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout for user {UserId}", userId);
            return Result<bool>.Failure("Çıkış işlemi sırasında hata oluştu");
        }
    }

    public async Task<Result<bool>> LogoutAllDevicesAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await LogoutAsync(userId, null, cancellationToken);
    }

    public async Task<Result<bool>> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            await RevokeRefreshTokenAsync(refreshToken, "Manually revoked");
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return Result<bool>.Failure("Token iptal edilemedi");
        }
    }

    public async Task<Result<bool>> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = await _tokenService.GetUserIdFromTokenAsync(accessToken);
            if (!userId.IsSuccess)
            {
                return Result<bool>.Success(false);
            }

            // Check if token is blacklisted (for logout scenarios)
            var jti = GetJtiFromToken(accessToken);
            if (!string.IsNullOrEmpty(jti))
            {
                var isBlacklisted = await _tokenService.IsTokenBlacklistedAsync(jti, cancellationToken);
                if (isBlacklisted.IsSuccess && isBlacklisted.Value)
                {
                    return Result<bool>.Success(false);
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return Result<bool>.Success(false);
        }
    }

    public async Task<Result<TokenResponse>> SwitchGroupAsync(string userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        try
        {
            var canAccess = await _groupService.CanUserAccessGroupAsync(userId, groupId, cancellationToken);
            if (!canAccess.IsSuccess || !canAccess.Value)
            {
                return Result<TokenResponse>.Failure("Bu gruba erişim izniniz yok");
            }

            // Generate new token with new group context
            var tokenResult = await _tokenService.GenerateTokenAsync(userId, groupId, cancellationToken: cancellationToken);
            if (!tokenResult.IsSuccess)
            {
                return Result<TokenResponse>.Failure("Token oluşturulamadı");
            }

            // Update user's last group switch time
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                user.LastGroupSwitch = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }

            _logger.LogInformation("User {UserId} switched to group {GroupId}", userId, groupId);

            return Result<TokenResponse>.Success(tokenResult.Value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error switching group for user {UserId} to group {GroupId}", userId, groupId);
            return Result<TokenResponse>.Failure("Grup değiştirme sırasında hata oluştu");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetUserPermissionsAsync(string userId, Guid? groupId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"user_permissions_{userId}_{groupId}";
            var cachedPermissions = await _cacheService.GetAsync<IEnumerable<string>>(cacheKey, cancellationToken);
            
            if (cachedPermissions != null)
            {
                return Result<IEnumerable<string>>.Success(cachedPermissions);
            }

            // Get user roles and permissions from database
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<IEnumerable<string>>.Failure("Kullanıcı bulunamadı");
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var permissions = new List<string>();

            // Add basic user permissions
            permissions.Add("identity.read");
            permissions.Add("profile.read");
            permissions.Add("profile.write");

            // Add role-based permissions
            if (userRoles.Contains("Admin"))
            {
                permissions.AddRange(new[] { "users.read", "users.write", "groups.read", "groups.write" });
            }

            // Cache permissions for 5 minutes
            await _cacheService.SetAsync(cacheKey, permissions, TimeSpan.FromMinutes(5), cancellationToken);

            return Result<IEnumerable<string>>.Success(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for {UserId}", userId);
            return Result<IEnumerable<string>>.Failure("İzinler alınamadı");
        }
    }

    #region Private Methods

    private async Task HandleFailedLoginAsync(ApplicationUser user, string reason, string? ipAddress, string? userAgent, string? deviceId)
    {
        user.FailedLoginAttempts++;
        
        if (user.FailedLoginAttempts >= _maxFailedAttempts)
        {
            user.LockedUntil = DateTime.UtcNow.AddMinutes(_lockoutDuration);
            _logger.LogWarning("User {UserId} account locked due to too many failed attempts", user.Id);
        }

        await _userManager.UpdateAsync(user);
        await LogFailedAttemptAsync(user.Email!, reason, ipAddress, userAgent, deviceId, user.Id);
    }

    private async Task LogFailedAttemptAsync(string email, string reason, string? ipAddress, string? userAgent, string? deviceId, string? userId = null)
    {
        var attempt = new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            AttemptedAt = DateTime.UtcNow,
            IsSuccessful = false,
            FailureReason = reason,
            IpAddress = ipAddress ?? "Unknown",
            UserAgent = userAgent,
            DeviceId = deviceId,
            Type = LoginType.Password
        };

        // Store login attempt (you would implement this in repository)
        // await _loginAttemptRepository.CreateAsync(attempt);
    }

    private async Task LogSuccessfulAttemptAsync(string userId, string? ipAddress, string? userAgent, string? deviceId, Guid? groupId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return;

        var attempt = new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = user.Email!,
            AttemptedAt = DateTime.UtcNow,
            IsSuccessful = true,
            IpAddress = ipAddress ?? "Unknown",
            UserAgent = userAgent,
            DeviceId = deviceId,
            GroupId = groupId,
            Type = LoginType.Password
        };

        // Store login attempt
        // await _loginAttemptRepository.CreateAsync(attempt);
    }

    private async Task TrackUserDeviceAsync(string userId, string deviceId, string? deviceName, string? userAgent, string? ipAddress)
    {
        // Implementation for tracking user devices
        // This would typically involve checking if device exists and updating last seen info
    }

    private async Task<bool> Validate2FACodeAsync(ApplicationUser user, string code)
    {
        // Implementation for 2FA validation
        // This would typically use TOTP or SMS verification
        return await Task.FromResult(false); // Placeholder
    }

    private async Task<string?> GetUserIdFromRefreshTokenAsync(string refreshToken)
    {
        // Implementation to extract user ID from refresh token
        return await Task.FromResult<string?>(null); // Placeholder
    }

    private async Task<Guid?> GetGroupIdFromRefreshTokenAsync(string refreshToken)
    {
        // Implementation to extract group ID from refresh token
        return await Task.FromResult<Guid?>(null); // Placeholder
    }

    private async Task RevokeRefreshTokenAsync(string refreshToken, string reason)
    {
        // Implementation to revoke refresh token
        await Task.CompletedTask; // Placeholder
    }

    private async Task RevokeUserRefreshTokensAsync(string userId, string? deviceId = null)
    {
        // Implementation to revoke user's refresh tokens
        await Task.CompletedTask; // Placeholder
    }

    private string? GetJtiFromToken(string token)
    {
        // Implementation to extract JTI from JWT token
        return null; // Placeholder
    }

    #endregion
}