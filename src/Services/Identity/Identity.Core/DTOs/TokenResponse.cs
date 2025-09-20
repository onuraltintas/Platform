namespace Identity.Core.DTOs;

public class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    
    // User Information
    public UserDto User { get; set; } = null!;
    
    // Group Information
    public GroupDto? ActiveGroup { get; set; }
    public IEnumerable<GroupDto> AvailableGroups { get; set; } = new List<GroupDto>();
    
    // Permissions
    public IEnumerable<string> Permissions { get; set; } = new List<string>();
    public IEnumerable<string> Roles { get; set; } = new List<string>();
    
    // Device Information
    public string? DeviceId { get; set; }
    public bool IsNewDevice { get; set; }
    public bool RequiresTwoFactor { get; set; }
}

public class RefreshTokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    
    // Updated permissions (in case they changed)
    public IEnumerable<string> Permissions { get; set; } = new List<string>();
}