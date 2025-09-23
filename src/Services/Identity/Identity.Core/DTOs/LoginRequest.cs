using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Identity.Core.DTOs;

public class LoginRequest
{
    [Required(ErrorMessage = "E-posta adresi veya kullanıcı adı gereklidir")]
    public string EmailOrUsername { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir")]
    [MinLength(6, ErrorMessage = "Şifre en az 6 karakter olmalıdır")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = false;

    public Guid? GroupId { get; set; }

    public string? TwoFactorCode { get; set; }

    // Device information
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }

    // Aliases for backward compatibility with clients sending different field names
    // Map 'email' to EmailOrUsername if provided
    [JsonPropertyName("email")]
    public string? Email
    {
        get => EmailOrUsername;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                EmailOrUsername = value!;
            }
        }
    }

    // Map 'username' to EmailOrUsername if provided
    [JsonPropertyName("username")]
    public string? Username
    {
        get => EmailOrUsername;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                EmailOrUsername = value!;
            }
        }
    }

    // Note: System.Text.Json is case-insensitive by default; 'username' covers 'userName' too
}

public class GoogleLoginRequest
{
    [Required(ErrorMessage = "Google ID token gereklidir")]
    public string IdToken { get; set; } = string.Empty;

    public Guid? GroupId { get; set; }

    // Device information
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token gereklidir")]
    public string RefreshToken { get; set; } = string.Empty;

    public string? DeviceId { get; set; }
}

public class VerifyEmailRequest
{
    [Required(ErrorMessage = "Doğrulama token'ı gereklidir")]
    public string Token { get; set; } = string.Empty;
}

public class ResendVerificationRequest
{
    [Required(ErrorMessage = "E-posta adresi gereklidir")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "E-posta adresi gereklidir")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "E-posta adresi gereklidir")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sıfırlama token'ı gereklidir")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre gereklidir")]
    [MinLength(8, ErrorMessage = "Şifre en az 8 karakter olmalıdır")]
    public string NewPassword { get; set; } = string.Empty;
}