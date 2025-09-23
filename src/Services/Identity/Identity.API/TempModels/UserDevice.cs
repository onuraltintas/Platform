using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class UserDevice
{
    public Guid Id { get; set; }

    public string DeviceId { get; set; } = null!;

    public string DeviceName { get; set; } = null!;

    public string DeviceType { get; set; } = null!;

    public string? OperatingSystem { get; set; }

    public string? Browser { get; set; }

    public DateTime FirstSeenAt { get; set; }

    public DateTime LastSeenAt { get; set; }

    public string? LastIpAddress { get; set; }

    public bool IsTrusted { get; set; }

    public bool IsActive { get; set; }

    public string UserId { get; set; } = null!;

    public string? PushToken { get; set; }

    public DateTime? PushTokenUpdatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
