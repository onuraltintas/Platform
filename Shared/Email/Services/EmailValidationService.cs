using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using EgitimPlatform.Shared.Email.Configuration;
using EgitimPlatform.Shared.Email.Services;

namespace EgitimPlatform.Shared.Email.Services;

public class EmailValidationService : IEmailValidationService
{
    private readonly EmailOptions _options;
    private readonly ILogger<EmailValidationService> _logger;
    private readonly HttpClient _httpClient;
    
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    private static readonly HashSet<string> DisposableEmailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "tempmail.org", "10minutemail.com", "guerrillamail.com", "mailinator.com",
        "temp-mail.org", "throwaway.email", "getnada.com", "maildrop.cc",
        "sharklasers.com", "grr.la", "trashmail.com", "yopmail.com"
    };

    public EmailValidationService(
        IOptions<EmailOptions> options,
        ILogger<EmailValidationService> logger,
        HttpClient httpClient)
    {
        _options = options.Value;
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task<bool> IsValidEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            // Basic format validation
            if (!EmailRegex.IsMatch(email))
                return false;

            // Domain validation if enabled
            if (_options.Validation.ValidateDomains)
            {
                var domain = GetDomainFromEmail(email);
                if (!await IsDomainValidAsync(domain, cancellationToken))
                    return false;
            }

            // Disposable email check if enabled
            if (_options.Validation.BlockDisposableEmails)
            {
                if (await IsDisposableEmailAsync(email, cancellationToken))
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating email: {Email}", email);
            return false;
        }
    }

    public async Task<EmailValidationResult> ValidateEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var result = new EmailValidationResult
        {
            Email = email
        };

        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                result.ValidationErrors.Add("Email is required");
                return result;
            }

            // Basic format validation
            if (!EmailRegex.IsMatch(email))
            {
                result.ValidationErrors.Add("Invalid email format");
                return result;
            }

            result.IsValid = true;
            result.Domain = GetDomainFromEmail(email);

            // Domain validation
            if (_options.Validation.ValidateDomains && !string.IsNullOrEmpty(result.Domain))
            {
                result.HasMxRecord = await HasMxRecordAsync(result.Domain, cancellationToken);
                
                if (!result.HasMxRecord)
                {
                    result.IsDeliverable = false;
                    result.ValidationErrors.Add("Domain does not have MX record");
                }
                else
                {
                    result.IsDeliverable = true;
                }
            }

            // Disposable email check
            if (_options.Validation.BlockDisposableEmails)
            {
                result.IsDisposable = await IsDisposableEmailAsync(email, cancellationToken);
                
                if (result.IsDisposable)
                {
                    result.ValidationErrors.Add("Disposable email addresses are not allowed");
                }
            }

            // Catch-all check (simplified implementation)
            result.IsCatchAll = await IsCatchAllDomainAsync(result.Domain, cancellationToken);

            // Add metadata
            result.Metadata["validatedAt"] = DateTime.UtcNow;
            result.Metadata["validationLevel"] = _options.Validation.ValidationLevel.ToString();
            
            result.IsValid = result.ValidationErrors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during email validation: {Email}", email);
            result.ValidationErrors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    public async Task<IEnumerable<EmailValidationResult>> ValidateEmailsAsync(IEnumerable<string> emails, CancellationToken cancellationToken = default)
    {
        var emailList = emails.ToList();
        _logger.LogDebug("Validating {Count} emails", emailList.Count);

        var tasks = emailList.Select(email => ValidateEmailAsync(email, cancellationToken));
        var results = await Task.WhenAll(tasks);

        _logger.LogDebug("Completed validation of {Count} emails. Valid: {ValidCount}", 
            emailList.Count, results.Count(r => r.IsValid));

        return results;
    }

    public async Task<bool> IsDomainValidAsync(string domain, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            // Check for MX record
            return await HasMxRecordAsync(domain, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating domain: {Domain}", domain);
            return false;
        }
    }

    public async Task<bool> IsDisposableEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        try
        {
            var domain = GetDomainFromEmail(email);
            if (string.IsNullOrEmpty(domain))
                return false;

            // Check against known disposable domains
            if (DisposableEmailDomains.Contains(domain))
                return true;

            // Check against custom blocked domains
            if (_options.Validation.BlockedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
                return true;

            // If external API is configured, check with it
            if (!string.IsNullOrEmpty(_options.Validation.DisposableEmailApiUrl))
            {
                return await CheckDisposableEmailWithApiAsync(domain, cancellationToken);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking disposable email: {Email}", email);
            return false;
        }
    }

    private async Task<bool> HasMxRecordAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(domain, 5000);
            
            // This is a simplified check - in production, you'd want to use proper DNS queries
            // Consider using DnsClient library for more robust DNS lookups
            return reply.Status == IPStatus.Success;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> IsCatchAllDomainAsync(string domain, CancellationToken cancellationToken)
    {
        // Simplified implementation - in production, you'd want to test with a random email
        // and see if it gets accepted by the mail server
        try
        {
            if (string.IsNullOrWhiteSpace(domain))
                return false;

            // This would require SMTP connection testing which is complex
            // For now, return false (not catch-all)
            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> CheckDisposableEmailWithApiAsync(string domain, CancellationToken cancellationToken)
    {
        try
        {
            var apiUrl = _options.Validation.DisposableEmailApiUrl!
                .Replace("{domain}", Uri.EscapeDataString(domain));

            using var response = await _httpClient.GetAsync(apiUrl, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                
                // This depends on the API format - adjust based on your chosen service
                // Example for a simple JSON response: {"disposable": true}
                return content.Contains("\"disposable\":true", StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check disposable email API for domain: {Domain}", domain);
            return false;
        }
    }

    private static string GetDomainFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var atIndex = email.LastIndexOf('@');
        if (atIndex < 0 || atIndex == email.Length - 1)
            return string.Empty;

        return email.Substring(atIndex + 1).ToLowerInvariant();
    }
}