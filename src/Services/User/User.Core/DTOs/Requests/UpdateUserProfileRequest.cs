using Enterprise.Shared.Common.Enums;

namespace User.Core.DTOs.Requests;

/// <summary>
/// Request to update user profile
/// </summary>
public class UpdateUserProfileRequest
{
    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Date of birth
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Bio/about information
    /// </summary>
    public string? Bio { get; set; }

    /// <summary>
    /// User's timezone
    /// </summary>
    public Enterprise.Shared.Common.Enums.TimeZone? TimeZone { get; set; }

    /// <summary>
    /// User's preferred language
    /// </summary>
    public LanguageCode? Language { get; set; }
}