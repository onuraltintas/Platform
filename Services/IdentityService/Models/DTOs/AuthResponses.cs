namespace EgitimPlatform.Services.IdentityService.Models.DTOs;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserDto User { get; set; } = new();
}

public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsEmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}

public class RegisterResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool RequiresEmailConfirmation { get; set; } = true;
}

public class PasswordResetResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}

public class EmailConfirmationResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}