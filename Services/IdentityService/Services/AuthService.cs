using Microsoft.EntityFrameworkCore;
using AutoMapper;
using EgitimPlatform.Services.IdentityService.Data;
using EgitimPlatform.Services.IdentityService.Models.DTOs;
using EgitimPlatform.Services.IdentityService.Models.Entities;
using EgitimPlatform.Shared.Errors.Common;
using EgitimPlatform.Shared.Errors.Exceptions;
using EgitimPlatform.Shared.Security.Services;
using EgitimPlatform.Shared.Security.Models;
using EgitimPlatform.Shared.Logging.Services;
using EgitimPlatform.Shared.Email.Services;
using System.Diagnostics.CodeAnalysis;

namespace EgitimPlatform.Services.IdentityService.Services;

[SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Service layer loglayıp ApiResponse döndürür; global exception handler genel durumları ele alır")]
[SuppressMessage("Style", "SA1101:Prefix local calls with this", Justification = "Ekip stili gereği this prefix zorunlu değil")]
[SuppressMessage("Style", "SA1413:Use trailing comma in multi-line initializers", Justification = "Minör stil uyarısı, kademeli ele alınacak")]
[SuppressMessage("Style", "SA1512:Single-line comments should not be followed by blank line", Justification = "Kademeli temizlik planı")]
[SuppressMessage("Style", "SA1513:Closing brace should be followed by blank line", Justification = "Kademeli temizlik planı")]
[SuppressMessage("Style", "SA1515:Single-line comment should be preceded by blank line", Justification = "Kademeli temizlik planı")]
[SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Public members should come before private members", Justification = "Mantıksal grupla düzenlenmiş")]
[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Öncelik fonksiyonel düzeltmeler; performans optimizasyonu daha sonra")]
[SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "ASP.NET Core bağlamı; kademeli olarak eklenecek")]
public class AuthService : IAuthService
{
    private readonly IdentityDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IStructuredLogger _logger;
    private readonly IEmailService _emailService;

    public AuthService(
        IdentityDbContext context,
        IPasswordService passwordService,
        ITokenService tokenService,
        IMapper mapper,
        IStructuredLogger logger,
        IEmailService emailService)
    {
        _context = context;
        _passwordService = passwordService;
        _tokenService = tokenService;
        _mapper = mapper;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        if (request == null)
        {
            return ApiResponse<AuthResponse>.Fail(ErrorCodes.BAD_REQUEST, "Request cannot be null");
        }
        _logger.LogInformation("Login attempt for {EmailOrUsername}", request.EmailOrUsername);
        try
        {
            var isEmail = request.EmailOrUsername.Contains('@', StringComparison.Ordinal);

            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .FirstOrDefaultAsync(u => isEmail ? u.Email == request.EmailOrUsername : u.UserName == request.EmailOrUsername).ConfigureAwait(false);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for {EmailOrUsername}", request.EmailOrUsername);
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.INVALID_CREDENTIALS, "Invalid email or password");
            }

            _logger.LogInformation("User found: {UserId}, IsActive: {IsActive}, IsEmailConfirmed: {IsEmailConfirmed}", user.Id, user.IsActive, user.IsEmailConfirmed);

            if (!user.IsActive)
            {
                _logger.LogSecurityEvent("LoginFailed", user.Id, new { EmailOrUsername = request.EmailOrUsername, Reason = "UserInactive", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.ACCOUNT_LOCKED, "Account is deactivated");
            }

            if (user.IsLocked && user.LockoutEnd > DateTime.UtcNow)
            {
                _logger.LogSecurityEvent("LoginFailed", user.Id, new { EmailOrUsername = request.EmailOrUsername, Reason = "AccountLocked", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.ACCOUNT_LOCKED, "Account is temporarily locked");
            }

            if (!user.IsEmailConfirmed)
            {
                _logger.LogSecurityEvent("LoginFailed", user.Id, new { EmailOrUsername = request.EmailOrUsername, Reason = "EmailNotConfirmed", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.EMAIL_NOT_CONFIRMED, "Email address is not confirmed");
            }

            var isPasswordValid = _passwordService.VerifyPassword(request.Password, user.PasswordHash);
            _logger.LogInformation("Password verification for user {UserId}: {IsValid}", user.Id, isPasswordValid);

            if (!isPasswordValid)
            {
                await HandleFailedLoginAttempt(user);
                _logger.LogSecurityEvent("LoginFailed", user.Id, new { EmailOrUsername = request.EmailOrUsername, Reason = "InvalidPassword", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.INVALID_CREDENTIALS, "Invalid email or password");
            }

            // Reset failed login attempts on successful login
            await ResetFailedLoginAttempts(user);

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Create security user
            var securityUser = await CreateSecurityUser(user);

            // Generate tokens
            var tokenResult = await _tokenService.GenerateTokenAsync(securityUser, request.DeviceId, ipAddress);

            // Save refresh token
            await SaveRefreshToken(user.Id, tokenResult.RefreshToken, request.DeviceId, ipAddress);

            var userDto = _mapper.Map<UserDto>(user);
            var response = new AuthResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                User = userDto
            };

            _logger.LogUserAction(user.Id, "Login", new { EmailOrUsername = request.EmailOrUsername, IpAddress = ipAddress });

            return ApiResponse<AuthResponse>.Ok(response, "Login successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for emailOrUsername {EmailOrUsername}", request.EmailOrUsername);
            return ApiResponse<AuthResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during login");
        }
    }

    public async Task<ApiResponse<RegisterResponse>> RegisterAsync(RegisterRequest request, string? ipAddress = null)
    {
        try
        {
            if (request == null)
            {
                return ApiResponse<RegisterResponse>.Fail(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email || u.UserName == request.UserName)
                .ConfigureAwait(false);

            if (existingUser != null)
            {
                if (existingUser.Email == request.Email)
                    return ApiResponse<RegisterResponse>.Fail(ErrorCodes.User.EMAIL_ALREADY_EXISTS, "Email address is already registered");

                return ApiResponse<RegisterResponse>.Fail(ErrorCodes.User.USERNAME_ALREADY_EXISTS, "Username is already taken");
            }

            // Validate password strength
            if (!_passwordService.IsPasswordStrong(request.Password))
            {
                return ApiResponse<RegisterResponse>.Fail(ErrorCodes.User.INVALID_PASSWORD,
                    "Password must be at least 8 characters long and contain uppercase, lowercase, digit and special character");
            }

            // Create new user
            var user = new User
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = _passwordService.HashPassword(request.Password),
                PhoneNumber = request.PhoneNumber,
                EmailConfirmationToken = _passwordService.GeneratePasswordResetToken(),
                EmailConfirmationTokenExpires = DateTime.UtcNow.AddDays(1)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Assign default Student role
            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Student").ConfigureAwait(false);
            if (studentRole != null)
            {
                var userRole = new UserRole
                {
                    UserId = user.Id,
                    RoleId = studentRole.Id,
                    AssignedBy = "System"
                };
                _context.UserRoles.Add(userRole);
            }

            // Assign categories if provided
            if (request.Categories.Any())
            {
                var categories = await _context.Categories
                    .Where(c => request.Categories.Contains(c.Name))
                    .ToListAsync()
                    .ConfigureAwait(false);

                foreach (var category in categories)
                {
                    var userCategory = new UserCategory
                    {
                        UserId = user.Id,
                        CategoryId = category.Id,
                        AssignedBy = "System"
                    };
                    _context.UserCategories.Add(userCategory);
                }
            }

            _logger.LogInformation("About to save changes for user {Email}", request.Email);
            await _context.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("SaveChangesAsync completed for user {Email}", request.Email);

            _logger.LogInformation("User saved successfully. Now attempting email confirmation for {Email}", user.Email);

            // Send email confirmation (speedreading-app)
            var confirmationLink = $"http://localhost:4202/auth/confirm-email?token={Uri.EscapeDataString(user.EmailConfirmationToken ?? string.Empty)}&userId={user.Id}";
            var emailSubject = "EğitimPlatform | Hesabınızı Doğrulayın";
            var emailBody = $@"
<!doctype html>
<html lang='tr'>
  <head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Hesap Doğrulama</title>
  </head>
  <body style='margin:0;padding:0;background-color:#f5f7fb;'>
    <table role='presentation' cellpadding='0' cellspacing='0' width='100%'>
      <tr>
        <td align='center' style='padding:32px 12px;'>
          <table role='presentation' cellpadding='0' cellspacing='0' width='100%' style='max-width:560px;background:#ffffff;border-radius:12px;box-shadow:0 4px 20px rgba(0,0,0,0.06);overflow:hidden;'>
            <tr>
              <td style='padding:24px 24px 0 24px; text-align:center;'>
                <div style='display:inline-flex;align-items:center;gap:8px;'>
                  <span style='display:inline-block;width:36px;height:36px;border-radius:8px;background:#4f46e5;color:#fff;line-height:36px;font-weight:700;font-family:Arial,sans-serif;'>EP</span>
                  <span style='font-family:Arial,sans-serif;font-size:16px;color:#111827;font-weight:700'>EğitimPlatform</span>
                </div>
              </td>
            </tr>
            <tr>
              <td style='padding:16px 24px 0 24px;'>
                <h1 style='margin:0 0 8px 0;font-family:Arial,sans-serif;font-size:22px;color:#111827;'>Hoş geldiniz, {user.FirstName}!</h1>
                <p style='margin:0 0 16px 0;font-family:Arial,sans-serif;font-size:14px;color:#6b7280;'>Hesabınızı aktifleştirmek için aşağıdaki butona tıklayın. Bu işlem sadece birkaç saniye sürer.</p>
              </td>
            </tr>
            <tr>
              <td style='padding:8px 24px 24px 24px;' align='center'>
                <a href='{confirmationLink}' style='display:inline-block;background:#10b981;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:10px;font-family:Arial,sans-serif;font-weight:600;'>Hesabımı Doğrula</a>
                <p style='margin:12px 0 0 0;font-family:Arial,sans-serif;font-size:12px;color:#9ca3af'>Bağlantı 24 saat boyunca geçerlidir.</p>
              </td>
            </tr>
            <tr>
              <td style='padding:0 24px 24px 24px;'>
                <p style='margin:0;font-family:Arial,sans-serif;font-size:12px;color:#6b7280;'>Eğer bu işlemi siz yapmadıysanız, bu e-postayı yok sayabilirsiniz.</p>
              </td>
            </tr>
            <tr>
              <td style='background:#f9fafb;padding:16px 24px;text-align:center;'>
                <p style='margin:0;font-family:Arial,sans-serif;font-size:12px;color:#9ca3af;'>© {DateTime.UtcNow:yyyy} EğitimPlatform • Tüm hakları saklıdır.</p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

            try
            {
                _logger.LogInformation("About to call email service for {Email}", user.Email ?? string.Empty);
                _logger.LogInformation("Email service instance: {EmailServiceType}", _emailService?.GetType()?.Name ?? "NULL");
                _logger.LogInformation("Attempting to send confirmation email to {Email}", user.Email ?? string.Empty);
                var email = user.Email ?? string.Empty;
                var emailResult = await _emailService!.SendEmailAsync(email, emailSubject, emailBody, isHtml: true);

                if (emailResult.IsSuccess)
                {
                    _logger.LogUserAction(user.Id, "EmailConfirmationSent", new { Email = request.Email ?? string.Empty });
                    _logger.LogInformation("Successfully sent confirmation email to {Email}", user.Email ?? string.Empty);
                }
                else
                {
                    _logger.LogWarning("Failed to send confirmation email to {Email}: {ErrorMessage}", user.Email ?? string.Empty, emailResult.ErrorMessage ?? string.Empty);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Exception occurred while sending confirmation email to {Email}", user.Email);
                // Continue with success response
            }

            var response = new RegisterResponse
            {
                UserId = user.Id,
                Message = "Registration successful. Please check your email to confirm your account.",
                RequiresEmailConfirmation = true
            };

            _logger.LogUserAction(user.Id, "Register", new { Email = request.Email, IpAddress = ipAddress });

            return ApiResponse<RegisterResponse>.Ok(response, "User registered successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email {Email}", request.Email);
            return ApiResponse<RegisterResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during registration");
        }
    }

    public async Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, string? ipAddress = null)
    {
        try
        {
            if (request == null)
            {
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserCategories)
                    .ThenInclude(uc => uc.Category)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken).ConfigureAwait(false);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                _logger.LogSecurityEvent("RefreshTokenFailed", null, new { Token = request.RefreshToken, Reason = "InvalidToken", IpAddress = ipAddress });
                return ApiResponse<AuthResponse>.Fail(ErrorCodes.Authentication.INVALID_TOKEN, "Invalid refresh token");
            }

            // Revoke old refresh token
            refreshToken.IsRevoked = true;
            refreshToken.RevokedAt = DateTime.UtcNow;

            var user = refreshToken.User;

            // Create security user
            var securityUser = await CreateSecurityUser(user).ConfigureAwait(false);

            // Generate new tokens
            var tokenResult = await _tokenService.GenerateTokenAsync(securityUser, request.DeviceId, ipAddress).ConfigureAwait(false);

            // Save new refresh token
            await SaveRefreshToken(user.Id, tokenResult.RefreshToken, request.DeviceId, ipAddress).ConfigureAwait(false);

            await _context.SaveChangesAsync().ConfigureAwait(false);

            var userDto = _mapper.Map<UserDto>(user);
            var response = new AuthResponse
            {
                AccessToken = tokenResult.AccessToken,
                RefreshToken = tokenResult.RefreshToken,
                ExpiresAt = tokenResult.ExpiresAt,
                User = userDto
            };

            _logger.LogUserAction(user.Id, "RefreshToken", new { IpAddress = ipAddress });

            return ApiResponse<AuthResponse>.Ok(response, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return ApiResponse<AuthResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during token refresh");
        }
    }

    public async Task<ApiResponse<object>> LogoutAsync(string refreshToken)
    {
        try
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (token != null)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogUserAction(token.UserId, "Logout", new { RefreshToken = refreshToken });
            }

            return ApiResponse.Ok("Logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during logout");
        }
    }

    public async Task<ApiResponse<object>> LogoutAllAsync(string userId)
    {
        try
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogUserAction(userId, "LogoutAll", new { TokenCount = tokens.Count });

            return ApiResponse.Ok("All sessions logged out successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout all for user {UserId}", userId);
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during logout");
        }
    }

    public async Task<ApiResponse<PasswordResetResponse>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return ApiResponse<PasswordResetResponse>.Fail(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                // Don't reveal that user doesn't exist
                return ApiResponse<PasswordResetResponse>.Ok(new PasswordResetResponse
                {
                    Message = "If the email address exists, a password reset link has been sent.",
                    Success = true
                });
            }

            user.PasswordResetToken = _passwordService.GeneratePasswordResetToken();
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);

            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Send password reset email (speedreading-app)
            var resetLink = $"http://localhost:4202/reset-password?token={Uri.EscapeDataString(user.PasswordResetToken ?? string.Empty)}&email={Uri.EscapeDataString(user.Email)}";
            var emailSubject = "Şifre Sıfırlama Talebi - EğitimPlatform";
            var emailBody = $@"
