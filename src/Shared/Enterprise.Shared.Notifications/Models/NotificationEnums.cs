namespace Enterprise.Shared.Notifications.Models;

/// <summary>
/// Notification types
/// </summary>
public enum NotificationType
{
    Welcome,
    EmailVerification,
    PasswordReset,
    OrderConfirmation,
    PaymentSuccess,
    PaymentFailed,
    AccountUpdate,
    SecurityAlert,
    SystemMaintenance,
    Reminder,
    Custom
}

/// <summary>
/// Notification channels
/// </summary>
public enum NotificationChannel
{
    Email,
    SMS,
    Push,
    InApp,
    Webhook
}

/// <summary>
/// Notification priorities
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// Notification statuses
/// </summary>
public enum NotificationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Cancelled,
    Expired
}

/// <summary>
/// Notification delivery statuses
/// </summary>
public enum DeliveryStatus
{
    Unknown,
    Pending,
    Sent,
    Delivered,
    Bounced,
    Failed,
    Rejected
}