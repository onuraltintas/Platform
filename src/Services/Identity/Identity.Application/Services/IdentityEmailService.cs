using Identity.Core.Interfaces;
using Enterprise.Shared.Email.Interfaces;
using Enterprise.Shared.Email.Models;
using Enterprise.Shared.Common.Models;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Services;

public class IdentityEmailService : Identity.Core.Interfaces.IEmailService
{
    private readonly Enterprise.Shared.Email.Interfaces.IEmailService _emailService;
    private readonly ILogger<IdentityEmailService> _logger;

    public IdentityEmailService(
        Enterprise.Shared.Email.Interfaces.IEmailService emailService,
        ILogger<IdentityEmailService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<bool>> SendEmailConfirmationAsync(string email, string firstName, string confirmationToken, CancellationToken cancellationToken = default)
    {
        var subject = "E-posta Doğrulama";
        var body = $"Merhaba {firstName},\n\nE-posta adresinizi doğrulamak için aşağıdaki kodu kullanın veya doğrulama bağlantısına tıklayın.\n\nKod/Token: {confirmationToken}\n\nTeşekkürler.";
        var message = new EmailMessage { To = email, Subject = subject, Body = body, IsHtml = false };
        var result = await _emailService.SendAsync(message, cancellationToken);
        return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
    }

    public async Task<Result<bool>> SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default)
    {
        var subject = "Aramıza Hoş Geldiniz";
        var body = $"Merhaba {firstName},\n\nKayıt işleminiz başarıyla tamamlandı. Platformumuza hoş geldiniz!";
        var message = new EmailMessage { To = email, Subject = subject, Body = body, IsHtml = false };
        var result = await _emailService.SendAsync(message, cancellationToken);
        return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
    }

    public async Task<Result<bool>> SendPasswordResetEmailAsync(string email, string firstName, string resetToken, CancellationToken cancellationToken = default)
    {
        var subject = "Şifre Sıfırlama";
        var body = $"Merhaba {firstName},\n\nŞifrenizi sıfırlamak için bu kodu kullanın: {resetToken}\n\nEğer bu isteği siz yapmadıysanız bu e-postayı yok sayabilirsiniz.";
        var message = new EmailMessage { To = email, Subject = subject, Body = body, IsHtml = false };
        var result = await _emailService.SendAsync(message, cancellationToken);
        return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
    }

    public async Task<Result<bool>> SendAccountLockedEmailAsync(string email, string firstName, DateTime lockedUntil, CancellationToken cancellationToken = default)
    {
        var subject = "Hesap Kilitlendi";
        var body = $"Merhaba {firstName},\n\nHesabınız başarısız giriş denemeleri nedeniyle {lockedUntil:yyyy-MM-dd HH:mm} tarihine kadar kilitlenmiştir.";
        var message = new EmailMessage { To = email, Subject = subject, Body = body, IsHtml = false };
        var result = await _emailService.SendAsync(message, cancellationToken);
        return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
    }

    public async Task<Result<bool>> SendSuspiciousActivityEmailAsync(string email, string firstName, string activity, string ipAddress, CancellationToken cancellationToken = default)
    {
        var subject = "Şüpheli Aktivite";
        var body = $"Merhaba {firstName},\n\nHesabınızda şüpheli bir aktivite tespit edildi: {activity}. IP: {ipAddress}. Eğer bu işlem size ait değilse lütfen şifrenizi değiştirin.";
        var message = new EmailMessage { To = email, Subject = subject, Body = body, IsHtml = false };
        var result = await _emailService.SendAsync(message, cancellationToken);
        return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
    }

    public async Task<Result<bool>> SendNewDeviceLoginEmailAsync(string email, string firstName, string deviceName, string ipAddress, CancellationToken cancellationToken = default)
    {
        var subject = "Yeni Cihaz Girişi";
        var body = $"Merhaba {firstName},\n\nHesabınıza yeni bir cihazdan giriş yapıldı. Cihaz: {deviceName}, IP: {ipAddress}. Bu siz değilseniz şifrenizi değiştirin.";
        var message = new EmailMessage { To = email, Subject = subject, Body = body, IsHtml = false };
        var result = await _emailService.SendAsync(message, cancellationToken);
        return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
    }

    public async Task<Result<bool>> SendGroupInvitationEmailAsync(string email, string firstName, string groupName, string invitationCode, CancellationToken cancellationToken = default)
    {
        var subject = "Grup Daveti";
        var body = $"Merhaba {firstName},\n\n'{groupName}' adlı gruba katılmak için bu davet kodunu kullanın: {invitationCode}.";
        var message = new EmailMessage { To = email, Subject = subject, Body = body, IsHtml = false };
        var result = await _emailService.SendAsync(message, cancellationToken);
        return result.IsSuccess ? Result<bool>.Success(true) : Result<bool>.Failure(result.Error);
    }
}

