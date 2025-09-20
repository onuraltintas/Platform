using Enterprise.Shared.Events.Models;

namespace User.Core.Events;

/// <summary>
/// Event consumed from Identity Service when user is registered
/// </summary>
public record UserRegisteredFromIdentityEvent : IntegrationEvent
{
    /// <summary>
    /// User ID from Identity Service
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Username
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// First name if provided during registration
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name if provided during registration
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Phone number if provided
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Registration method (Email, Google, Microsoft, etc.)
    /// </summary>
    public string RegistrationMethod { get; set; } = string.Empty;

    /// <summary>
    /// Is email confirmed
    /// </summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>
    /// Registration IP address
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent string
    /// </summary>
    public string? UserAgent { get; set; }
}