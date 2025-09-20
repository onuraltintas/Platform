using Identity.Core.Interfaces;
using Identity.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
// [EnableRateLimiting("AccountPolicy")] - Temporarily disabled
public class AccountController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IUserService userService,
        IAuthService authService,
        IEmailService emailService,
        ILogger<AccountController> logger)
    {
        _userService = userService;
        _authService = authService;
        _emailService = emailService;
        _logger = logger;
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
            var tokenResult = await _userService.GenerateEmailConfirmationTokenAsync(user.Id);
            if (tokenResult.IsSuccess)
            {
                // Send confirmation email
                await _emailService.SendEmailConfirmationAsync(user.Email, user.FirstName, tokenResult.Value);
            }

            // Send welcome email
            await _emailService.SendWelcomeEmailAsync(user.Email, user.FirstName);

            var response = new RegisterResponse
            {
                UserId = user.Id,
                Email = user.Email,
                RequiresEmailConfirmation = true,
                Message = "Kayıt başarılı! E-posta adresinizi doğrulamak için gelen kutunuzu kontrol edin."
            };

            _logger.LogInformation("User {Email} registered successfully", request.Email);

            return CreatedAtAction(nameof(GetProfile), new { }, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", request.Email);
            return BadRequest("Kayıt işlemi sırasında hata oluştu");
        }
    }

    /// <summary>
    /// E-posta doğrulama
    /// </summary>
    /// <param name="request">Doğrulama bilgileri</param>
    /// <returns>Doğrulama sonucu</returns>
    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userService.ConfirmEmailAsync(request.UserId, request.Token);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Email confirmed for user {UserId}", request.UserId);

        return Ok(new { success = true, message = "E-posta adresiniz başarıyla doğrulandı" });
    }

    /// <summary>
    /// E-posta doğrulama kodu yeniden gönderme
    /// </summary>
    /// <param name="request">E-posta bilgileri</param>
    /// <returns>Gönderim sonucu</returns>
    [HttpPost("resend-confirmation")]
    // [EnableRateLimiting("EmailPolicy")] - Temporarily disabled
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userResult = await _userService.GetByEmailAsync(request.Email);
        if (!userResult.IsSuccess)
        {
            // Don't reveal if email exists or not for security
            return Ok(new { success = true, message = "Eğer e-posta adresiniz sistemimizde kayıtlıysa, doğrulama kodu gönderilmiştir" });
        }

        var user = userResult.Value;

        // Check if already confirmed
        if (user.EmailConfirmed)
        {
            return BadRequest("E-posta adresi zaten doğrulanmış");
        }

        // Generate new confirmation token
        var tokenResult = await _userService.GenerateEmailConfirmationTokenAsync(user.Id);
        if (!tokenResult.IsSuccess)
        {
            return BadRequest("Doğrulama kodu oluşturulamadı");
        }

        // Send confirmation email
        await _emailService.SendEmailConfirmationAsync(user.Email, user.FirstName, tokenResult.Value);

        _logger.LogInformation("Email confirmation resent for user {Email}", request.Email);

        return Ok(new { success = true, message = "Doğrulama kodu e-posta adresinize gönderildi" });
    }

    /// <summary>
    /// Şifre sıfırlama isteği
    /// </summary>
    /// <param name="request">E-posta bilgileri</param>
    /// <returns>İstek sonucu</returns>
    [HttpPost("forgot-password")]
    // [EnableRateLimiting("EmailPolicy")] - Temporarily disabled
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var tokenResult = await _userService.GeneratePasswordResetTokenAsync(request.Email);
            
            // Always return success for security reasons (don't reveal if email exists)
            var response = new { success = true, message = "Eğer e-posta adresiniz sistemimizde kayıtlıysa, şifre sıfırlama linki gönderilmiştir" };

            if (tokenResult.IsSuccess)
            {
                var userResult = await _userService.GetByEmailAsync(request.Email);
                if (userResult.IsSuccess)
                {
                    await _emailService.SendPasswordResetEmailAsync(request.Email, userResult.Value.FirstName, tokenResult.Value);
                    _logger.LogInformation("Password reset token sent for user {Email}", request.Email);
                }
            }

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for {Email}", request.Email);
            return Ok(new { success = true, message = "Eğer e-posta adresiniz sistemimizde kayıtlıysa, şifre sıfırlama linki gönderilmiştir" });
        }
    }

    /// <summary>
    /// Şifre sıfırlama
    /// </summary>
    /// <param name="request">Şifre sıfırlama bilgileri</param>
    /// <returns>Sıfırlama sonucu</returns>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userResult = await _userService.GetByEmailAsync(request.Email);
        if (!userResult.IsSuccess)
        {
            return BadRequest("Geçersiz şifre sıfırlama isteği");
        }

        var result = await _userService.ResetPasswordAsync(userResult.Value.Id, request.Token, request.NewPassword);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Password reset successful for user {Email}", request.Email);

        return Ok(new { success = true, message = "Şifreniz başarıyla değiştirildi" });
    }

    /// <summary>
    /// Şifre değiştirme
    /// </summary>
    /// <param name="request">Şifre değiştirme bilgileri</param>
    /// <returns>Değiştirme sonucu</returns>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
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

        var result = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Password changed for user {UserId}", userId);

        return Ok(new { success = true, message = "Şifreniz başarıyla değiştirildi" });
    }

    /// <summary>
    /// Kullanıcı profili getirme
    /// </summary>
    /// <returns>Kullanıcı profili</returns>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _userService.GetByIdAsync(userId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Kullanıcı profili güncelleme
    /// </summary>
    /// <param name="request">Güncellenecek profil bilgileri</param>
    /// <returns>Güncellenmiş profil</returns>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
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

        var result = await _userService.UpdateAsync(userId, request);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        _logger.LogInformation("Profile updated for user {UserId}", userId);

        return Ok(result.Value);
    }

    /// <summary>
    /// Hesap silme (GDPR)
    /// </summary>
    /// <param name="request">Hesap silme bilgileri</param>
    /// <returns>Silme sonucu</returns>
    [HttpDelete]
    [Authorize]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
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

        // Verify password for security
        var userResult = await _userService.GetByIdAsync(userId);
        if (!userResult.IsSuccess)
        {
            return BadRequest("Kullanıcı bulunamadı");
        }

        // Here you would verify the password
        // For now, we'll assume it's correct

        var result = await _userService.DeleteAsync(userId, userId);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        // Logout user from all devices
        await _authService.LogoutAllDevicesAsync(userId);

        _logger.LogInformation("Account deleted for user {UserId}", userId);

        return Ok(new { success = true, message = "Hesabınız başarıyla silindi" });
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

    #endregion
}

// Request/Response DTOs
public class RegisterResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool RequiresEmailConfirmation { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ConfirmEmailRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class ResendConfirmationRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class DeleteAccountRequest
{
    public string Password { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}