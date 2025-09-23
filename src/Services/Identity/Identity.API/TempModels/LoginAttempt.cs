using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class LoginAttempt
{
    public Guid Id { get; set; }

    public string? UserId { get; set; }

    public string Email { get; set; } = null!;

    public DateTime AttemptedAt { get; set; }

    public bool IsSuccessful { get; set; }

    public string? FailureReason { get; set; }

    public string IpAddress { get; set; } = null!;

    public string? UserAgent { get; set; }

    public string? DeviceId { get; set; }

    public string? Location { get; set; }

    public int Type { get; set; }

    public Guid? GroupId { get; set; }

    public virtual Group? Group { get; set; }

    public virtual User? User { get; set; }
}