<html>
<body>
    <h2>Şifre Sıfırlama Talebi</h2>
    <p>Merhaba {user.FirstName},</p>
    <p>Hesabınız için şifre sıfırlama talebi aldık. Şifrenizi sıfırlamak için aşağıdaki linke tıklayın:</p>
    <p><a href='{resetLink}' style='background-color: #007bff; color: white; padding: 10px 15px; text-decoration: none; border-radius: 5px;'>Şifremi Sıfırla</a></p>
    <p>Bu link 1 saat boyunca geçerlidir.</p>
    <p>Eğer bu talebi siz yapmadıysanız, bu e-postayı göz ardı edebilirsiniz.</p>
    <br>
    <p>Teşekkürler,<br>EğitimPlatform Ekibi</p>
</body>
</html>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody, isHtml: true).ConfigureAwait(false);
                _logger.LogUserAction(user.Id, "ForgotPasswordEmailSent", new { Email = request.Email });
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send password reset email to {Email}", user.Email);
                // Continue with success response to prevent user enumeration
            }

            _logger.LogUserAction(user.Id, "ForgotPassword", new { Email = request.Email });

            return ApiResponse<PasswordResetResponse>.Ok(new PasswordResetResponse
            {
                Message = "Password reset link has been sent to your email.",
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for email {Email}", request.Email);
            return ApiResponse<PasswordResetResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during password reset");
        }
    }

    public async Task<ApiResponse<PasswordResetResponse>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return ApiResponse<PasswordResetResponse>.Fail(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email &&
                                         u.PasswordResetToken == request.Token &&
                                         u.PasswordResetTokenExpires > DateTime.UtcNow);

            if (user == null)
            {
                return ApiResponse<PasswordResetResponse>.Fail(ErrorCodes.Authentication.INVALID_TOKEN, "Invalid or expired reset token");
            }

            if (!_passwordService.IsPasswordStrong(request.NewPassword))
            {
                return ApiResponse<PasswordResetResponse>.Fail(ErrorCodes.User.INVALID_PASSWORD,
                    "Password must be at least 8 characters long and contain uppercase, lowercase, digit and special character");
            }

            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.UtcNow;

            // Revoke all active refresh tokens (evaluate IsActive client-side)
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id)
                .ToListAsync();
            refreshTokens = refreshTokens.Where(rt => !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow).ToList();

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogUserAction(user.Id, "ResetPassword", new { Email = request.Email });

            return ApiResponse<PasswordResetResponse>.Ok(new PasswordResetResponse
            {
                Message = "Password has been reset successfully.",
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for email {Email}", request.Email);
            return ApiResponse<PasswordResetResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during password reset");
        }
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        try
        {
            if (request == null)
            {
                return ApiResponse.Fail(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var user = await _context.Users.FindAsync(userId).ConfigureAwait(false);

            if (user == null)
            {
                return ApiResponse.Fail(ErrorCodes.NOT_FOUND, "User not found");
            }

            if (!_passwordService.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                return ApiResponse.Fail(ErrorCodes.Authentication.INVALID_CREDENTIALS, "Current password is incorrect");
            }

            if (!_passwordService.IsPasswordStrong(request.NewPassword))
            {
                return ApiResponse.Fail(ErrorCodes.User.INVALID_PASSWORD,
                    "Password must be at least 8 characters long and contain uppercase, lowercase, digit and special character");
            }

            user.PasswordHash = _passwordService.HashPassword(request.NewPassword);
            user.SecurityStamp = Guid.NewGuid().ToString();
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogUserAction(userId, "ChangePassword");

            return ApiResponse.Ok("Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password change for user {UserId}", userId);
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during password change");
        }
    }

    public async Task<ApiResponse<object>> ChangeUserNameAsync(string userId, ChangeUserNameRequest request)
    {
        try
        {
            if (request == null)
            {
                return ApiResponse.Fail(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var user = await _context.Users.FindAsync(userId).ConfigureAwait(false);
            if (user == null)
            {
                return ApiResponse.Fail(ErrorCodes.NOT_FOUND, "User not found");
            }

            var exists = await _context.Users.AnyAsync(u => u.UserName == request.NewUserName && u.Id != userId).ConfigureAwait(false);
            if (exists)
            {
                return ApiResponse.Fail(ErrorCodes.CONFLICT, "Username already in use");
            }

            user.UserName = request.NewUserName;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogUserAction(userId, "ChangeUserName", new { NewUserName = request.NewUserName });
            return ApiResponse.Ok("Username changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing username for user {UserId}", userId);
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred while changing username");
        }
    }

    // ChangeEmailAsync removed per request

    public async Task<ApiResponse<EmailConfirmationResponse>> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        try
        {
            if (request == null)
            {
                return ApiResponse<EmailConfirmationResponse>.Fail(ErrorCodes.BAD_REQUEST, "Request cannot be null");
            }
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId &&
                                         u.EmailConfirmationToken == request.Token &&
                                         u.EmailConfirmationTokenExpires > DateTime.UtcNow).ConfigureAwait(false);

            if (user == null)
            {
                return ApiResponse<EmailConfirmationResponse>.Fail(ErrorCodes.Authentication.INVALID_TOKEN, "Invalid or expired confirmation token");
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;
            user.EmailConfirmationTokenExpires = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogUserAction(user.Id, "ConfirmEmail");

            return ApiResponse<EmailConfirmationResponse>.Ok(new EmailConfirmationResponse
            {
                Message = "Email confirmed successfully.",
                Success = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email confirmation for user {UserId}", request.UserId);
            return ApiResponse<EmailConfirmationResponse>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during email confirmation");
        }
    }

    public async Task<ApiResponse<object>> ResendEmailConfirmationAsync(string email)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null || user.IsEmailConfirmed)
            {
                // Don't reveal user existence or confirmation status
                return ApiResponse.Ok("If the email address exists and is not confirmed, a confirmation link has been sent.");
            }

            user.EmailConfirmationToken = _passwordService.GeneratePasswordResetToken();
            user.EmailConfirmationTokenExpires = DateTime.UtcNow.AddDays(1);

            await _context.SaveChangesAsync().ConfigureAwait(false);

            // Send email confirmation (speedreading-app)
            var confirmationLink = $"http://localhost:4202/auth/confirm-email?token={Uri.EscapeDataString(user.EmailConfirmationToken ?? string.Empty)}&userId={user.Id}";
            var emailSubject = "EğitimPlatform | Hesabınızı Doğrulayın";
            var emailBody = $@"
<!doctype html>
<html lang='tr'>
  <head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <title>Hesap Doğrulama</title>
  </head>
  <body style='margin:0;padding:0;background-color:#f5f7fb;'>
    <table role='presentation' cellpadding='0' cellspacing='0' width='100%'>
      <tr>
        <td align='center' style='padding:32px 12px;'>
          <table role='presentation' cellpadding='0' cellspacing='0' width='100%' style='max-width:560px;background:#ffffff;border-radius:12px;box-shadow:0 4px 20px rgba(0,0,0,0.06);overflow:hidden;'>
            <tr>
              <td style='padding:24px 24px 0 24px; text-align:center;'>
                <div style='display:inline-flex;align-items:center;gap:8px;'>
                  <span style='display:inline-block;width:36px;height:36px;border-radius:8px;background:#4f46e5;color:#fff;line-height:36px;font-weight:700;font-family:Arial,sans-serif;'>EP</span>
                  <span style='font-family:Arial,sans-serif;font-size:16px;color:#111827;font-weight:700'>EğitimPlatform</span>
                </div>
              </td>
            </tr>
            <tr>
              <td style='padding:16px 24px 0 24px;'>
                <h1 style='margin:0 0 8px 0;font-family:Arial,sans-serif;font-size:22px;color:#111827;'>Hesabınızı Doğrulayın</h1>
                <p style='margin:0 0 16px 0;font-family:Arial,sans-serif;font-size:14px;color:#6b7280;'>Merhaba {user.FirstName}, hesabınızı aktifleştirmek için aşağıdaki butona tıklayın. Bu işlem sadece birkaç saniye sürer.</p>
              </td>
            </tr>
            <tr>
              <td style='padding:8px 24px 24px 24px;' align='center'>
                <a href='{confirmationLink}' style='display:inline-block;background:#10b981;color:#ffffff;text-decoration:none;padding:12px 20px;border-radius:10px;font-family:Arial,sans-serif;font-weight:600;'>Hesabımı Doğrula</a>
                <p style='margin:12px 0 0 0;font-family:Arial,sans-serif;font-size:12px;color:#9ca3af'>Bağlantı 24 saat boyunca geçerlidir.</p>
              </td>
            </tr>
            <tr>
              <td style='padding:0 24px 24px 24px;'>
                <p style='margin:0;font-family:Arial,sans-serif;font-size:12px;color:#6b7280;'>Eğer bu işlemi siz yapmadıysanız, bu e-postayı yok sayabilirsiniz.</p>
              </td>
            </tr>
            <tr>
              <td style='background:#f9fafb;padding:16px 24px;text-align:center;'>
                <p style='margin:0;font-family:Arial,sans-serif;font-size:12px;color:#9ca3af;'>© {DateTime.UtcNow:yyyy} EğitimPlatform • Tüm hakları saklıdır.</p>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

            try
            {
                _logger.LogInformation("Attempting to resend confirmation email to {Email}", user.Email ?? string.Empty);
                var emailResult = await _emailService.SendEmailAsync(user.Email ?? string.Empty, emailSubject, emailBody, isHtml: true);

                if (emailResult.IsSuccess)
                {
                    _logger.LogUserAction(user.Id, "ResendEmailConfirmationSent", new { Email = email ?? string.Empty });
                    _logger.LogInformation("Successfully resent confirmation email to {Email}", user.Email ?? string.Empty);
                }
                else
                {
                    _logger.LogWarning("Failed to resend confirmation email to {Email}: {ErrorMessage}", user.Email ?? string.Empty, emailResult.ErrorMessage ?? string.Empty);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Exception occurred while resending confirmation email to {Email}", user.Email ?? string.Empty);
            }

            _logger.LogUserAction(user.Id, "ResendEmailConfirmation");

            return ApiResponse.Ok("Confirmation email has been sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during resend email confirmation for email {Email}", email);
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred during email confirmation resend");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .Include(u => u.UserCategories).ThenInclude(uc => uc.Category)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return ApiResponse<UserDto>.Fail(ErrorCodes.NOT_FOUND, "User not found");
            }

            var userDto = _mapper.Map<UserDto>(user);
            return ApiResponse<UserDto>.Ok(userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return ApiResponse<UserDto>.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred while getting user");
        }
    }

    public async Task<ApiResponse<object>> LockUserAsync(string userId, DateTime? lockoutEnd = null)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return ApiResponse.Fail(ErrorCodes.NOT_FOUND, "User not found");
            }

            user.IsLocked = true;
            user.LockoutEnd = lockoutEnd ?? DateTime.UtcNow.AddDays(30);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogUserAction(userId, "LockUser", new { LockoutEnd = user.LockoutEnd });

            return ApiResponse.Ok("User locked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user {UserId}", userId);
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred while locking user");
        }
    }

    public async Task<ApiResponse<object>> UnlockUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return ApiResponse.Fail(ErrorCodes.NOT_FOUND, "User not found");
            }

            user.IsLocked = false;
            user.LockoutEnd = null;
            user.AccessFailedCount = 0;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogUserAction(userId, "UnlockUser");

            return ApiResponse.Ok("User unlocked successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {UserId}", userId);
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred while unlocking user");
        }
    }

    public async Task<ApiResponse<object>> DeactivateUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return ApiResponse.Fail(ErrorCodes.NOT_FOUND, "User not found");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            // Revoke all refresh tokens
            var refreshTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsActive)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            _logger.LogUserAction(userId, "DeactivateUser");

            return ApiResponse.Ok("User deactivated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", userId);
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred while deactivating user");
        }
    }

    public async Task<ApiResponse<object>> ActivateUserAsync(string userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return ApiResponse.Fail(ErrorCodes.NOT_FOUND, "User not found");
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogUserAction(userId, "ActivateUser");

            return ApiResponse.Ok("User activated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", userId);
            return ApiResponse.Fail(ErrorCodes.INTERNAL_SERVER_ERROR, "An error occurred while activating user");
        }
    }

    private async Task HandleFailedLoginAttempt(User user)
    {
        user.AccessFailedCount++;

        if (user.AccessFailedCount >= 5)
        {
            user.IsLocked = true;
            user.LockoutEnd = DateTime.UtcNow.AddMinutes(30);
        }

        await _context.SaveChangesAsync().ConfigureAwait(false);
    }

    private async Task ResetFailedLoginAttempts(User user)
    {
        if (user.AccessFailedCount > 0)
        {
            user.AccessFailedCount = 0;
            user.IsLocked = false;
            user.LockoutEnd = null;
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private async Task<SecurityUser> CreateSecurityUser(User user)
    {
        var roles = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.Role.Name).ToList();
        var categories = user.UserCategories.Where(uc => uc.IsActive).Select(uc => uc.Category.Name).ToList();

        // Get permissions from roles
        var roleIds = user.UserRoles.Where(ur => ur.IsActive).Select(ur => ur.RoleId).ToList();
        var permissions = await _context.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.IsActive)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Name)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false);

        return new SecurityUser
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            Roles = roles,
            Categories = categories,
            Permissions = permissions,
            IsEmailConfirmed = user.IsEmailConfirmed,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            SecurityStamp = user.SecurityStamp
        };
    }

    private async Task SaveRefreshToken(string userId, string token, string? deviceId, string? ipAddress)
    {
        var refreshToken = new RefreshToken
        {
            Token = token,
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7), // 7 days expiry
            DeviceId = deviceId,
            IpAddress = ipAddress
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync().ConfigureAwait(false);
    }
}