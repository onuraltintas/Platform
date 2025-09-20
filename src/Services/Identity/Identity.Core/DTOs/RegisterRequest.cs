using System.ComponentModel.DataAnnotations;

namespace Identity.Core.DTOs;

public class RegisterRequest
{
    [Required(ErrorMessage = "E-posta adresi gereklidir")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanıcı adı gereklidir")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter arasında olmalıdır")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrarı gereklidir")]
    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad gereklidir")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad gereklidir")]
    [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    public string? PhoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    // Group assignment
    public Guid? GroupId { get; set; }
    public string? GroupInvitationCode { get; set; }

    // GDPR Consents
    [Required(ErrorMessage = "Kullanım şartlarını kabul etmelisiniz")]
    public bool AcceptTerms { get; set; }

    [Required(ErrorMessage = "Gizlilik politikasını kabul etmelisiniz")]
    public bool AcceptPrivacyPolicy { get; set; }

    public bool AcceptMarketing { get; set; } = false;

    // Device information
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
}