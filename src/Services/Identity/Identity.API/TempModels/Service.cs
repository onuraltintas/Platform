using System;
using System.Collections.Generic;

namespace Identity.API.TempModels;

public partial class Service
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public string Endpoint { get; set; } = null!;

    public string Version { get; set; } = null!;

    public int Type { get; set; }

    public string? HealthCheckEndpoint { get; set; }

    public DateTime? LastHealthCheckAt { get; set; }

    public int Status { get; set; }

    public string? StatusMessage { get; set; }

    public string? ApiKey { get; set; }

    public bool RequiresAuthentication { get; set; }

    public string Metadata { get; set; } = null!;

    public DateTime RegisteredAt { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public string? RegisteredBy { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<GroupService> GroupServices { get; set; } = new List<GroupService>();

    public virtual ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}
