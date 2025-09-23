using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class EmailVerification
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? RequestIpAddress { get; set; }

    public string? VerificationIpAddress { get; set; }

    public int AttemptCount { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public string VerificationType { get; set; } = null!;

    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual UserProfile User { get; set; } = null!;
}
