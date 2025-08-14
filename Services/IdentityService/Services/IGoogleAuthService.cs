// StyleCop: Dosya başlığı uyarısını proje standardı gereği bastırıyoruz
using System.Diagnostics.CodeAnalysis;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Shared.Errors.Common;

[assembly: SuppressMessage("Style", "SA1633:File header is missing or not located at the top of the file", Justification = "Interface dosyası için basit başlık politikası")]

namespace EgitimPlatform.Services.IdentityService.Services;

public interface IGoogleAuthService
{
    Task<ApiResponse<AuthResponse>> LoginAsync(GoogleLoginRequest request, string? ipAddress = null);
    Task<ApiResponse<RegisterResponse>> RegisterAsync(GoogleRegisterRequest request, string? ipAddress = null);
    Task<ApiResponse<AuthResponse>> HandleCallbackAsync(string code, string state);
    Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken);
    string GetAuthorizationUrl(string state);
}