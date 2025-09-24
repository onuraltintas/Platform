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
    private readonly IPermissionService _permissionService;
    private readonly Enterprise.Shared.Security.Interfaces.ITokenService _enterpriseTokenService;
    private readonly ICacheService _cacheService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<TokenService> _logger;
    private readonly IConfiguration _configuration;

    private readonly int _accessTokenExpiryMinutes;
    private readonly int _refreshTokenExpiryMinutes;

    public TokenService(
        UserManager<ApplicationUser> userManager,
        IUserService userService,
        IGroupService groupService,
        IPermissionService permissionService,
        Enterprise.Shared.Security.Interfaces.ITokenService enterpriseTokenService,
        ICacheService cacheService,
        IRefreshTokenRepository refreshTokenRepository,
        IMapper mapper,
        ILogger<TokenService> logger,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _userService = userService;
        _groupService = groupService;
        _permissionService = permissionService;
        _enterpriseTokenService = enterpriseTokenService;
        _cacheService = cacheService;
        _refreshTokenRepository = refreshTokenRepository;
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

            // Get user permissions from PermissionService
            var permissionsResult = await _permissionService.GetUserPermissionNamesAsync(userId, activeGroup?.Id, cancellationToken);
            var permissions = permissionsResult.IsSuccess ? permissionsResult.Value : new List<string>();

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

                // SuperAdmin gets wildcard permission
                if (userRoles.Contains("SuperAdmin"))
                {
                    claims["is_super"] = true;
                    claims["permissions"] = new List<string> { "*.*.*" };
                }
            }

            // Add regular permissions if not SuperAdmin
            if (!userRoles.Contains("SuperAdmin") && permissions.Any())
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

            // Convert to standard ASP.NET Core Claims list for proper authorization
            var claimsList = new List<System.Security.Claims.Claim>();

            // Add standard identity claims
            claimsList.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId));
            claimsList.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email!));
            claimsList.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()));
            claimsList.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.GivenName, user.FirstName));
            claimsList.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Surname, user.LastName));
            claimsList.Add(new System.Security.Claims.Claim("email_verified", user.EmailConfirmed.ToString().ToLower()));
            claimsList.Add(new System.Security.Claims.Claim("jti", jti));

            // Add roles using standard ClaimTypes.Role
            if (userRoles.Any())
            {
                foreach (var role in userRoles)
                {
                    claimsList.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));
                }

                // Add SuperAdmin flag for easy checking
                if (userRoles.Contains("SuperAdmin"))
                {
                    claimsList.Add(new System.Security.Claims.Claim("is_super", "true"));
                    claimsList.Add(new System.Security.Claims.Claim("permission", "*.*.*"));
                }
            }

            // Add permissions as individual claims
            if (!userRoles.Contains("SuperAdmin") && permissions.Any())
            {
                foreach (var permission in permissions)
                {
                    claimsList.Add(new System.Security.Claims.Claim("permission", permission));
                }
            }

            // Add group information as custom claims (standardized)
            if (activeGroup != null)
            {
                // Backward-compatible: keep old claims
                claimsList.Add(new System.Security.Claims.Claim("active_group_id", activeGroup.Id.ToString()));
                claimsList.Add(new System.Security.Claims.Claim("active_group_name", activeGroup.Name));
                claimsList.Add(new System.Security.Claims.Claim("active_group_type", activeGroup.Type.ToString()));

                // Standard claim used across handlers and gateway
                claimsList.Add(new System.Security.Claims.Claim("GroupId", activeGroup.Id.ToString()));
            }

            // Add device info if provided
            if (!string.IsNullOrEmpty(deviceId))
            {
                claimsList.Add(new System.Security.Claims.Claim("device_id", deviceId));
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

            // Store refresh token securely (hashed)
            await _refreshTokenRepository.CreateAsync(refreshTokenEntity, cancellationToken);

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
            var hashed = Hash(refreshToken);
            return ValidateHashedRefreshTokenAsync(hashed, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating refresh token");
            return Task.FromResult(Result<bool>.Failure("Refresh token doğrulanamadı"));
        }
    }

    private async Task<Result<bool>> ValidateHashedRefreshTokenAsync(string hashedToken, CancellationToken cancellationToken)
    {
        var entity = await _refreshTokenRepository.GetByHashedTokenAsync(hashedToken, cancellationToken);
        if (entity == null) return Result<bool>.Success(false);

        if (entity.IsRevoked) return Result<bool>.Success(false);
        if (entity.IsUsed) return Result<bool>.Success(false);
        if (entity.ExpiresAt < DateTime.UtcNow) return Result<bool>.Success(false);

        return Result<bool>.Success(true);
    }

    private static string Hash(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
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