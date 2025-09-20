using Enterprise.Shared.Common.Models;

namespace Identity.Core.Interfaces;

public interface IEmailService
{
    Task<Result<bool>> SendEmailConfirmationAsync(string email, string firstName, string confirmationToken, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendWelcomeEmailAsync(string email, string firstName, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendPasswordResetEmailAsync(string email, string firstName, string resetToken, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendAccountLockedEmailAsync(string email, string firstName, DateTime lockedUntil, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendSuspiciousActivityEmailAsync(string email, string firstName, string activity, string ipAddress, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendNewDeviceLoginEmailAsync(string email, string firstName, string deviceName, string ipAddress, CancellationToken cancellationToken = default);
    Task<Result<bool>> SendGroupInvitationEmailAsync(string email, string firstName, string groupName, string invitationCode, CancellationToken cancellationToken = default);
}