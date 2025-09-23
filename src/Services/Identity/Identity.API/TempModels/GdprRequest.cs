using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class GdprRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = null!;

    public string RequestType { get; set; } = null!;

    public int Status { get; set; }

    public string? Reason { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ProcessedBy { get; set; }

    public string? ResultData { get; set; }

    public string VerificationToken { get; set; } = null!;

    public DateTime TokenExpiresAt { get; set; }

    public string? ProcessorNotes { get; set; }

    public bool IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public int Priority { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public virtual UserProfile User { get; set; } = null!;
}
