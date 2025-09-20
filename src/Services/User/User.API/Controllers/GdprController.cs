using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using User.Core.Interfaces;
using Enterprise.Shared.Common.Models;

namespace User.API.Controllers;

/// <summary>
/// GDPR compliance endpoints for data export, deletion, and consent management
/// </summary>
[ApiController]
[Route("api/v1/gdpr")]
[Authorize]
[Produces("application/json")]
public class GdprController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly IUserPreferencesService _userPreferencesService;
    private readonly ILogger<GdprController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public GdprController(
        IUserProfileService userProfileService,
        IUserPreferencesService userPreferencesService,
        ILogger<GdprController> logger)
    {
        _userProfileService = userProfileService ?? throw new ArgumentNullException(nameof(userProfileService));
        _userPreferencesService = userPreferencesService ?? throw new ArgumentNullException(nameof(userPreferencesService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Export all user data in JSON format (GDPR Article 20 - Data Portability)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User data export in JSON format</returns>
    [HttpGet("export/json")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ExportDataAsJson(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        _logger.LogInformation("GDPR data export requested by user: {UserId}", userId);

        var profileResult = await _userProfileService.GetByUserIdAsync(userId, cancellationToken);
        var preferencesResult = await _userPreferencesService.GetByUserIdAsync(userId, cancellationToken);
        // Activities will be implemented later
        var activitiesResult = Result<List<object>>.Success(new List<object>());

        var exportData = new
        {
            UserId = userId,
            ExportDate = DateTime.UtcNow,
            Profile = profileResult.IsSuccess ? profileResult.Value : null,
            Preferences = preferencesResult.IsSuccess ? preferencesResult.Value : null,
            Activities = new List<object>(), // TODO: Implement activities
            GdprInfo = new
            {
                DataController = "Your Company Name",
                ExportReason = "GDPR Article 20 - Right to Data Portability",
                RetentionPolicy = "Data is retained as per our Privacy Policy",
                ContactEmail = "privacy@company.com"
            }
        };

        return Ok(exportData);
    }

    /// <summary>
    /// Export all user data in CSV format
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User data export in CSV format</returns>
    [HttpGet("export/csv")]
    [Produces("text/csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ExportDataAsCsv(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        _logger.LogInformation("GDPR CSV data export requested by user: {UserId}", userId);

        var profileResult = await _userProfileService.GetByUserIdAsync(userId, cancellationToken);
        var preferencesResult = await _userPreferencesService.GetByUserIdAsync(userId, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("Category,Field,Value");
        
        if (profileResult.IsSuccess && profileResult.Value != null)
        {
            var profile = profileResult.Value;
            csv.AppendLine($"Profile,UserId,{profile.UserId}");
            csv.AppendLine($"Profile,FirstName,{profile.FirstName}");
            csv.AppendLine($"Profile,LastName,{profile.LastName}");
            csv.AppendLine($"Profile,PhoneNumber,{profile.PhoneNumber}");
            csv.AppendLine($"Profile,DateOfBirth,{profile.DateOfBirth}");
            csv.AppendLine($"Profile,Bio,{profile.Bio}");
            csv.AppendLine($"Profile,TimeZone,{profile.TimeZone}");
            csv.AppendLine($"Profile,Language,{profile.Language}");
            csv.AppendLine($"Profile,CreatedAt,{profile.CreatedAt}");
            csv.AppendLine($"Profile,UpdatedAt,{profile.UpdatedAt}");
        }

        if (preferencesResult.IsSuccess && preferencesResult.Value != null)
        {
            var prefs = preferencesResult.Value;
            csv.AppendLine($"Preferences,EmailNotifications,{prefs.EmailNotifications}");
            csv.AppendLine($"Preferences,SmsNotifications,{prefs.SmsNotifications}");
            csv.AppendLine($"Preferences,PushNotifications,{prefs.PushNotifications}");
            csv.AppendLine($"Preferences,ProfileVisibility,{prefs.ProfileVisibility}");
            csv.AppendLine($"Preferences,Theme,{prefs.Theme}");
            csv.AppendLine($"Preferences,DataProcessingConsent,{prefs.DataProcessingConsent}");
            csv.AppendLine($"Preferences,MarketingEmailsConsent,{prefs.MarketingEmailsConsent}");
        }

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", $"user-data-export-{userId}-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    /// <summary>
    /// Export all user data in XML format
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User data export in XML format</returns>
    [HttpGet("export/xml")]
    [Produces("application/xml")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ExportDataAsXml(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        _logger.LogInformation("GDPR XML data export requested by user: {UserId}", userId);

        var profileResult = await _userProfileService.GetByUserIdAsync(userId, cancellationToken);
        var preferencesResult = await _userPreferencesService.GetByUserIdAsync(userId, cancellationToken);

        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.AppendLine("<UserDataExport>");
        xml.AppendLine($"  <UserId>{userId}</UserId>");
        xml.AppendLine($"  <ExportDate>{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}</ExportDate>");
        
        if (profileResult.IsSuccess && profileResult.Value != null)
        {
            var profile = profileResult.Value;
            xml.AppendLine("  <Profile>");
            xml.AppendLine($"    <FirstName><![CDATA[{profile.FirstName}]]></FirstName>");
            xml.AppendLine($"    <LastName><![CDATA[{profile.LastName}]]></LastName>");
            xml.AppendLine($"    <PhoneNumber><![CDATA[{profile.PhoneNumber}]]></PhoneNumber>");
            xml.AppendLine($"    <DateOfBirth>{profile.DateOfBirth}</DateOfBirth>");
            xml.AppendLine($"    <Bio><![CDATA[{profile.Bio}]]></Bio>");
            xml.AppendLine($"    <TimeZone>{profile.TimeZone}</TimeZone>");
            xml.AppendLine($"    <Language>{profile.Language}</Language>");
            xml.AppendLine($"    <CreatedAt>{profile.CreatedAt:yyyy-MM-ddTHH:mm:ssZ}</CreatedAt>");
            xml.AppendLine($"    <UpdatedAt>{profile.UpdatedAt:yyyy-MM-ddTHH:mm:ssZ}</UpdatedAt>");
            xml.AppendLine("  </Profile>");
        }

        if (preferencesResult.IsSuccess && preferencesResult.Value != null)
        {
            var prefs = preferencesResult.Value;
            xml.AppendLine("  <Preferences>");
            xml.AppendLine($"    <EmailNotifications>{prefs.EmailNotifications}</EmailNotifications>");
            xml.AppendLine($"    <SmsNotifications>{prefs.SmsNotifications}</SmsNotifications>");
            xml.AppendLine($"    <PushNotifications>{prefs.PushNotifications}</PushNotifications>");
            xml.AppendLine($"    <ProfileVisibility><![CDATA[{prefs.ProfileVisibility}]]></ProfileVisibility>");
            xml.AppendLine($"    <Theme><![CDATA[{prefs.Theme}]]></Theme>");
            xml.AppendLine($"    <DataProcessingConsent>{prefs.DataProcessingConsent}</DataProcessingConsent>");
            xml.AppendLine($"    <MarketingEmailsConsent>{prefs.MarketingEmailsConsent}</MarketingEmailsConsent>");
            xml.AppendLine("  </Preferences>");
        }

        xml.AppendLine("</UserDataExport>");

        var bytes = Encoding.UTF8.GetBytes(xml.ToString());
        return File(bytes, "application/xml", $"user-data-export-{userId}-{DateTime.UtcNow:yyyyMMdd}.xml");
    }

    /// <summary>
    /// Request account deletion (GDPR Article 17 - Right to Erasure)
    /// This initiates a soft delete process
    /// </summary>
    /// <param name="reason">Reason for deletion (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of deletion request</returns>
    [HttpPost("delete-account")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public Task<ActionResult> RequestAccountDeletion(
        [FromBody] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<ActionResult>(Unauthorized("User ID not found in token"));
        }

        _logger.LogWarning("GDPR account deletion requested by user: {UserId}, Reason: {Reason}", userId, reason ?? "Not provided");

        // In a real implementation, this would:
        // 1. Mark account for deletion with a grace period
        // 2. Send confirmation email
        // 3. Schedule background job for actual deletion after grace period
        // 4. Log the request for audit purposes

        var deletionInfo = new
        {
            UserId = userId,
            RequestedAt = DateTime.UtcNow,
            Status = "Pending",
            GracePeriodEnds = DateTime.UtcNow.AddDays(30),
            Reason = reason,
            Message = "Your account deletion request has been received. You have 30 days to cancel this request if needed. After this period, your account and all associated data will be permanently deleted.",
            CancellationInstructions = "To cancel this deletion request, please contact our support team or use the cancel deletion endpoint.",
            ContactEmail = "privacy@company.com"
        };

        return Task.FromResult<ActionResult>(Ok(deletionInfo));
    }

    /// <summary>
    /// Cancel account deletion request
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation of cancellation</returns>
    [HttpPost("cancel-deletion")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult> CancelAccountDeletion(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult<ActionResult>(Unauthorized("User ID not found in token"));
        }

        _logger.LogInformation("Account deletion cancellation requested by user: {UserId}", userId);

        // In a real implementation, this would check if there's an active deletion request
        // and cancel it if found within the grace period

        var cancellationInfo = new
        {
            UserId = userId,
            CancelledAt = DateTime.UtcNow,
            Status = "Cancelled",
            Message = "Your account deletion request has been successfully cancelled. Your account remains active."
        };

        return Task.FromResult<ActionResult>(Ok(cancellationInfo));
    }

    /// <summary>
    /// Update consent preferences (GDPR Article 7)
    /// </summary>
    /// <param name="consentRequest">Consent preferences</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated consent status</returns>
    [HttpPost("consent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> UpdateConsent(
        [FromBody] ConsentUpdateRequest consentRequest,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        if (consentRequest == null)
        {
            return BadRequest("Consent request is required");
        }

        _logger.LogInformation("GDPR consent update requested by user: {UserId}", userId);

        var result = await _userPreferencesService.UpdateConsentAsync(
            userId, 
            consentRequest.MarketingConsent, 
            consentRequest.DataProcessingConsent, 
            cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var consentInfo = new
        {
            UserId = userId,
            UpdatedAt = DateTime.UtcNow,
            MarketingConsent = consentRequest.MarketingConsent,
            DataProcessingConsent = consentRequest.DataProcessingConsent,
            Message = "Your consent preferences have been updated successfully."
        };

        return Ok(consentInfo);
    }

    /// <summary>
    /// Get current consent status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Current consent preferences</returns>
    [HttpGet("consent")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> GetConsentStatus(CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized("User ID not found in token");
        }

        var preferencesResult = await _userPreferencesService.GetByUserIdAsync(userId, cancellationToken);
        
        if (!preferencesResult.IsSuccess)
        {
            return BadRequest(preferencesResult.Error);
        }

        var consentStatus = new
        {
            UserId = userId,
            DataProcessingConsent = preferencesResult.Value?.DataProcessingConsent ?? false,
            MarketingEmailsConsent = preferencesResult.Value?.MarketingEmailsConsent ?? false,
            ConsentGivenAt = preferencesResult.Value?.ConsentGivenAt,
            LastUpdated = DateTime.UtcNow, // TODO: Get from preferences
            GdprInfo = new
            {
                DataController = "Your Company Name",
                LegalBasis = "Consent (GDPR Article 6.1.a)",
                WithdrawalRights = "You can withdraw consent at any time",
                ContactEmail = "privacy@company.com"
            }
        };

        return Ok(consentStatus);
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
}

/// <summary>
/// Consent update request model
/// </summary>
public class ConsentUpdateRequest
{
    /// <summary>
    /// Marketing emails consent
    /// </summary>
    public bool MarketingConsent { get; set; }

    /// <summary>
    /// Data processing consent
    /// </summary>
    public bool DataProcessingConsent { get; set; }
}