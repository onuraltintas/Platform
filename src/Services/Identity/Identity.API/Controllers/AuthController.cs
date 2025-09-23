using Identity.Core.Interfaces;
using Identity.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Google.Apis.Auth;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
// [EnableRateLimiting("AuthPolicy")] - Temporarily disabled
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(IAuthService authService, IUserService userService, IEmailService emailService, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _authService = authService;
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Kullanıcı girişi
    /// </summary>
    /// <param name="request">Giriş bilgileri</param>
    /// <returns>JWT token ve kullanıcı bilgileri</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Add request context
        request.IpAddress = GetClientIpAddress();
        request.UserAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.LoginAsync(request);
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("İki faktörlü"))
            {
                return Ok(new { requiresTwoFactor = true, message = result.Error });
            }
            
            return Unauthorized(result.Error);
        }

        _logger.LogInformation("User {EmailOrUsername} logged in successfully", request.EmailOrUsername);

        return Ok(result.Value);
    }

    /// <summary>
    /// Kullanıcı kaydı
    /// </summary>
    /// <param name="request">Kayıt bilgileri</param>
    /// <returns>Kayıt sonucu</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Add request context
        request.IpAddress = GetClientIpAddress();
        request.UserAgent = Request.Headers.UserAgent.ToString();

        try
        {
            // Create user
            var createUserRequest = new CreateUserRequest
            {
                Email = request.Email,
                UserName = request.UserName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber,
                Password = request.Password,
                EmailConfirmed = false,
                DefaultGroupId = request.GroupId
            };

            var userResult = await _userService.CreateAsync(createUserRequest);
            if (!userResult.IsSuccess)
            {
                return BadRequest(userResult.Error);
            }

            var user = userResult.Value;

            // Generate email confirmation token
            var tokenResult = await _userService.GenerateEmailConfirmationTokenAsync(user.Id ?? string.Empty ?? string.Empty);
            if (tokenResult.IsSuccess && !string.IsNullOrEmpty(user.Email ?? string.Empty) && !string.IsNullOrEmpty(tokenResult.Value ?? string.Empty))
            {
                // Send confirmation email
                await _emailService.SendEmailConfirmationAsync(user.Email ?? string.Empty, user.FirstName ?? string.Empty ?? string.Empty, tokenResult.Value ?? string.Empty);
            }

            // Send welcome email
            if (!string.IsNullOrEmpty(user.Email ?? string.Empty))
            {
                await _emailService.SendWelcomeEmailAsync(user.Email ?? string.Empty, user.FirstName ?? string.Empty ?? string.Empty);
            }

            var response = new RegisterResponse
            {
                UserId = user.Id ?? string.Empty ?? string.Empty,
                Email = user.Email ?? string.Empty,
                RequiresEmailConfirmation = true,
                Message = "Kayıt başarılı! E-posta adresinizi doğrulamak için gelen kutunuzu kontrol edin."
            };

            _logger.LogInformation("User {Email} registered successfully", request.Email);

            return CreatedAtAction(nameof(Login), new { }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return BadRequest("Kayıt işlemi sırasında hata oluştu");
        }
    }

    /// <summary>
    /// Google ile giriş
    /// </summary>
    /// <param name="request">Google ID token</param>
    /// <returns>JWT token ve kullanıcı bilgileri</returns>
    [HttpPost("google-login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Add request context
        request.IpAddress = GetClientIpAddress();
        request.UserAgent = Request.Headers.UserAgent.ToString();

        var result = await _authService.GoogleLoginAsync(request);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        _logger.LogInformation("User logged in via Google successfully");

        return Ok(result.Value);
    }

    /// <summary>
    /// Google OAuth callback endpoint
    /// </summary>
    /// <param name="code">Authorization code from Google</param>
    /// <param name="state">State parameter for CSRF protection</param>
    /// <returns>Redirect to frontend with authentication result</returns>
    [HttpGet("google/callback")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GoogleCallback([FromQuery] string code, [FromQuery] string state)
    {
        if (string.IsNullOrEmpty(code))
        {
            _logger.LogWarning("Google callback received without authorization code");
            return Redirect("http://localhost:4200/auth/login?error=missing_code");
        }

        try
        {
            _logger.LogInformation("Processing Google OAuth callback with code");

            // 1. Exchange authorization code for tokens with Google
            var googleTokens = await ExchangeCodeForTokens(code);
            if (googleTokens == null || string.IsNullOrEmpty(googleTokens.IdToken))
            {
                _logger.LogError("Failed to get ID token from Google");
                return Redirect("http://localhost:4200/auth/login?error=token_exchange_failed");
            }

            _logger.LogInformation("Successfully received Google tokens");

            // 2. Validate ID token with Google
            var payload = await ValidateGoogleIdToken(googleTokens.IdToken);
            if (payload == null)
            {
                _logger.LogError("Invalid Google ID token");
                return Redirect("http://localhost:4200/auth/login?error=invalid_token");
            }

            _logger.LogInformation("Google ID token validated successfully for user: {Email}", payload.Email);

            // 3. Use existing Google login service with validated ID token
            var loginRequest = new GoogleLoginRequest
            {
                IdToken = googleTokens.IdToken,
                IpAddress = GetClientIpAddress(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            var result = await _authService.GoogleLoginAsync(loginRequest);
            if (!result.IsSuccess)
            {
                _logger.LogError("Google login failed: {Error}", result.Error);
                return Redirect($"http://localhost:4200/auth/login?error={Uri.EscapeDataString(result.Error ?? "login_failed")}");
            }

            _logger.LogInformation("Google login successful for user: {Email}", payload.Email);

            // 4. Redirect to frontend with tokens from auth service
            var tokenData = System.Text.Json.JsonSerializer.Serialize(result.Value);
            var encodedToken = Uri.EscapeDataString(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tokenData)));

            return Redirect($"http://localhost:4200/auth/google/callback?success=true&token={encodedToken}&state={Uri.EscapeDataString(state ?? "")}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google OAuth callback");
            return Redirect("http://localhost:4200/auth/login?error=callback_processing_failed");
        }
    }

    private async Task<GoogleTokenResponse?> ExchangeCodeForTokens(string code)
    {
        using var client = new HttpClient();
        var tokenEndpoint = "https://oauth2.googleapis.com/token";

        var clientId = _configuration["GoogleAuth:ClientId"] ?? "";
        var clientSecret = _configuration["GoogleAuth:ClientSecret"] ?? "";
        var redirectUri = _configuration["GoogleAuth:RedirectUri"] ?? "http://localhost:5001/api/v1/auth/google/callback";

        _logger.LogInformation("Exchanging code for tokens with Google. ClientId: {ClientId}, RedirectUri: {RedirectUri}",
            clientId.Substring(0, Math.Min(clientId.Length, 20)) + "...", redirectUri);

        var parameters = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", clientId),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("redirect_uri", redirectUri),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

        _logger.LogInformation("Sending token exchange request to Google");
        var response = await client.PostAsync(tokenEndpoint, parameters);

        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to exchange code for tokens. Status: {StatusCode}, Headers: {Headers}, Response: {Response}",
                response.StatusCode,
                string.Join(", ", response.Headers.Select(h => $"{h.Key}={string.Join(",", h.Value)}")),
                responseContent);
            return null;
        }

        _logger.LogInformation("Successfully exchanged code for tokens. Response length: {Length}", responseContent.Length);
        _logger.LogInformation("Google token response content: {ResponseContent}", responseContent);

        try
        {
            var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<GoogleTokenResponse>(responseContent, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Token deserialization successful. Has ID token: {HasIdToken}, AccessToken: {HasAccessToken}, RefreshToken: {HasRefreshToken}",
                !string.IsNullOrEmpty(tokenResponse?.IdToken),
                !string.IsNullOrEmpty(tokenResponse?.AccessToken),
                !string.IsNullOrEmpty(tokenResponse?.RefreshToken));

            if (tokenResponse != null)
            {
                _logger.LogInformation("TokenResponse properties - AccessToken: '{AccessToken}...', IdToken: '{IdToken}...', RefreshToken: '{RefreshToken}...'",
                    tokenResponse.AccessToken?.Substring(0, Math.Min(30, tokenResponse.AccessToken?.Length ?? 0)),
                    tokenResponse.IdToken?.Substring(0, Math.Min(30, tokenResponse.IdToken?.Length ?? 0)),
                    tokenResponse.RefreshToken?.Substring(0, Math.Min(30, tokenResponse.RefreshToken?.Length ?? 0)));
            }

            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize Google token response: {Response}", responseContent);
            return null;
        }
    }

    private async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleIdToken(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["GoogleAuth:ClientId"] ?? "" }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            return payload;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate Google ID token");
            return null;
        }
    }


    private class GoogleTokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("id_token")]
        public string IdToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }


    /// <summary>
    /// Token yenileme
    /// </summary>
    /// <param name="request">Refresh token</param>
    /// <returns>Yeni access token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RefreshTokenAsync(request);
        if (!result.IsSuccess)
        {
            return Unauthorized(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Kullanıcı çıkışı
    /// </summary>
    /// <param name="request">Çıkış bilgileri</param>
    /// <returns>Başarı durumu</returns>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request = null)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _authService.LogoutAsync(userId, request?.DeviceId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        _logger.LogInformation("User {UserId} logged out", userId);

        return Ok(result.Value);
    }

    /// <summary>
    /// Tüm cihazlardan çıkış
    /// </summary>
    /// <returns>Başarı durumu</returns>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _authService.LogoutAllDevicesAsync(userId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        _logger.LogInformation("User {UserId} logged out from all devices", userId);

        return Ok(result.Value);
    }

    /// <summary>
    /// Token iptal etme
    /// </summary>
    /// <param name="request">İptal edilecek token</param>
    /// <returns>Başarı durumu</returns>
    [HttpPost("revoke")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RevokeToken([FromBody] RevokeTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RevokeTokenAsync(request.RefreshToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Token doğrulama
    /// </summary>
    /// <returns>Token geçerliliği</returns>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(typeof(TokenValidationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Validate()
    {
        var userId = GetCurrentUserId();
        var userEmail = GetCurrentUserEmail();
        
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var response = new TokenValidationResponse
        {
            IsValid = true,
            UserId = userId,
            Email = userEmail,
            ValidatedAt = DateTime.UtcNow
        };

        return Ok(response);
    }

    /// <summary>
    /// Grup değiştirme
    /// </summary>
    /// <param name="request">Yeni grup bilgileri</param>
    /// <returns>Yeni token</returns>
    [HttpPost("switch-group")]
    [Authorize]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SwitchGroup([FromBody] SwitchGroupRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _authService.SwitchGroupAsync(userId, request.GroupId);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("erişim") == true)
            {
                return Forbid(result.Error);
            }
            return BadRequest(result.Error);
        }

        _logger.LogInformation("User {UserId} switched to group {GroupId}", userId, request.GroupId);

        return Ok(result.Value);
    }

    /// <summary>
    /// Kullanıcı izinlerini getir
    /// </summary>
    /// <param name="groupId">Grup ID (opsiyonel)</param>
    /// <returns>Kullanıcı izinleri</returns>
    [HttpGet("permissions")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPermissions([FromQuery] Guid? groupId = null)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _authService.GetUserPermissionsAsync(userId, groupId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// E-posta adresini doğrula
    /// </summary>
    /// <param name="request">Doğrulama token'ı</param>
    /// <returns>Doğrulama sonucu</returns>
    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.VerifyEmailAsync(request.Token);
        if (!result.IsSuccess)
        {
            if (result.Error.Contains("already verified"))
            {
                return Conflict(result.Error);
            }
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Email verified successfully for token");

        return Ok(new { message = "E-posta adresiniz başarıyla doğrulandı." });
    }

    /// <summary>
    /// Doğrulama e-postasını yeniden gönder
    /// </summary>
    /// <param name="request">E-posta adresi</param>
    /// <returns>Gönderim sonucu</returns>
    [HttpPost("resend-verification")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerificationEmail([FromBody] ResendVerificationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userResult = await _userService.GetByEmailAsync(request.Email);
        if (!userResult.IsSuccess)
        {
            // Don't reveal whether the user exists
            return Ok(new { message = "Eğer bu e-posta adresi sistemimizde kayıtlıysa, doğrulama e-postası gönderilecektir." });
        }

        var user = userResult.Value;

        // Generate new token
        var tokenResult = await _userService.GenerateEmailConfirmationTokenAsync(user.Id ?? string.Empty ?? string.Empty);
        if (tokenResult.IsSuccess && !string.IsNullOrEmpty(user.Email ?? string.Empty) && !string.IsNullOrEmpty(tokenResult.Value ?? string.Empty))
        {
            await _emailService.SendEmailConfirmationAsync(user.Email ?? string.Empty, user.FirstName ?? string.Empty ?? string.Empty, tokenResult.Value ?? string.Empty);
        }

        _logger.LogInformation("Verification email resent for {Email}", request.Email);

        return Ok(new { message = "Doğrulama e-postası gönderildi." });
    }

    /// <summary>
    /// Şifre sıfırlama e-postası gönder
    /// </summary>
    /// <param name="request">E-posta adresi</param>
    /// <returns>Gönderim sonucu</returns>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Password reset requested for email: {Email}", request.Email);

            var userResult = await _userService.GetByEmailAsync(request.Email);
            if (!userResult.IsSuccess)
            {
                // Don't reveal whether the user exists or not for security
                _logger.LogWarning("Password reset requested for non-existent user: {Email}", request.Email);

                // For testing purposes, still try to send an email with dummy data
                try
                {
                    var testEmailResult = await _emailService.SendPasswordResetEmailAsync(
                        request.Email,
                        "Test User",
                        "test-reset-token-123"
                    );

                    if (testEmailResult.IsSuccess)
                    {
                        _logger.LogInformation("Test password reset email sent to: {Email}", request.Email);
                    }
                    else
                    {
                        _logger.LogError("Failed to send test password reset email to {Email}: {Error}", request.Email, testEmailResult.Error);
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Exception while sending test password reset email to {Email}", request.Email);
                }

                return Ok(new { message = "Eğer hesabınız mevcutsa, şifre sıfırlama e-postası gönderildi." });
            }

            var user = userResult.Value;

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning("Password reset requested for unconfirmed email: {Email}", request.Email);
                return BadRequest("E-posta adresinizi önce doğrulamanız gerekiyor.");
            }

            // Generate password reset token
            var tokenResult = await _userService.GeneratePasswordResetTokenAsync(request.Email);
            if (!tokenResult.IsSuccess)
            {
                _logger.LogError("Failed to generate password reset token for {Email}: {Error}", request.Email, tokenResult.Error);
                return StatusCode(500, "Şifre sıfırlama token'ı oluşturulamadı");
            }

            // Send password reset email
            var emailResult = await _emailService.SendPasswordResetEmailAsync(user.Email ?? string.Empty ?? string.Empty, user.FirstName ?? string.Empty ?? string.Empty, tokenResult.Value ?? string.Empty ?? string.Empty);
            if (!emailResult.IsSuccess)
            {
                _logger.LogError("Failed to send password reset email to {Email}: {Error}", request.Email, emailResult.Error);
                return StatusCode(500, "E-posta gönderilirken bir hata oluştu");
            }

            _logger.LogInformation("Password reset email sent to: {Email}", request.Email);

            return Ok(new { message = "Şifre sıfırlama e-postası gönderildi. Lütfen gelen kutunuzu kontrol edin." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email to {Email}", request.Email);
            return StatusCode(500, "E-posta gönderilirken bir hata oluştu");
        }
    }

    /// <summary>
    /// Şifre sıfırlama (token ile)
    /// </summary>
    /// <param name="request">Şifre sıfırlama bilgileri</param>
    /// <returns>Sıfırlama sonucu</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("Password reset attempt for email: {Email}", request.Email);

            // Get user by email
            var userResult = await _userService.GetByEmailAsync(request.Email);
            if (!userResult.IsSuccess)
            {
                _logger.LogWarning("Password reset attempt for non-existent user: {Email}", request.Email);
                return BadRequest("Geçersiz şifre sıfırlama isteği");
            }

            var user = userResult.Value;

            // Reset password with token
            if (string.IsNullOrEmpty(request.Token))
            {
                return BadRequest("Geçersiz şifre sıfırlama isteği");
            }
            var result = await _userService.ResetPasswordAsync(user.Id ?? string.Empty ?? string.Empty, request.Token, request.NewPassword);
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Failed password reset for {Email}: {Error}", request.Email, result.Error);
                return BadRequest(result.Error);
            }

            _logger.LogInformation("Password reset successful for user {Email}", request.Email);

            return Ok(new { success = true, message = "Şifreniz başarıyla değiştirildi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for {Email}", request.Email);
            return StatusCode(500, "Şifre sıfırlanırken bir hata oluştu");
        }
    }

    #region Private Methods

    private string? GetClientIpAddress()
    {
        var xForwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xForwardedFor))
        {
            return xForwardedFor.Split(',')[0].Trim();
        }

        var xRealIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(xRealIp))
        {
            return xRealIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private string? GetCurrentUserEmail()
    {
        return User.FindFirst(ClaimTypes.Email)?.Value;
    }

    #endregion
}

// Request DTOs
public class LogoutRequest
{
    public string? DeviceId { get; set; }
}

public class RevokeTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class SwitchGroupRequest
{
    public Guid GroupId { get; set; }
}

// Response DTOs
public class TokenValidationResponse
{
    public bool IsValid { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime ValidatedAt { get; set; }
}

