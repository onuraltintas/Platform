namespace Enterprise.Shared.Auditing.Models;

/// <summary>
/// Represents security context information for an audit event
/// </summary>
public class SecurityContext
{
    /// <summary>
    /// The authentication scheme used
    /// </summary>
    [StringLength(50)]
    public string? AuthenticationScheme { get; set; }

    /// <summary>
    /// List of user claims
    /// </summary>
    public List<AuditClaim> Claims { get; set; } = new();

    /// <summary>
    /// List of user roles
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// List of permissions
    /// </summary>
    public List<string> Permissions { get; set; } = new();

    /// <summary>
    /// The client application ID
    /// </summary>
    [StringLength(100)]
    public string? ClientId { get; set; }

    /// <summary>
    /// The scope of the operation
    /// </summary>
    [StringLength(200)]
    public string? Scope { get; set; }

    /// <summary>
    /// JWT token ID (JTI claim)
    /// </summary>
    [StringLength(100)]
    public string? TokenId { get; set; }

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime? TokenExpiration { get; set; }

    /// <summary>
    /// Whether multi-factor authentication was used
    /// </summary>
    public bool MfaUsed { get; set; }

    /// <summary>
    /// MFA method used (if applicable)
    /// </summary>
    [StringLength(50)]
    public string? MfaMethod { get; set; }

    /// <summary>
    /// Device trust level
    /// </summary>
    public DeviceTrustLevel DeviceTrustLevel { get; set; } = DeviceTrustLevel.Unknown;

    /// <summary>
    /// Whether the session is remembered
    /// </summary>
    public bool IsRemembered { get; set; }

    /// <summary>
    /// Session creation time
    /// </summary>
    public DateTime? SessionCreatedAt { get; set; }

    /// <summary>
    /// Last activity time
    /// </summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>
    /// Creates security context from ClaimsPrincipal
    /// </summary>
    public static SecurityContext FromClaimsPrincipal(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return new SecurityContext();
        }

        var context = new SecurityContext
        {
            AuthenticationScheme = principal.Identity.AuthenticationType,
            Claims = principal.Claims.Select(c => new AuditClaim 
            { 
                Type = c.Type, 
                Value = c.Value,
                Issuer = c.Issuer
            }).ToList()
        };

        // Extract common claims
        context.Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        context.ClientId = principal.FindFirst("client_id")?.Value;
        context.TokenId = principal.FindFirst("jti")?.Value;
        
        if (DateTime.TryParse(principal.FindFirst("exp")?.Value, out var exp))
        {
            context.TokenExpiration = DateTimeOffset.FromUnixTimeSeconds(long.Parse(principal.FindFirst("exp")!.Value)).DateTime;
        }

        context.MfaUsed = principal.FindFirst("amr")?.Value?.Contains("mfa") == true;
        context.MfaMethod = principal.FindFirst("mfa_method")?.Value;

        return context;
    }

    /// <summary>
    /// Adds a role to the context
    /// </summary>
    public SecurityContext WithRole(string role)
    {
        if (!string.IsNullOrEmpty(role) && !Roles.Contains(role))
        {
            Roles.Add(role);
        }
        return this;
    }

    /// <summary>
    /// Adds a permission to the context
    /// </summary>
    public SecurityContext WithPermission(string permission)
    {
        if (!string.IsNullOrEmpty(permission) && !Permissions.Contains(permission))
        {
            Permissions.Add(permission);
        }
        return this;
    }

    /// <summary>
    /// Sets MFA information
    /// </summary>
    public SecurityContext WithMfa(bool mfaUsed, string? mfaMethod = null)
    {
        MfaUsed = mfaUsed;
        MfaMethod = mfaMethod;
        return this;
    }

    /// <summary>
    /// Sets device trust information
    /// </summary>
    public SecurityContext WithDeviceTrust(DeviceTrustLevel trustLevel)
    {
        DeviceTrustLevel = trustLevel;
        return this;
    }
}

/// <summary>
/// Represents a claim in the audit context
/// </summary>
public class AuditClaim
{
    /// <summary>
    /// The claim type
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The claim value
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The claim issuer
    /// </summary>
    [StringLength(200)]
    public string? Issuer { get; set; }
}

/// <summary>
/// Device trust levels
/// </summary>
public enum DeviceTrustLevel
{
    /// <summary>
    /// Trust level is unknown
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Device is not trusted
    /// </summary>
    Untrusted = 1,

    /// <summary>
    /// Device has basic trust
    /// </summary>
    Basic = 2,

    /// <summary>
    /// Device is trusted
    /// </summary>
    Trusted = 3,

    /// <summary>
    /// Device is fully trusted (managed device)
    /// </summary>
    FullyTrusted = 4
}