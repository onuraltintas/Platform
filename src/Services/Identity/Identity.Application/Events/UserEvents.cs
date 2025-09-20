using Enterprise.Shared.Events.Models;

namespace Identity.Application.Events;

/// <summary>
/// Event published when a user successfully logs in
/// </summary>
public record UserLoggedInEvent : IntegrationEvent
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string? DeviceId { get; init; }
    public Guid? GroupId { get; init; }
    public DateTime LoginAt { get; init; }
    public string Method { get; init; } = "Standard"; // Standard, Google, etc.
}

/// <summary>
/// Event published when a new user registers
/// </summary>
public record UserRegisteredEvent : IntegrationEvent
{
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string RegistrationMethod { get; init; } = "Standard";
    public DateTime RegisteredAt { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceId { get; init; }
}

/// <summary>
/// Event published when a user logs out
/// </summary>
public record UserLoggedOutEvent : IntegrationEvent
{
    public string UserId { get; init; } = string.Empty;
    public string? DeviceId { get; init; }
    public DateTime LogoutAt { get; init; }
    public string Reason { get; init; } = "User requested"; // User requested, Token expired, Security, etc.
}

/// <summary>
/// Event published when a new service is registered
/// </summary>
public record ServiceRegisteredEvent : IntegrationEvent
{
    public Guid ServiceId { get; init; }
    public string ServiceName { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string ServiceType { get; init; } = string.Empty;
    public DateTime RegisteredAt { get; init; }
    public string RegisteredBy { get; init; } = string.Empty;
}