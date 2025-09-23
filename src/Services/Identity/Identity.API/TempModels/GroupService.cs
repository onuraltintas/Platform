using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class GroupService
{
    public Guid GroupId { get; set; }

    public Guid ServiceId { get; set; }

    public DateTime GrantedAt { get; set; }

    public string? GrantedBy { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public int? MaxRequests { get; set; }

    public int? MaxUsers { get; set; }

    public decimal? MaxStorage { get; set; }

    public bool IsActive { get; set; }

    public string? Notes { get; set; }

    public virtual Group Group { get; set; } = null!;

    public virtual Service Service { get; set; } = null!;
}
