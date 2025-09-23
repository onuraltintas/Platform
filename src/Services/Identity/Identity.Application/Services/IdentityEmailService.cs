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

        // Frontend URL - should be configurable in production
        var frontendUrl = "http://localhost:4200";
        var verificationLink = $"{frontendUrl}/auth/verify-email?token={Uri.EscapeDataString(confirmationToken)}&email={Uri.EscapeDataString(email)}";

        var body = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background-color: #f8f9fa; padding: 30px; border-radius: 0 0 5px 5px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }}
        .footer {{ margin-top: 20px; padding-top: 20px; border-top: 1px solid #ddd; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2>E-posta Doğrulama</h2>
        </div>
        <div class='content'>
            <p>Merhaba {firstName},</p>
            <p>Hesabınızı oluşturduğunuz için teşekkür ederiz! E-posta adresinizi doğrulamak için lütfen aşağıdaki butona tıklayın:</p>
            <center>
                <a href='{verificationLink}' class='button'>E-postamı Doğrula</a>
            </center>
            <p>Ya da aşağıdaki linki tarayıcınıza kopyalayabilirsiniz:</p>
            <p style='word-break: break-all; background: #fff; padding: 10px; border-radius: 3px;'>{verificationLink}</p>
            <div class='footer'>
                <p>Bu e-postayı beklemiyordunuz? Bu mesajı göz ardı edebilirsiniz.</p>
                <p>Bu link 24 saat geçerlidir.</p>
            </div>
        </div>
    </div>
</body>
</html>";

        var message = new EmailMessage { To = email, Subject = subject, Body = body, IsHtml = true };
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
        var frontendUrl = "http://localhost:4200";
        var resetUrl = $"{frontendUrl}/auth/reset-password?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(email)}";

        var htmlBody = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Şifre Sıfırlama</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f7f9fc;
        }}
        .container {{
            background: white;
            padding: 40px;
            border-radius: 12px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            text-align: center;
            margin-bottom: 30px;
        }}
        .logo {{
            font-size: 24px;
            font-weight: bold;
            color: #6366f1;
            margin-bottom: 10px;
        }}
        .title {{
            font-size: 24px;
            color: #1e293b;
            margin-bottom: 20px;
        }}
        .message {{
            color: #64748b;
            margin-bottom: 30px;
            line-height: 1.6;
        }}
        .reset-button {{
            display: inline-block;
            background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
            color: white;
            padding: 14px 28px;
            text-decoration: none;
            border-radius: 8px;
            font-weight: 600;
            text-align: center;
            margin: 20px 0;
        }}
        .reset-button:hover {{
            transform: translateY(-1px);
        }}
        .alternative-link {{
            background-color: #f1f5f9;
            padding: 15px;
            border-radius: 8px;
            margin-top: 20px;
            word-break: break-all;
            font-size: 14px;
            color: #64748b;
        }}
        .footer {{
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #e2e8f0;
            color: #64748b;
            font-size: 14px;
            text-align: center;
        }}
        .warning {{
            background-color: #fef3c7;
            border: 1px solid #f59e0b;
            color: #92400e;
            padding: 15px;
            border-radius: 8px;
            margin: 20px 0;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <div class='logo'>OnAl Platform</div>
            <h1 class='title'>Şifre Sıfırlama</h1>
        </div>

        <div class='message'>
            <p>Merhaba {firstName},</p>
            <p>Hesabınız için şifre sıfırlama talebinde bulunuldu. Şifrenizi sıfırlamak için aşağıdaki butona tıklayın:</p>
        </div>

        <div style='text-align: center;'>
            <a href='{resetUrl}' class='reset-button'>Şifremi Sıfırla</a>
        </div>

        <div class='warning'>
            <strong>Güvenlik Uyarısı:</strong> Bu bağlantı 1 saat geçerlidir. Eğer şifre sıfırlama talebinde bulunmadıysanız, bu e-postayı görmezden gelin.
        </div>

        <div class='alternative-link'>
            <p><strong>Buton çalışmıyor mu?</strong> Aşağıdaki bağlantıyı tarayıcınıza kopyalayarak yapıştırın:</p>
            <p>{resetUrl}</p>
        </div>

        <div class='footer'>
            <p>Bu e-posta OnAl Platform tarafından otomatik olarak gönderilmiştir.</p>
            <p>Herhangi bir sorunuz varsa bizimle iletişime geçin.</p>
        </div>
    </div>
</body>
</html>";

        var emailMessage = new EmailMessage
        {
            To = email,
            Subject = "Şifre Sıfırlama - OnAl Platform",
            Body = htmlBody,
            IsHtml = true
        };

        var result = await _emailService.SendAsync(emailMessage, cancellationToken);
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

