using Enterprise.Shared.Events.Models;

namespace User.Core.Events;

/// <summary>
/// Event published when user preferences are changed
/// </summary>
public record UserPreferencesChangedEvent : DomainEvent
{
    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Changed preferences as key-value pairs
    /// </summary>
    public Dictionary<string, object> ChangedPreferences { get; set; } = new();

    /// <summary>
    /// Previous values for audit
    /// </summary>
    public Dictionary<string, object> PreviousValues { get; set; } = new();

    /// <summary>
    /// Who made the changes
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Change source (Web, Mobile, Admin, etc.)
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Whether GDPR consent was affected
    /// </summary>
    public bool GdprConsentChanged { get; set; }
}