using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class UserActivity
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string ActivityType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? DeviceInfo { get; set; }

    public string? Location { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string? Metadata { get; set; }

    public string? SessionId { get; set; }

    public int RiskScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual UserProfile User { get; set; } = null!;
}
