using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class UserConsent
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = null!;

    public int Type { get; set; }

    public string Purpose { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsGranted { get; set; }

    public DateTime ConsentedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string LegalBasis { get; set; } = null!;

    public string? DataController { get; set; }

    public string? DataProcessor { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string Version { get; set; } = null!;

    public string? PreviousVersion { get; set; }

    public virtual User User { get; set; } = null!;
}
