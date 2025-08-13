namespace EgitimPlatform.Shared.Configuration.ConfigurationOptions;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiryInMinutes { get; set; } = 60;
    public int RefreshTokenExpiryInDays { get; set; } = 7;
    public string Algorithm { get; set; } = "HS256";
}