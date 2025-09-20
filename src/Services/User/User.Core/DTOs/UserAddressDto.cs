namespace User.Core.DTOs;

/// <summary>
/// User address data transfer object
/// </summary>
public class UserAddressDto
{
    /// <summary>
    /// Address ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Address type
    /// </summary>
    public string AddressType { get; set; } = string.Empty;

    /// <summary>
    /// Address line 1
    /// </summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>
    /// Address line 2
    /// </summary>
    public string? AddressLine2 { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Postal code
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country
    /// </summary>
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Is primary address
    /// </summary>
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}