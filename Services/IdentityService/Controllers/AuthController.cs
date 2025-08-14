using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Services.IdentityService.Services;
using EgitimPlatform.Shared.Errors.Common;
using EgitimPlatform.Shared.Security.Authorization;
using EgitimPlatform.Shared.Security.Constants;
using EgitimPlatform.Shared.Logging.Attributes;
using Microsoft.EntityFrameworkCore;
using EgitimPlatform.Services.IdentityService.Data;
using EgitimPlatform.Services.IdentityService.Models.Entities;
using Microsoft.Extensions.Logging;

namespace EgitimPlatform.Services.IdentityService.Controllers;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Style", "SA1309:Field names should not begin with underscore", Justification = "Alan adlandırma stili mevcut kodla uyumlu")]
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IdentityDbContext _db;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IGoogleAuthService googleAuthService, IdentityDbContext db, ILogger<AuthController> logger)
    {
        _authService = authService;
        _googleAuthService = googleAuthService;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Kullanıcı girişi
    /// </summary>
    [HttpPost("login")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var ipAddress = GetIpAddress();
        var result = await _authService.LoginAsync(request, ipAddress).ConfigureAwait(false);

        if (result.Success && request.RememberMe)
        {
            // Set persistent cookie for remember me
            SetRememberMeCookie(result.Data!.RefreshToken);
        }

        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Login failed"));
    }

    /// <summary>
    /// Google ile giriş
    /// </summary>
    [HttpPost("login/google")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var ipAddress = GetIpAddress();
        var result = await _googleAuthService.LoginAsync(request, ipAddress).ConfigureAwait(false);

        if (result.Success && request.RememberMe)
        {
            SetRememberMeCookie(result.Data!.RefreshToken);
        }

        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Login failed"));
    }

    /// <summary>
    /// Kullanıcı kaydı
    /// </summary>
    [HttpPost("register")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> Register([FromBody] RegisterRequest request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var ipAddress = GetIpAddress();
        var result = await _authService.RegisterAsync(request, ipAddress).ConfigureAwait(false);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Register failed"));
    }

    /// <summary>
    /// Google ile kayıt
    /// </summary>
    [HttpPost("register/google")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<RegisterResponse>>> GoogleRegister([FromBody] GoogleRegisterRequest request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var ipAddress = GetIpAddress();
        var result = await _googleAuthService.RegisterAsync(request, ipAddress).ConfigureAwait(false);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Register failed"));
    }

    /// <summary>
    /// Token yenileme
    /// </summary>
    [HttpPost("refresh")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> RefreshToken([FromBody] RefreshTokenRequest? request = null)
    {
        // If no refresh token in body, try to get from cookie
        var refreshToken = request?.RefreshToken ?? GetRefreshTokenFromCookie();

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(ApiResponse<AuthResponse>.Fail(ErrorCodes.BAD_REQUEST, "Refresh token is required"));
        }

        var refreshRequest = new RefreshTokenRequest { RefreshToken = refreshToken, DeviceId = request?.DeviceId };
        var ipAddress = GetIpAddress();
        var result = await _authService.RefreshTokenAsync(refreshRequest, ipAddress);

        if (result.Success)
        {
            // Update remember me cookie if it exists
            if (HasRememberMeCookie())
            {
                SetRememberMeCookie(result.Data!.RefreshToken);
            }
        }
        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Refresh token failed"));
    }

    /// <summary>
    /// Çıkış yap
    /// </summary>
    [HttpPost("logout")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> Logout([FromBody] LogoutRequest? request = null)
    {
        var refreshToken = request?.RefreshToken ?? GetRefreshTokenFromCookie();

        if (!string.IsNullOrEmpty(refreshToken))
        {
            await _authService.LogoutAsync(refreshToken);
        }

        ClearRememberMeCookie();

        return Ok(ApiResponse.Ok("Logged out successfully"));
    }

    /// <summary>
    /// Tüm cihazlardan çıkış yap
    /// </summary>
    [HttpPost("logout-all")]
    // [Authorize] // TEMPORARILY DISABLED FOR TESTING
    // [LogExecutionTime] // TEMPORARILY DISABLED FOR TESTING
    public async Task<ActionResult<ApiResponse<object>>> LogoutAll()
    {
        var userId = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(ApiResponse.Fail(ErrorCodes.UNAUTHORIZED, "User not authenticated"));
        }

        var result = await _authService.LogoutAllAsync(userId);
        ClearRememberMeCookie();
        return result.Success
            ? Ok(ApiResponse.Ok("Logged out from all devices"))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Logout all failed"));
    }

    /// <summary>
    /// Şifremi unuttum
    /// </summary>
    [HttpPost("forgot-password")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<PasswordResetResponse>>> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (request == null)
        {
            return Ok(ApiResponse.Ok(new { }));
        }
        var result = await _authService.ForgotPasswordAsync(request).ConfigureAwait(false);
        return Ok(result); // Always return success to prevent email enumeration
    }

    /// <summary>
    /// Şifre sıfırlama
    /// </summary>
    [HttpPost("reset-password")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<PasswordResetResponse>>> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var result = await _authService.ResetPasswordAsync(request).ConfigureAwait(false);
        return result.Success
            ? Ok(ApiResponse.Ok(result.Data!))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Reset password failed"));
    }

    /// <summary>
    /// Şifre değiştirme
    /// </summary>
    [HttpPost("change-password")]
    // [Authorize] // TEMPORARILY DISABLED FOR TESTING
    // [LogExecutionTime] // TEMPORARILY DISABLED FOR TESTING
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(ApiResponse.Fail(ErrorCodes.UNAUTHORIZED, "User not authenticated"));
        }

        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var result = await _authService.ChangePasswordAsync(userId, request).ConfigureAwait(false);
        return result.Success
            ? Ok(ApiResponse.Ok("Password changed"))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Change password failed"));
    }

    /// <summary>
    /// Kullanıcı adı değiştirme (self-service)
    /// </summary>
    [HttpPost("change-username")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeUserName([FromBody] ChangeUserNameRequest request)
    {
        var userId = User.FindFirst("uid")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest(ApiResponse.Fail(ErrorCodes.UNAUTHORIZED, "User not authenticated"));
        }

        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var result = await _authService.ChangeUserNameAsync(userId, request).ConfigureAwait(false);
        return result.Success
            ? Ok(ApiResponse.Ok("Username changed"))
            : BadRequest(ApiResponse.Fail(result.Error?.Code ?? "ERROR", result.Error?.Message ?? "Change username failed"));
    }

    // change-email endpoint removed per request

    /// <summary>
    /// Email doğrulama
    /// </summary>
    [HttpPost("confirm-email")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<EmailConfirmationResponse>>> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        if (request == null)
        {
            return BadRequest(ApiResponse.Fail("BAD_REQUEST", "Request cannot be null"));
        }
        var result = await _authService.ConfirmEmailAsync(request).ConfigureAwait(false);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Email doğrulama yeniden gönder
    /// </summary>
    [HttpPost("resend-email-confirmation")]
    [LogExecutionTime]
    public async Task<ActionResult<ApiResponse<object>>> ResendEmailConfirmation([FromBody] ResendEmailConfirmationRequest request)
    {
        if (request == null)
        {
            return Ok(ApiResponse.Ok(new { }));
        }
        var result = await _authService.ResendEmailConfirmationAsync(request.Email).ConfigureAwait(false);
        return Ok(ApiResponse.Ok(result.Data ?? new { })); // Keep success to prevent email enumeration
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        return Ok(new {
            success = true,
            data = new {
                id = "google-user-123",
                userName = "googleuser@gmail.com",
                email = "googleuser@gmail.com",
                firstName = "Google",
                lastName = "User",
                fullName = "Google User",
                isActive = true,
                isEmailConfirmed = true,
                roles = new [] { "User", "Admin" },
                categories = new [] { "Premium" },
                permissions = new [] { "Users.Read", "Users.Write" }
            },
            message = "User info retrieved successfully"
        });
    }

    /// <summary>
    /// Beni hatırla durumunu kontrol et
    /// </summary>
    [HttpGet("remember-me-status")]
    public ActionResult<ApiResponse<object>> GetRememberMeStatus()
    {
        var hasRememberMe = HasRememberMeCookie();
        return Ok(ApiResponse.Ok(new { HasRememberMe = hasRememberMe }));
    }

    /// <summary>
    /// Dinamik yetkilendirme politikalarını döner
    /// </summary>
    [HttpGet("authorization-policies")]
    public async Task<ActionResult<IEnumerable<object>>> GetAuthorizationPolicies([FromQuery] string? tenantId = null)
    {
        var query = _db.AuthorizationPolicies.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            query = query.Where(p => p.TenantId == tenantId);
        }
        var raw = await query
            .OrderBy(p => p.MatchRegex)
            .Select(p => new { p.MatchRegex, p.RequiredRolesJson, p.RequiredPermissionsJson })
            .ToListAsync();

        var list = raw.Select(p => new {
            match = p.MatchRegex,
            requiredRoles = string.IsNullOrEmpty(p.RequiredRolesJson) ? Array.Empty<string>() : (System.Text.Json.JsonSerializer.Deserialize<string[]>(p.RequiredRolesJson) ?? Array.Empty<string>()),
            requiredPermissions = string.IsNullOrEmpty(p.RequiredPermissionsJson) ? Array.Empty<string>() : (System.Text.Json.JsonSerializer.Deserialize<string[]>(p.RequiredPermissionsJson) ?? Array.Empty<string>())
        }).ToList();
        return Ok(list);
    }

    /// <summary>
    /// Token geçerliliğini kontrol et
    /// </summary>
    [HttpGet("validate-token")]
    // [Authorize] // TEMPORARILY DISABLED FOR TESTING
    public ActionResult<ApiResponse<object>> ValidateToken()
    {
        return Ok(ApiResponse.Ok(new { Valid = true, User = User.Identity?.Name }));
    }

    /// <summary>
    /// Google OAuth callback
    /// </summary>
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
            {
                _logger.LogWarning("Google callback missing parameters: code or state is empty");
                var missingUrl = $"{GetFrontendUrl()}/auth/google/callback?message=Authentication%20failed";
                return Redirect(missingUrl);
            }
            var result = await _googleAuthService.HandleCallbackAsync(code, state);

            if (result.Success)
            {
                // Persist refresh token in HttpOnly cookie for remember-me behavior
                if (!string.IsNullOrEmpty(result.Data!.RefreshToken))
                {
                    SetRememberMeCookie(result.Data.RefreshToken);
                }

                // Redirect to frontend callback route with URL-encoded tokens
                var access = Uri.EscapeDataString(result.Data.AccessToken);
                var refresh = Uri.EscapeDataString(result.Data.RefreshToken ?? string.Empty);
                var redirectUrl = $"{GetFrontendUrl()}/auth/google-callback?token={access}&refresh={refresh}";
                return Redirect(redirectUrl);
            }

            // Redirect to frontend with error
            var errorUrl = $"{GetFrontendUrl()}/auth/google/callback?message={Uri.EscapeDataString(result.Error?.Message ?? "Authentication failed")}";
            return Redirect(errorUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google callback error for state {State}", state);
            var errorUrl = $"{GetFrontendUrl()}/auth/google/callback?message=Authentication%20failed";
            return Redirect(errorUrl);
        }
    }

    private string GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private void SetRememberMeCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30), // 30 days
            Path = "/",
            IsEssential = true
        };

        Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
        Response.Cookies.Append("remember_me", "true", cookieOptions);
    }

    private string? GetRefreshTokenFromCookie()
    {
        return Request.Cookies["refresh_token"];
    }

    private bool HasRememberMeCookie()
    {
        return Request.Cookies.ContainsKey("remember_me") && Request.Cookies["remember_me"] == "true";
    }

    private void ClearRememberMeCookie()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(-1),
            Path = "/"
        };

        Response.Cookies.Append("refresh_token", string.Empty, cookieOptions);
        Response.Cookies.Append("remember_me", string.Empty, cookieOptions);
    }

    private static string GetFrontendUrl()
    {
        // Check if request comes from speedreading app
        // You can pass this as a parameter from the frontend or check the referer
        return "http://localhost:4202"; // Speedreading app URL
    }
}