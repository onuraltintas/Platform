// StyleCop: Dosya başlığı uyarısını proje standardı gereği bastırıyoruz
using System.Diagnostics.CodeAnalysis;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Shared.Errors.Common;

[assembly: SuppressMessage("Style", "SA1633:File header is missing or not located at the top of the file", Justification = "Interface dosyası için basit başlık politikası")]

namespace EgitimPlatform.Services.IdentityService.Services;

public interface IAuthService
{
    Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null);
    Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request, string? ipAddress = null);
    Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null);
    Task<ApiResponse<object>> LogoutAsync(string refreshToken);
    Task<ApiResponse<object>> LogoutAllAsync(string userId);
    Task<ApiResponse<PasswordResetResponse>> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse<PasswordResetResponse>> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse<object>> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<ApiResponse<object>> ChangeUserNameAsync(string userId, ChangeUserNameRequest request);
    Task<ApiResponse<EmailConfirmationResponse>> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<ApiResponse<object>> ResendEmailConfirmationAsync(string email);
    Task<ApiResponse<UserDto>> GetUserAsync(string userId);
    Task<ApiResponse<object>> LockUserAsync(string userId, DateTime? lockoutEnd = null);
    Task<ApiResponse<object>> UnlockUserAsync(string userId);
    Task<ApiResponse<object>> DeactivateUserAsync(string userId);
    Task<ApiResponse<object>> ActivateUserAsync(string userId);
}