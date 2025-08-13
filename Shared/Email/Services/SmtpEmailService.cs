using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using EgitimPlatform.Shared.Email.Configuration;
using EgitimPlatform.Shared.Email.Models;
using System.Net;

namespace EgitimPlatform.Shared.Email.Services;

public class SmtpEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IOptions<EmailOptions> options,
        IEmailTemplateService templateService,
        ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task<EmailDeliveryResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Preparing to send email with ID: {MessageId} to {Recipients}", 
                message.Id, string.Join(", ", message.To.Select(t => t.Email)));

            // Process template if specified
            if (!string.IsNullOrEmpty(message.TemplateName))
            {
                message.Body = await _templateService.RenderTemplateAsync(message.TemplateName, message.TemplateData, cancellationToken);
                message.IsHtml = true;
            }

            // Create MimeMessage
            var mimeMessage = CreateMimeMessage(message);

            // Send email
            using var client = new SmtpClient();
            await ConnectAsync(client, cancellationToken);
            await AuthenticateAsync(client, cancellationToken);

            var response = await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Successfully sent email with ID: {MessageId}", message.Id);

            return EmailDeliveryResult.Success(message.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email with ID: {MessageId}", message.Id);
            return EmailDeliveryResult.Failure(message.Id, ex.Message, ex);
        }
    }

    public async Task<BulkEmailResult> SendBulkEmailAsync(IEnumerable<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var messageList = messages.ToList();
        var result = new BulkEmailResult
        {
            TotalEmails = messageList.Count,
            StartedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Starting bulk email send for {Count} messages", messageList.Count);

        var successCount = 0;
        var failCount = 0;
        
        var semaphore = new SemaphoreSlim(_options.Throttling.BulkEmailBatchSize, _options.Throttling.BulkEmailBatchSize);
        var tasks = messageList.Select(async message =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var deliveryResult = await SendEmailAsync(message, cancellationToken);
                
                if (deliveryResult.IsSuccess)
                {
                    Interlocked.Increment(ref successCount);
                }
                else
                {
                    Interlocked.Increment(ref failCount);
                }
                
                result.Results.Add(deliveryResult);
                
                // Add delay between emails if configured
                if (_options.Throttling.DelayBetweenEmailsMs > 0)
                {
                    await Task.Delay(_options.Throttling.DelayBetweenEmailsMs, cancellationToken);
                }
                
                return deliveryResult;
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        
        result.SuccessfulDeliveries = successCount;
        result.FailedDeliveries = failCount;
        result.CompletedAt = DateTime.UtcNow;
        result.TotalProcessingTime = result.CompletedAt.Value - result.StartedAt;

        _logger.LogInformation("Completed bulk email send. Success: {Success}, Failed: {Failed}, Total time: {Time}ms",
            result.SuccessfulDeliveries, result.FailedDeliveries, result.TotalProcessingTime.TotalMilliseconds);

        return result;
    }

    public async Task<EmailDeliveryResult> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage(to, subject, body, isHtml);
        return await SendEmailAsync(message, cancellationToken);
    }

    public async Task<EmailDeliveryResult> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage(to.ToList(), subject, body, isHtml);
        return await SendEmailAsync(message, cancellationToken);
    }

    public async Task<EmailDeliveryResult> SendTemplateEmailAsync(string to, string templateName, object templateData, string? subject = null, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            Subject = subject ?? string.Empty,
            TemplateName = templateName,
            TemplateData = ConvertToTemplateData(templateData)
        };
        message.AddTo(to);

        return await SendEmailAsync(message, cancellationToken);
    }

    public async Task<EmailDeliveryResult> SendTemplateEmailAsync(IEnumerable<string> to, string templateName, object templateData, string? subject = null, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            Subject = subject ?? string.Empty,
            TemplateName = templateName,
            TemplateData = ConvertToTemplateData(templateData)
        };
        
        foreach (var email in to)
        {
            message.AddTo(email);
        }

        return await SendEmailAsync(message, cancellationToken);
    }

    public async Task<bool> ValidateEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var mailAddress = new System.Net.Mail.MailAddress(email);
            return mailAddress.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public async Task<EmailDeliveryStatistics> GetDeliveryStatisticsAsync(DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        // This would typically query a database or logging system
        // For now, return empty statistics
        return new EmailDeliveryStatistics();
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient();
            await ConnectAsync(client, cancellationToken);
            await AuthenticateAsync(client, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
            
            _logger.LogInformation("SMTP connection test successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP connection test failed");
            return false;
        }
    }

    private MimeMessage CreateMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // From
        var fromAddress = message.From.Email.IsNullOrEmpty() 
            ? new MailboxAddress(_options.Settings.DefaultFromName, _options.Settings.DefaultFromEmail)
            : new MailboxAddress(message.From.Name, message.From.Email);
        mimeMessage.From.Add(fromAddress);

        // To
        foreach (var to in message.To)
        {
            mimeMessage.To.Add(new MailboxAddress(to.Name, to.Email));
        }

        // Cc
        foreach (var cc in message.Cc)
        {
            mimeMessage.Cc.Add(new MailboxAddress(cc.Name, cc.Email));
        }

        // Bcc
        foreach (var bcc in message.Bcc)
        {
            mimeMessage.Bcc.Add(new MailboxAddress(bcc.Name, bcc.Email));
        }

        // Reply-To
        if (message.ReplyTo != null && !message.ReplyTo.Email.IsNullOrEmpty())
        {
            mimeMessage.ReplyTo.Add(new MailboxAddress(message.ReplyTo.Name, message.ReplyTo.Email));
        }
        else if (!_options.Settings.DefaultReplyToEmail.IsNullOrEmpty())
        {
            mimeMessage.ReplyTo.Add(new MailboxAddress(_options.Settings.DefaultReplyToName, _options.Settings.DefaultReplyToEmail));
        }

        // Subject
        var subjectPrefix = _options.Settings.DefaultSubjectPrefix;
        mimeMessage.Subject = message.Subject.StartsWith(subjectPrefix) 
            ? message.Subject 
            : $"{subjectPrefix}{message.Subject}";

        // Priority
        mimeMessage.Priority = ConvertPriority(message.Priority);

        // Headers
        foreach (var header in message.Headers)
        {
            mimeMessage.Headers.Add(header.Key, header.Value);
        }

        // Message ID
        mimeMessage.MessageId = $"{message.Id}@{_options.Smtp.Domain ?? Environment.MachineName}";

        // Body and attachments
        var bodyBuilder = new BodyBuilder();
        
        if (message.IsHtml)
        {
            bodyBuilder.HtmlBody = message.Body;
        }
        else
        {
            bodyBuilder.TextBody = message.Body;
        }

        // Attachments
        foreach (var attachment in message.Attachments)
        {
            if (attachment.IsInline && !string.IsNullOrEmpty(attachment.ContentId))
            {
                bodyBuilder.LinkedResources.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));
            }
            else
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType ?? "application/octet-stream"));
            }
        }

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        return mimeMessage;
    }

    private async Task ConnectAsync(SmtpClient client, CancellationToken cancellationToken)
    {
        var secureSocketOptions = _options.Smtp.EnableSsl ? SecureSocketOptions.SslOnConnect :
                                 _options.Smtp.EnableStartTls ? SecureSocketOptions.StartTls :
                                 SecureSocketOptions.None;

        await client.ConnectAsync(_options.Smtp.Host, _options.Smtp.Port, secureSocketOptions, cancellationToken);
    }

    private async Task AuthenticateAsync(SmtpClient client, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_options.Smtp.Username) && !string.IsNullOrEmpty(_options.Smtp.Password))
        {
            await client.AuthenticateAsync(_options.Smtp.Username, _options.Smtp.Password, cancellationToken);
        }
    }

    private static MessagePriority ConvertPriority(EmailPriority priority)
    {
        return priority switch
        {
            EmailPriority.Low => MessagePriority.NonUrgent,
            EmailPriority.Normal => MessagePriority.Normal,
            EmailPriority.High => MessagePriority.Urgent,
            EmailPriority.Urgent => MessagePriority.Urgent,
            _ => MessagePriority.Normal
        };
    }

    private Dictionary<string, object> ConvertToTemplateData(object data)
    {
        if (data is Dictionary<string, object> dict)
        {
            return dict;
        }
        
        var result = new Dictionary<string, object>();
        var properties = data.GetType().GetProperties();
        
        foreach (var property in properties)
        {
            var value = property.GetValue(data);
            if (value != null)
            {
                result[property.Name] = value;
            }
        }
        
        return result;
    }
}

internal static class StringExtensions
{
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }
}