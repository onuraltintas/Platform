using Enterprise.Shared.Common.Enums;

namespace User.Core.DTOs;

/// <summary>
/// User profile data transfer object
/// </summary>
public class UserProfileDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

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
    /// Profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// User's timezone
    /// </summary>
    public Enterprise.Shared.Common.Enums.TimeZone TimeZone { get; set; }

    /// <summary>
    /// User's preferred language
    /// </summary>
    public LanguageCode Language { get; set; }

    /// <summary>
    /// User preferences
    /// </summary>
    public UserPreferencesDto? Preferences { get; set; }

    /// <summary>
    /// User addresses
    /// </summary>
    public IEnumerable<UserAddressDto> Addresses { get; set; } = new List<UserAddressDto>();

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}