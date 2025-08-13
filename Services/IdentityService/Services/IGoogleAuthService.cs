using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Shared.Errors.Common;

namespace EgitimPlatform.Services.IdentityService.Services;

public interface IGoogleAuthService
{
    Task<ApiResponse<AuthResponse>> LoginAsync(GoogleLoginRequest request, string? ipAddress = null);
    Task<ApiResponse<RegisterResponse>> RegisterAsync(GoogleRegisterRequest request, string? ipAddress = null);
    Task<ApiResponse<AuthResponse>> HandleCallbackAsync(string code, string state);
    Task<GoogleUserInfo?> ValidateGoogleTokenAsync(string idToken);
    string GetAuthorizationUrl(string state);
}