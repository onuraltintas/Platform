using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.IdentityService.Models.DTOs;

public class GoogleLoginRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; } = false;
    
    public string? DeviceId { get; set; }
}

public class GoogleRegisterRequest
{
    [Required]
    public string IdToken { get; set; } = string.Empty;
    
    public List<string> Categories { get; set; } = new();
    
    public string? DeviceId { get; set; }
}

public class LogoutRequest
{
    public string? RefreshToken { get; set; }
}

public class ResendEmailConfirmationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class GoogleUserInfo
{
    public string Sub { get; set; } = string.Empty; // Google user ID
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
}