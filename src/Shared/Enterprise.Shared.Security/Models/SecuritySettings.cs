using System.ComponentModel.DataAnnotations;

namespace Enterprise.Shared.Security.Models;

/// <summary>
/// Security configuration settings
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Security";

    // Encryption Settings
    [Required]
    public string? EncryptionKey { get; set; }
    public string? EncryptionIV { get; set; }
    public bool UseDataProtectionApi { get; set; } = true;

    // JWT Settings
    [Required]
    public string? JwtSecretKey { get; set; }
    
    [Required]
    public string JwtIssuer { get; set; } = "Enterprise.Platform";
    
    [Required]
    public string JwtAudience { get; set; } = "Enterprise.Platform.Users";
    
    [Range(1, 10080)]
    public int JwtAccessTokenExpirationMinutes { get; set; } = 60;
    
    [Range(1, 365)]
    public int RefreshTokenExpirationDays { get; set; } = 30;
    
    [Range(0, 60)]
    public int? JwtClockSkewMinutes { get; set; } = 5;

    // Password Policy
    [Range(6, 128)]
    public int? PasswordMinLength { get; set; } = 8;
    
    [Range(1, 32)]
    public int PasswordMaxLength { get; set; } = 128;
    
    public bool? PasswordRequireUppercase { get; set; } = true;
    public bool? PasswordRequireLowercase { get; set; } = true;
    public bool? PasswordRequireDigit { get; set; } = true;
    public bool? PasswordRequireSpecialChar { get; set; } = true;
    
    [Range(10, 15)]
    public int? BCryptWorkFactor { get; set; } = 12;

    // API Key Settings
    [Range(1, 3650)]
    public int? ApiKeyExpirationDays { get; set; } = 365;
    
    [Range(1, 100000)]
    public int? ApiKeyRateLimit { get; set; } = 1000;

    // Security Audit Settings
    [Range(1, 20)]
    public int? MaxFailedLoginAttempts { get; set; } = 5;
    
    [Range(1, 1440)]
    public int? FailedLoginWindowMinutes { get; set; } = 30;

    // CORS Settings
    public bool EnableCors { get; set; } = true;
    public string[]? AllowedOrigins { get; set; }
    public string[]? AllowedMethods { get; set; } = { "GET", "POST", "PUT", "DELETE", "OPTIONS" };
    public string[]? AllowedHeaders { get; set; } = { "*" };
    public bool AllowCredentials { get; set; } = false;

    // Security Headers
    public bool EnableSecurityHeaders { get; set; } = true;
    public bool EnableHsts { get; set; } = true;
    public int HstsMaxAgeSeconds { get; set; } = 31536000;
    public bool EnableXssProtection { get; set; } = true;
    public bool EnableContentTypeNosniff { get; set; } = true;
    public bool EnableFrameOptions { get; set; } = true;
    public string FrameOptionsPolicy { get; set; } = "DENY";

    // Rate Limiting
    public bool EnableRateLimiting { get; set; } = true;
    public int RateLimitWindowSeconds { get; set; } = 60;
    public int RateLimitPermitLimit { get; set; } = 100;
    public int RateLimitQueueLimit { get; set; } = 2;

    // IP Blocking
    public bool EnableIpBlocking { get; set; } = true;
    public string[]? WhitelistedIps { get; set; }
    public string[]? BlacklistedIps { get; set; }
    public int? IpBlockDurationMinutes { get; set; } = 60;
}