using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using User.Core.Interfaces;

namespace User.API.Controllers;

/// <summary>
/// Email verification endpoints
/// </summary>
[ApiController]
[Route("api/v1/email-verification")]
[Produces("application/json")]
public class EmailVerificationController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly ILogger<EmailVerificationController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public EmailVerificationController(
        IUserProfileService userProfileService,
        ILogger<EmailVerificationController> logger)
    {
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Send email verification token to user's email
    /// </summary>
    /// <param name="request">Email verification request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification token sent confirmation</returns>
    [HttpPost("send")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public Task<ActionResult> SendVerificationEmail(
        [FromBody] SendVerificationEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Email))
        {
            return Task.FromResult<ActionResult>(BadRequest("Email is required"));
        }

        _logger.LogInformation("Email verification requested for: {Email}", request.Email);

        // In a real implementation, this would:
        // 1. Generate a secure verification token
        // 2. Store token with expiration in database/cache
        // 3. Send email with verification link
        // 4. Implement rate limiting to prevent abuse

        var verificationToken = Guid.NewGuid().ToString("N");
        
        var response = new
        {
            Message = "Verification email sent successfully",
            Email = request.Email,
            TokenSent = true,
            ExpiresIn = TimeSpan.FromHours(24).TotalMinutes,
            Instructions = "Please check your email and click the verification link to complete the process."
        };

        // TODO: Implement actual email sending logic
        // await _emailService.SendVerificationEmailAsync(request.Email, verificationToken, cancellationToken);

        return Task.FromResult<ActionResult>(Ok(response));
    }

    /// <summary>
    /// Verify email using token
    /// </summary>
    /// <param name="token">Verification token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    [HttpPost("verify")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult> VerifyEmail(
        [FromQuery] string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<ActionResult>(BadRequest("Verification token is required"));
        }

        _logger.LogInformation("Email verification attempted with token: {Token}", token.Substring(0, 8) + "...");

        // In a real implementation, this would:
        // 1. Look up the token in database/cache
        // 2. Check if token is valid and not expired
        // 3. Mark user's email as verified
        // 4. Clean up the used token

        var verificationResult = new
        {
            Success = true,
            Message = "Email verified successfully",
            VerifiedAt = DateTime.UtcNow,
            Token = token.Substring(0, 8) + "...",
            NextSteps = "You can now access all features of your account."
        };

        // TODO: Implement actual verification logic
        // var result = await _verificationService.VerifyEmailTokenAsync(token, cancellationToken);

        return Task.FromResult<ActionResult>(Ok(verificationResult));
    }

    /// <summary>
    /// Resend verification email (authenticated)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Resend confirmation</returns>
    [HttpPost("resend")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> ResendVerificationEmail(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        _logger.LogInformation("Email verification resend requested by user: {UserId}", userId);

        // Get user profile to extract email
        var profileResult = await _userProfileService.GetByUserIdAsync(userId, cancellationToken);
        if (!profileResult.IsSuccess || profileResult.Value == null)
        {
            return BadRequest("User profile not found");
        }

        // In a real implementation, check if email is already verified
        // if (profileResult.Value.EmailVerified)
        // {
        //     return BadRequest("Email is already verified");
        // }

        var verificationToken = Guid.NewGuid().ToString("N");
        
        var response = new
        {
            Message = "Verification email resent successfully",
            UserId = userId,
            TokenSent = true,
            ExpiresIn = TimeSpan.FromHours(24).TotalMinutes,
            Instructions = "Please check your email and click the verification link to complete the process.",
            Note = "If you don't receive the email, please check your spam folder."
        };

        // TODO: Implement actual email resending logic
        // await _emailService.SendVerificationEmailAsync(profileResult.Value.Email, verificationToken, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Check email verification status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification status</returns>
    [HttpGet("status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetVerificationStatus(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var profileResult = await _userProfileService.GetByUserIdAsync(userId, cancellationToken);
        if (!profileResult.IsSuccess || profileResult.Value == null)
        {
            return BadRequest("User profile not found");
        }

        var status = new
        {
            UserId = userId,
            EmailVerified = false, // TODO: Get from actual profile field
            VerifiedAt = (DateTime?)null, // TODO: Get from actual profile field
            PendingVerification = true,
            Message = "Email verification is pending. Please check your email for the verification link.",
            Actions = new[]
            {
                "Resend verification email if needed",
                "Check spam folder for verification email",
                "Contact support if you continue to have issues"
            }
        };

        return Ok(status);
    }

    /// <summary>
    /// Update email address (requires re-verification)
    /// </summary>
    /// <param name="request">Email update request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Email update result</returns>
    [HttpPost("update-email")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult> UpdateEmail(
        [FromBody] UpdateEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<ActionResult>(Unauthorized("User ID not found in token"));
        }

        if (request == null || string.IsNullOrWhiteSpace(request.NewEmail))
        {
            return Task.FromResult<ActionResult>(BadRequest("New email is required"));
        }

        if (!IsValidEmail(request.NewEmail))
        {
            return Task.FromResult<ActionResult>(BadRequest("Invalid email format"));
        }

        _logger.LogInformation("Email update requested by user: {UserId}, NewEmail: {NewEmail}", userId, request.NewEmail);

        // In a real implementation, this would:
        // 1. Send verification email to new address
        // 2. Keep old email active until new one is verified
        // 3. Handle email conflicts/duplicates
        // 4. Update profile after verification

        var verificationToken = Guid.NewGuid().ToString("N");
        
        var response = new
        {
            Message = "Email update initiated. Please verify your new email address.",
            UserId = userId,
            NewEmail = request.NewEmail,
            CurrentStatus = "Pending verification",
            VerificationSent = true,
            Instructions = "A verification email has been sent to your new email address. Click the link to complete the email update.",
            Note = "Your current email will remain active until the new email is verified."
        };

        // TODO: Send verification email to new address
        // await _emailService.SendEmailUpdateVerificationAsync(request.NewEmail, verificationToken, cancellationToken);

        return Task.FromResult<ActionResult>(Ok(response));
    }

    /// <summary>
    /// Get current user ID from JWT token
    /// </summary>
    /// <returns>User ID or null</returns>
    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
               User.FindFirst("sub")?.Value ??
               User.FindFirst("userId")?.Value;
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    /// <param name="email">Email to validate</param>
    /// <returns>True if valid</returns>
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Send verification email request
/// </summary>
public class SendVerificationEmailRequest
{
    /// <summary>
    /// Email address to verify
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Optional callback URL
    /// </summary>
    public string? CallbackUrl { get; set; }
}

/// <summary>
/// Update email request
/// </summary>
public class UpdateEmailRequest
{
    /// <summary>
    /// New email address
    /// </summary>
    public string NewEmail { get; set; } = string.Empty;

    /// <summary>
    /// Optional callback URL after verification
    /// </summary>
    public string? CallbackUrl { get; set; }
}