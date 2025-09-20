using Enterprise.Shared.Common.Entities;

namespace User.Core.Entities;

/// <summary>
/// User address information
/// </summary>
public class UserAddress : BaseEntity
{
    /// <summary>
    /// User ID reference
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property to user profile
    /// </summary>
    public virtual UserProfile UserProfile { get; set; } = null!;

    /// <summary>
    /// Address type (Home, Work, Billing, etc.)
    /// </summary>
    public string AddressType { get; set; } = "Home";

    /// <summary>
    /// Street address line 1
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Street address line 2
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal or ZIP code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Is this the default/primary address
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Additional notes about the address
    /// </summary>
    public string? Notes { get; set; }
}