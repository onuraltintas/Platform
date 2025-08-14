using System.ComponentModel.DataAnnotations;

namespace EgitimPlatform.Services.IdentityService.Models.Entities;

public class User
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public bool IsEmailConfirmed { get; set; } = false;
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } = false;
    public DateTime? LockoutEnd { get; set; }
    public int AccessFailedCount { get; set; } = 0;

    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public string? EmailConfirmationToken { get; set; }
    public DateTime? EmailConfirmationTokenExpires { get; set; }

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }

    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserCategory> UserCategories { get; set; } = new List<UserCategory>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public string FullName => $"{FirstName} {LastName}";
}