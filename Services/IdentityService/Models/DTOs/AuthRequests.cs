using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.IdentityService.Models.DTOs;

public class LoginRequest
{
    public string EmailOrUsername { get; set; } = string.Empty;
    
    public string Password { get; set; } = string.Empty;
    
    public bool RememberMe { get; set; } = false;
    
    public string? DeviceId { get; set; }
}

public class RegisterRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string UserName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
    
    [Phone]
    public string? PhoneNumber { get; set; }
    
    public List<string> Categories { get; set; } = new();
}

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
    
    public string? DeviceId { get; set; }
}

public class ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Token { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;
    
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
    
    [Required]
    [Compare("NewPassword")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ConfirmEmailRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class ChangeUserNameRequest
{
    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string NewUserName { get; set; } = string.Empty;
}