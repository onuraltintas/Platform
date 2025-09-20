using Identity.Core.Interfaces;
using Identity.Core.DTOs;
using Identity.Core.Entities;
using Enterprise.Shared.Common.Models;
using Enterprise.Shared.Security.Interfaces;
using Enterprise.Shared.Caching.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AutoMapper;
using System.Security.Cryptography;
using System.Text;

namespace Identity.Application.Services;

public class TokenService : Identity.Core.Interfaces.ITokenService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly Enterprise.Shared.Security.Interfaces.ITokenService _enterpriseTokenService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly ILogger<TokenService> _logger;
    private readonly IConfiguration _configuration;

    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryMinutes;

    public TokenService(
        UserManager<ApplicationUser> userManager,
        IUserService userService,
        IGroupService groupService,
        Enterprise.Shared.Security.Interfaces.ITokenService enterpriseTokenService,
        ICacheService cacheService,
        IMapper mapper,
        ILogger<TokenService> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _userService = userService;
        _groupService = groupService;
        _enterpriseTokenService = enterpriseTokenService;
        _cacheService = cacheService;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;

        _accessTokenExpiryMinutes = int.Parse(_configuration["JWT_ACCESS_TOKEN_EXPIRY"] ?? "15");
        _refreshTokenExpiryMinutes = int.Parse(_configuration["JWT_REFRESH_TOKEN_EXPIRY"] ?? "10080"); // 7 days
    }

    public async Task<Result<TokenResponse>> GenerateTokenAsync(string userId, Guid? groupId = null, string? deviceId = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Result<TokenResponse>.Failure("Kullanıcı bulunamadı");
            }

            // Get user DTO
            var userResult = await _userService.GetByIdAsync(userId, cancellationToken);
            if (!userResult.IsSuccess)
            {
                return Result<TokenResponse>.Failure("Kullanıcı bilgileri alınamadı");
            }

            var userDto = userResult.Value;

            // Get user groups
            var userGroupsResult = await _groupService.GetUserGroupsAsync(userId, cancellationToken);
            var userGroups = userGroupsResult.IsSuccess ? userGroupsResult.Value : new List<GroupDto>();

            // Determine active group
            GroupDto? activeGroup = null;
            if (groupId.HasValue)
            {
                activeGroup = userGroups.FirstOrDefault(g => g.Id == groupId.Value);
            }
            else if (user.DefaultGroupId.HasValue)
            {
                activeGroup = userGroups.FirstOrDefault(g => g.Id == user.DefaultGroupId.Value);
            }
            else
            {
                activeGroup = userGroups.FirstOrDefault();
            }

            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);

            // Get user permissions
            var permissions = await GetUserPermissionsForTokenAsync(userId, activeGroup?.Id, cancellationToken);

            // Prepare claims for JWT
            var claims = new Dictionary<string, object>
            {
                ["sub"] = userId,
                ["email"] = user.Email!,
                ["name"] = $"{user.FirstName} {user.LastName}".Trim(),
                ["given_name"] = user.FirstName,
                ["family_name"] = user.LastName,
                ["email_verified"] = user.EmailConfirmed
            };

            // Add group information
            if (activeGroup != null)
            {
                claims["active_group"] = new
                {
                    id = activeGroup.Id,
                    name = activeGroup.Name,
                    type = activeGroup.Type.ToString()
                };
            }

            if (userGroups.Any())
            {
                claims["groups"] = userGroups.Select(g => new
                {
                    id = g.Id,
                    name = g.Name,
                    type = g.Type.ToString(),
                    role = g.UserRole
                });
            }

            // Add roles and permissions
            if (userRoles.Any())
            {
                claims["roles"] = userRoles;
            }

            if (permissions.Any())
            {
                claims["permissions"] = permissions;
            }

            // Add device info if provided
            if (!string.IsNullOrEmpty(deviceId))
            {
                claims["device_id"] = deviceId;
            }

            // Generate access token
            var jti = Guid.NewGuid().ToString();
            claims["jti"] = jti;

            // Convert claims dictionary to Claims list
            var claimsList = new List<System.Security.Claims.Claim>();
            foreach (var claim in claims)
            {
                if (claim.Value is string stringValue)
                {
                    claimsList.Add(new System.Security.Claims.Claim(claim.Key, stringValue));
                }
                else if (claim.Value is IEnumerable<object> enumerable)
                {
                    foreach (var item in enumerable)
                    {
                        claimsList.Add(new System.Security.Claims.Claim(claim.Key, item.ToString() ?? string.Empty));
                    }
                }
                else
                {
                    claimsList.Add(new System.Security.Claims.Claim(claim.Key, claim.Value.ToString() ?? string.Empty));
                }
            }

            var accessToken = _enterpriseTokenService.GenerateAccessToken(claimsList, _accessTokenExpiryMinutes);

            // Generate refresh token
            var refreshTokenResult = await GenerateRefreshTokenAsync();
            if (!refreshTokenResult.IsSuccess)
            {
                return Result<TokenResponse>.Failure("Refresh token oluşturulamadı");
            }

            var refreshToken = refreshTokenResult.Value;

            // Store refresh token in database
            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                JwtId = jti,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_refreshTokenExpiryMinutes),
                UserId = userId,
                GroupId = activeGroup?.Id,
                DeviceId = deviceId,
                IpAddress = GetCurrentIpAddress(),
                UserAgent = GetCurrentUserAgent()
            };

            // Store refresh token (you would implement this in repository)
            // await _refreshTokenRepository.CreateAsync(refreshTokenEntity);

            var expiresAt = DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes);

            // Check if this is a new device
            var isNewDevice = !string.IsNullOrEmpty(deviceId) && await IsNewDeviceAsync(userId, deviceId);

            var tokenResponse = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = expiresAt,
                ExpiresIn = _accessTokenExpiryMinutes * 60,
                User = userDto,
                ActiveGroup = activeGroup,
                AvailableGroups = userGroups,
                Permissions = permissions,
                Roles = userRoles,
                DeviceId = deviceId,
                IsNewDevice = isNewDevice,
                RequiresTwoFactor = user.TwoFactorEnabled && isNewDevice
            };

            _logger.LogInformation("Token generated successfully for user {UserId}", userId);

            return Result<TokenResponse>.Success(tokenResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token for user {UserId}", userId);
            return Result<TokenResponse>.Failure("Token oluşturma sırasında hata oluştu");
        }
    }

    public Task<Result<string>> GenerateRefreshTokenAsync()
    {
        try
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var refreshToken = Convert.ToBase64String(randomBytes);

            return Task.FromResult(Result<string>.Success(refreshToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refresh token");
            return Task.FromResult(Result<string>.Failure("Refresh token oluşturulamadı"));
        }
    }

    public Task<Result<bool>> ValidateRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check refresh token in database
            // var tokenEntity = await _refreshTokenRepository.GetByTokenAsync(refreshToken);
            
            // For now, return true as placeholder
            return Task.FromResult(Result<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            return Task.FromResult(Result<bool>.Failure("Refresh token doğrulanamadı"));
        }
    }

    public async Task<Result<string>> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var principal = _enterpriseTokenService.ValidateToken(token);
            if (principal?.Claims == null)
            {
                return Result<string>.Failure("Geçersiz token");
            }

            var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Result<string>.Failure("Token'da kullanıcı ID'si bulunamadı");
            }

            return Result<string>.Success(userIdClaim);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting user ID from token");
            return Result<string>.Failure("Token'dan kullanıcı ID'si alınamadı");
        }
    }

    public async Task<Result<bool>> IsTokenBlacklistedAsync(string jti, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"blacklisted_token_{jti}";
            var isBlacklisted = await _cacheService.GetAsync<bool?>(cacheKey, cancellationToken);
            
            return Result<bool>.Success(isBlacklisted ?? false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if token is blacklisted");
            return Result<bool>.Success(false); // Assume not blacklisted on error
        }
    }

    public async Task<Result<bool>> BlacklistTokenAsync(string jti, DateTime expiresAt, CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = $"blacklisted_token_{jti}";
            var timeToLive = expiresAt - DateTime.UtcNow;
            
            if (timeToLive > TimeSpan.Zero)
            {
                await _cacheService.SetAsync(cacheKey, true, timeToLive, cancellationToken);
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error blacklisting token");
            return Result<bool>.Failure("Token kara listeye alınamadı");
        }
    }

    #region Private Methods

    private async Task<IEnumerable<string>> GetUserPermissionsForTokenAsync(string userId, Guid? groupId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return new List<string>();

            var permissions = new List<string>();
            var userRoles = await _userManager.GetRolesAsync(user);

            // Add basic permissions
            permissions.Add("profile.read");
            permissions.Add("profile.write");

            // Add role-based permissions
            foreach (var role in userRoles)
            {
                switch (role.ToLower())
                {
                    case "admin":
                        permissions.AddRange(new[]
                        {
                            "users.read", "users.write", "users.delete",
                            "groups.read", "groups.write", "groups.delete",
                            "services.read", "services.write", "services.delete",
                            "permissions.read", "permissions.write",
                            // Added for admin to see/manage roles and categories
                            "roles.read", "roles.write",
                            "categories.read", "categories.write",
                            // SpeedReading resources
                            "reading-texts.read", "reading-texts.write",
                            "exercises.read", "exercises.write",
                            "user-reading-profiles.read"
                        });
                        break;
                    case "manager":
                        permissions.AddRange(new[]
                        {
                            "users.read", "users.write",
                            "groups.read", "groups.write"
                        });
                        break;
                    case "user":
                        permissions.AddRange(new[]
                        {
                            "groups.read"
                        });
                        break;
                }
            }

            // Group-specific permissions
            if (groupId.HasValue)
            {
                var userGroupRole = await _groupService.GetUserRoleInGroupAsync(userId, groupId.Value, cancellationToken);
                if (userGroupRole.IsSuccess && userGroupRole.Value.HasValue)
                {
                    switch (userGroupRole.Value.Value)
                    {
                        case UserGroupRole.Owner:
                        case UserGroupRole.Admin:
                            permissions.AddRange(new[]
                            {
                                $"group.{groupId}.admin",
                                $"group.{groupId}.users.manage"
                            });
                            break;
                        case UserGroupRole.Moderator:
                            permissions.Add($"group.{groupId}.moderate");
                            break;
                        case UserGroupRole.Member:
                            permissions.Add($"group.{groupId}.member");
                            break;
                    }
                }
            }

            return permissions.Distinct();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user permissions for token");
            return new List<string>();
        }
    }

    private async Task<bool> IsNewDeviceAsync(string userId, string deviceId)
    {
        try
        {
            // Check if device exists in database
            // var device = await _userDeviceRepository.GetByUserAndDeviceAsync(userId, deviceId);
            // return device == null;
            
            return false; // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if device is new");
            return false;
        }
    }

    private string? GetCurrentIpAddress()
    {
        // This would typically be injected via IHttpContextAccessor
        return null;
    }

    private string? GetCurrentUserAgent()
    {
        // This would typically be injected via IHttpContextAccessor
        return null;
    }

    #endregion
}