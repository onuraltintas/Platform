using Gateway.Core.Interfaces;
using Gateway.Core.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Enterprise.Shared.Common.Models;
using Enterprise.Shared.Common.Enums;

namespace Gateway.Core.Services;

public class GatewayAuthenticationService : IGatewayAuthenticationService
{
    private readonly ILogger<GatewayAuthenticationService> _logger;
    private readonly GatewayOptions _options;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _tokenValidationParameters;

    public GatewayAuthenticationService(
        ILogger<GatewayAuthenticationService> logger,
        IOptions<GatewayOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _tokenHandler = new JwtSecurityTokenHandler();

        _tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Security.JwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = _options.Security.JwtIssuer,
            ValidateAudience = true,
            ValidAudience = _options.Security.JwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    }

    public async Task<Result<GatewayUserInfo>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Empty token provided for validation");
                return Result<GatewayUserInfo>.Failure("Token is required", OperationStatus.ValidationFailed);
            }

            // Remove Bearer prefix if present
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7);
            }

            var principal = _tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken)
            {
                _logger.LogWarning("Invalid JWT token format");
                return Result<GatewayUserInfo>.Failure("Invalid token format", OperationStatus.ValidationFailed);
            }

            var userInfo = ExtractUserInfoFromClaims(principal.Claims);
            
            _logger.LogDebug("Token validated successfully for user {UserId}", userInfo.UserId);
            return Result<GatewayUserInfo>.Success(userInfo);
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning("Token expired: {Message}", ex.Message);
            return Result<GatewayUserInfo>.Failure("Token has expired", OperationStatus.Unauthorized);
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger.LogWarning("Invalid token signature: {Message}", ex.Message);
            return Result<GatewayUserInfo>.Failure("Invalid token signature", OperationStatus.Unauthorized);
        }
        catch (SecurityTokenValidationException ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return Result<GatewayUserInfo>.Failure("Token validation failed", OperationStatus.Unauthorized);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return Result<GatewayUserInfo>.Failure("Authentication failed", OperationStatus.Failed);
        }
    }

    public async Task<Result<bool>> CheckPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("Empty userId provided for permission check");
                return Result<bool>.Failure("UserId is required", OperationStatus.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(resource))
            {
                _logger.LogWarning("Empty resource provided for permission check");
                return Result<bool>.Failure("Resource is required", OperationStatus.ValidationFailed);
            }

            if (string.IsNullOrWhiteSpace(action))
            {
                _logger.LogWarning("Empty action provided for permission check");
                return Result<bool>.Failure("Action is required", OperationStatus.ValidationFailed);
            }

            // TODO: Implement actual permission checking logic
            // This could involve calling an external authorization service or checking cached permissions
            
            // For now, implement basic role-based access control
            var hasPermission = await CheckUserPermissionAsync(userId, resource, action, cancellationToken);
            
            _logger.LogDebug("Permission check for user {UserId} on resource {Resource} with action {Action}: {Result}", 
                userId, resource, action, hasPermission);

            return Result<bool>.Success(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}", userId);
            return Result<bool>.Failure("Permission check failed", OperationStatus.Failed);
        }
    }

    private GatewayUserInfo ExtractUserInfoFromClaims(IEnumerable<Claim> claims)
    {
        var claimsList = claims.ToList();
        
        var userInfo = new GatewayUserInfo
        {
            UserId = GetClaimValue(claimsList, ClaimTypes.NameIdentifier) ?? GetClaimValue(claimsList, "sub") ?? string.Empty,
            Email = GetClaimValue(claimsList, ClaimTypes.Email) ?? string.Empty,
            FirstName = GetClaimValue(claimsList, ClaimTypes.GivenName) ?? GetClaimValue(claimsList, "given_name"),
            LastName = GetClaimValue(claimsList, ClaimTypes.Surname) ?? GetClaimValue(claimsList, "family_name"),
            GroupName = GetClaimValue(claimsList, "group_name"),
            ExpiresAt = GetTokenExpiryFromClaims(claimsList)
        };

        // Extract roles
        var roleClaims = claimsList.Where(c => c.Type == ClaimTypes.Role || c.Type == "role").Select(c => c.Value);
        userInfo.Roles.AddRange(roleClaims);

        // Extract permissions
        var permissionClaims = claimsList.Where(c => c.Type == "permission" || c.Type == "permissions").Select(c => c.Value);
        userInfo.Permissions.AddRange(permissionClaims);

        // Extract group ID
        var groupIdClaim = GetClaimValue(claimsList, "group_id");
        if (Guid.TryParse(groupIdClaim, out var groupId))
        {
            userInfo.GroupId = groupId;
        }

        return userInfo;
    }

    private static string? GetClaimValue(List<Claim> claims, string claimType)
    {
        return claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    private static DateTime GetTokenExpiryFromClaims(List<Claim> claims)
    {
        var expClaim = GetClaimValue(claims, "exp");
        if (long.TryParse(expClaim, out var exp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
        }
        return DateTime.UtcNow.AddMinutes(60); // Default fallback
    }

    private async Task<bool> CheckUserPermissionAsync(string userId, string resource, string action, CancellationToken cancellationToken)
    {
        // TODO: Implement actual permission checking logic
        // This is a placeholder implementation
        
        await Task.Delay(1, cancellationToken); // Simulate async operation
        
        // Basic resource-action mapping (placeholder)
        var allowedActions = new Dictionary<string, List<string>>
        {
            ["identity"] = new() { "read", "write", "delete" },
            ["users"] = new() { "read", "write", "update" },
            ["notifications"] = new() { "read", "send" },
            ["health"] = new() { "read" }
        };

        if (allowedActions.ContainsKey(resource.ToLower()))
        {
            return allowedActions[resource.ToLower()].Contains(action.ToLower());
        }

        // Default deny
        return false;
    }
}