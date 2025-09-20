using Enterprise.Shared.Events.Models;

namespace User.Core.Events;

/// <summary>
/// Event published when user profile is updated
/// </summary>
public record UserProfileUpdatedEvent : DomainEvent
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Updated first name
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Updated last name
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Updated phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Updated profile picture URL
    /// </summary>
    public string? ProfilePictureUrl { get; set; }

    /// <summary>
    /// Fields that were updated
    /// </summary>
    public List<string> UpdatedFields { get; set; } = new();

    /// <summary>
    /// Who updated the profile
    /// </summary>
    public string UpdatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Update source (Web, Mobile, Admin, etc.)
    /// </summary>
    public string Source { get; set; } = string.Empty;
}