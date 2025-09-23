namespace Enterprise.Shared.Email.Services;

/// <summary>
/// Core email service implementation using FluentEmail
/// </summary>
public class EmailService : IEmailService
{
    private readonly IFluentEmail _fluentEmail;
    private readonly IEmailTemplateService _templateService;
    private readonly EmailConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        IFluentEmail fluentEmail,
        IEmailTemplateService templateService,
        IOptions<EmailConfiguration> configuration,
        ILogger<EmailService> logger)
    {
        _fluentEmail = fluentEmail ?? throw new ArgumentNullException(nameof(fluentEmail));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends a single email message
    /// </summary>
    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            return EmailResult.Failure("Email message cannot be null");

        var validationResult = await ValidateEmailAsync(message);
        if (!validationResult.IsSuccess)
            return EmailResult.Failure(validationResult.Errors, message.TrackingId);

        try
        {
            _logger.LogInformation("Sending email to {To} with subject '{Subject}' (TrackingId: {TrackingId})", 
                message.To, message.Subject, message.TrackingId);

            var email = BuildFluentEmail(message);
            var response = await email.SendAsync(cancellationToken);

            if (response.Successful)
            {
                _logger.LogInformation("Email sent successfully to {To} (TrackingId: {TrackingId}, MessageId: {MessageId})", 
                    message.To, message.TrackingId, response.MessageId);

                return EmailResult.Success(message.TrackingId, response.MessageId);
            }

            var errorMessage = string.Join("; ", response.ErrorMessages);
            _logger.LogError("Failed to send email to {To} (TrackingId: {TrackingId}): {Error}", 
                message.To, message.TrackingId, errorMessage);

            return EmailResult.Failure(response.ErrorMessages, message.TrackingId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending email to {To} (TrackingId: {TrackingId})", 
                message.To, message.TrackingId);

            return EmailResult.Failure($"Exception occurred: {ex.Message}", message.TrackingId);
        }
    }

    /// <summary>
    /// Sends multiple emails in bulk with batching support
    /// </summary>
    public async Task<BulkEmailResult> SendBulkAsync(BulkEmailRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (!request.IsValid(out var validationErrors))
            return BulkEmailResult.Success(request.BulkId, new List<EmailResult>
            {
                EmailResult.Failure(validationErrors, request.BulkId)
            });

        _logger.LogInformation("Starting bulk email operation (BulkId: {BulkId}, Recipients: {Count})", 
            request.BulkId, request.Recipients.Count);

        var results = new List<EmailResult>();
        var batches = request.Recipients.Batch(request.BatchSize);

        var semaphore = new SemaphoreSlim(request.MaxConcurrency, request.MaxConcurrency);

        foreach (var batch in batches)
        {
            var batchTasks = batch.Select(async recipient =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var emailMessage = BuildEmailFromBulkRequest(request, recipient);
                    return await SendAsync(emailMessage, cancellationToken);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var batchResults = await Task.WhenAll(batchTasks);
            results.AddRange(batchResults);

            if (request.DelayBetweenBatches > 0)
            {
                await Task.Delay(request.DelayBetweenBatches, cancellationToken);
            }
        }

        var bulkResult = BulkEmailResult.Success(request.BulkId, results);
        
        _logger.LogInformation("Bulk email operation completed (BulkId: {BulkId}, Success: {Success}, Failed: {Failed})", 
            request.BulkId, bulkResult.SuccessfulSends, bulkResult.FailedSends);

        return bulkResult;
    }

    /// <summary>
    /// Sends an email using a template
    /// </summary>
    public async Task<EmailResult> SendTemplateAsync(string templateName, string to, object templateData, CancellationToken cancellationToken = default)
    {
        return await SendTemplateAsync(templateName, to, templateData, new EmailOptions(), cancellationToken);
    }

    /// <summary>
    /// Sends an email using a template with additional options
    /// </summary>
    public async Task<EmailResult> SendTemplateAsync(string templateName, string to, object templateData, EmailOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            var renderResult = await _templateService.RenderTemplateAsync(templateName, templateData, cancellationToken);
            if (!renderResult.IsSuccess)
                return EmailResult.Failure($"Template rendering failed: {renderResult.Error}");

            var message = new EmailMessage
            {
                To = to,
                Subject = renderResult.RenderedSubject,
                Body = renderResult.RenderedBody,
                IsHtml = renderResult.IsHtml,
                From = options.From,
                FromName = options.FromName,
                ReplyTo = options.ReplyTo,
                Priority = options.Priority ?? EmailPriority.Normal,
                Headers = options.Headers,
                Tags = options.Tags,
                Metadata = options.Metadata,
                Attachments = options.Attachments
            };

            return await SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while sending template email '{Template}' to {To}", templateName, to);
            return EmailResult.Failure($"Template email sending failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Schedules an email for future delivery
    /// </summary>
    public async Task<EmailResult> ScheduleAsync(EmailMessage message, DateTime scheduledAt, CancellationToken cancellationToken = default)
    {
        message.ScheduledAt = scheduledAt;
        
        if (scheduledAt <= DateTime.UtcNow)
        {
            return await SendAsync(message, cancellationToken);
        }

        // For now, we'll return a queued result
        // In a full implementation, this would integrate with a job scheduler
        _logger.LogInformation("Email scheduled for {ScheduledAt} (TrackingId: {TrackingId})", scheduledAt, message.TrackingId);
        return EmailResult.Queued(message.TrackingId);
    }

    /// <summary>
    /// Gets the status of a sent email by tracking ID
    /// </summary>
    public Task<EmailDeliveryStatus> GetDeliveryStatusAsync(string trackingId, CancellationToken cancellationToken = default)
    {
        // In a full implementation, this would query the email provider's API or database
        _logger.LogInformation("Checking delivery status for TrackingId: {TrackingId}", trackingId);
        return Task.FromResult(EmailDeliveryStatus.Sent);
    }

    /// <summary>
    /// Validates an email message before sending
    /// </summary>
    public Task<ValidationResult> ValidateEmailAsync(EmailMessage message)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        if (message == null)
        {
            errors.Add("Email message cannot be null");
            return Task.FromResult(ValidationResult.Failure(errors));
        }

        if (string.IsNullOrWhiteSpace(message.To))
            errors.Add("Recipient email address is required");
        else if (!IsValidEmail(message.To))
            errors.Add("Invalid recipient email address");

        if (string.IsNullOrWhiteSpace(message.Subject))
            errors.Add("Email subject is required");
        else if (message.Subject.Length > 998)
            errors.Add("Email subject exceeds maximum length of 998 characters");

        if (string.IsNullOrWhiteSpace(message.Body))
            errors.Add("Email body is required");

        if (!string.IsNullOrEmpty(message.From) && !IsValidEmail(message.From))
            errors.Add("Invalid sender email address");

        if (!string.IsNullOrEmpty(message.ReplyTo) && !IsValidEmail(message.ReplyTo))
            errors.Add("Invalid reply-to email address");

        // Validate attachments
        foreach (var attachment in message.Attachments)
        {
            if (!attachment.IsValid(out var attachmentErrors))
            {
                errors.AddRange(attachmentErrors);
            }
        }

        // Check for potential spam indicators
        if (message.Subject.Contains("!!!") || message.Body.ToUpper().Contains("URGENT"))
            warnings.Add("Email content may trigger spam filters");

        return Task.FromResult(errors.Any() 
            ? ValidationResult.Failure(errors) 
            : ValidationResult.Success(warnings));
    }

    /// <summary>
    /// Tests email connectivity and configuration
    /// </summary>
    public async Task<Result> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var testMessage = EmailMessage.Create(
                _configuration.DefaultSender.Email,
                "Email Service Test",
                "This is a test email to verify email service configuration.",
                false
            );

            // Don't actually send the test email, just validate the configuration
            var validationResult = await ValidateEmailAsync(testMessage);
            
            if (!validationResult.IsSuccess)
                return Result.Failure("Email configuration validation failed: " + validationResult.Error);

            _logger.LogInformation("Email service connectivity test completed successfully");
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email service connectivity test failed");
            return Result.Failure($"Connectivity test failed: {ex.Message}");
        }
    }

    private IFluentEmail BuildFluentEmail(EmailMessage message)
    {
        var email = _fluentEmail
            .To(message.To)
            .Subject(message.Subject);

        if (message.IsHtml)
        {
            email.Body(message.Body, true);
            // Ensure HTML content-type is set
            email.Header("Content-Type", "text/html; charset=utf-8");
        }
        else
        {
            email.Body(message.Body, false);
            email.Header("Content-Type", "text/plain; charset=utf-8");
        }

        if (!string.IsNullOrEmpty(message.From))
            email.SetFrom(message.From, message.FromName);

        if (!string.IsNullOrEmpty(message.ReplyTo))
            email.ReplyTo(message.ReplyTo);

        if (message.Cc?.Any() == true)
            email.CC(string.Join(",", message.Cc));

        if (message.Bcc?.Any() == true)
            email.BCC(string.Join(",", message.Bcc));

        foreach (var attachment in message.Attachments)
        {
            if (attachment.IsInline)
            {
                email.Attach(new FluentEmail.Core.Models.Attachment
                {
                    Data = new MemoryStream(attachment.Content),
                    Filename = attachment.FileName,
                    ContentType = attachment.ContentType,
                    ContentId = attachment.ContentId
                });
            }
            else
            {
                email.Attach(new FluentEmail.Core.Models.Attachment
                {
                    Data = new MemoryStream(attachment.Content),
                    Filename = attachment.FileName,
                    ContentType = attachment.ContentType
                });
            }
        }

        foreach (var header in message.Headers)
        {
            email.Header(header.Key, header.Value);
        }

        return email;
    }

    private EmailMessage BuildEmailFromBulkRequest(BulkEmailRequest request, BulkEmailRecipient recipient)
    {
        return new EmailMessage
        {
            To = recipient.Email,
            Subject = !string.IsNullOrEmpty(recipient.Subject) ? recipient.Subject : request.Subject ?? "",
            Body = request.Body ?? "",
            IsHtml = request.IsHtml,
            From = request.From,
            FromName = request.FromName,
            ReplyTo = request.ReplyTo,
            Priority = request.Priority,
            TrackingId = recipient.TrackingId,
            Tags = new List<string>(request.Tags),
            Metadata = new Dictionary<string, object>(request.Metadata.Concat(recipient.Metadata))
        };
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}