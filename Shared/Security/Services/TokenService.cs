using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EgitimPlatform.Shared.Security.Models;

namespace EgitimPlatform.Shared.Security.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly Dictionary<string, RefreshTokenInfo> _refreshTokens; // In production, use distributed cache or database
    
    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _refreshTokens = new Dictionary<string, RefreshTokenInfo>();
    }
    
    public async Task<TokenResult> GenerateTokenAsync(SecurityUser user, string? deviceId = null, string? ipAddress = null)
    {
        var jwtConfig = _configuration.GetSection("Jwt");
        var secretKey = jwtConfig["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtConfig["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
        var audience = jwtConfig["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
        var expiryMinutes = int.Parse(jwtConfig["ExpiryInMinutes"] ?? "60");
        var refreshTokenExpiryDays = int.Parse(jwtConfig["RefreshTokenExpiryInDays"] ?? "7");
        
        user.SessionId = Guid.NewGuid().ToString();
        user.DeviceId = deviceId;
        user.IpAddress = ipAddress;
        user.LastLoginAt = DateTime.UtcNow;
        
        var claims = user.ToClaims();
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        claims.Add(new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));
        
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
        
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );
        
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = GenerateSecureToken();
        
        var refreshTokenInfo = new RefreshTokenInfo
        {
            Token = refreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshTokenExpiryDays),
            DeviceId = deviceId,
            IpAddress = ipAddress
        };
        
        _refreshTokens[refreshToken] = refreshTokenInfo;
        
        _logger.LogInformation("Token generated for user {UserId} with session {SessionId}", user.Id, user.SessionId);
        
        return new TokenResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = user
        };
    }
    
    public async Task<TokenResult?> RefreshTokenAsync(string refreshToken, string? deviceId = null, string? ipAddress = null)
    {
        if (!_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
        {
            _logger.LogWarning("Invalid refresh token provided");
            return null;
        }
        
        if (tokenInfo.IsRevoked || tokenInfo.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired or revoked refresh token used for user {UserId}", tokenInfo.UserId);
            _refreshTokens.Remove(refreshToken);
            return null;
        }
        
        // In a real implementation, you would fetch the user from the database
        var user = new SecurityUser { Id = tokenInfo.UserId };
        
        // Remove old refresh token
        _refreshTokens.Remove(refreshToken);
        
        return await GenerateTokenAsync(user, deviceId, ipAddress);
    }
    
    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        if (_refreshTokens.TryGetValue(refreshToken, out var tokenInfo))
        {
            tokenInfo.IsRevoked = true;
            _logger.LogInformation("Refresh token revoked for user {UserId}", tokenInfo.UserId);
            return true;
        }
        
        return false;
    }
    
    public async Task<bool> RevokeAllUserTokensAsync(string userId)
    {
        var userTokens = _refreshTokens.Where(kvp => kvp.Value.UserId == userId).ToList();
        
        foreach (var token in userTokens)
        {
            token.Value.IsRevoked = true;
        }
        
        _logger.LogInformation("All refresh tokens revoked for user {UserId}", userId);
        return userTokens.Any();
    }
    
    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var jwtConfig = _configuration.GetSection("Jwt");
            var secretKey = jwtConfig["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
            var issuer = jwtConfig["Issuer"];
            var audience = jwtConfig["Audience"];
            
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = !string.IsNullOrEmpty(issuer),
                ValidIssuer = issuer,
                ValidateAudience = !string.IsNullOrEmpty(audience),
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }
    
    public async Task<bool> IsTokenValidAsync(string token)
    {
        var principal = ValidateToken(token);
        return principal != null;
    }
    
    public string GenerateSecureToken()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        return Convert.ToBase64String(randomBytes);
    }
}