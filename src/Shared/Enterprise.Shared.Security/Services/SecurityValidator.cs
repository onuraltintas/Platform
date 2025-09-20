using System.Text.RegularExpressions;
using System.Web;

namespace Enterprise.Shared.Security.Services;

/// <summary>
/// Service for validating security-related inputs
/// </summary>
public sealed partial class SecurityValidator : ISecurityValidator
{
    private readonly ILogger<SecurityValidator> _logger;
    private readonly SecuritySettings _settings;

    // Regex patterns
    private static readonly Regex EmailRegex = MyEmailRegex();
    private static readonly Regex UrlRegex = MyUrlRegex();
    private static readonly Regex SqlInjectionRegex = MySqlInjectionRegex();
    private static readonly Regex XssRegex = MyXssRegex();
    private static readonly Regex ApiKeyRegex = MyApiKeyRegex();

    public SecurityValidator(
        ILogger<SecurityValidator> logger,
        IOptions<SecuritySettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        _logger.LogDebug("Security validator initialized");
    }

    public PasswordValidationResult ValidatePassword(string password)
    {
        var result = new PasswordValidationResult();

        if (string.IsNullOrEmpty(password))
        {
            result.Errors.Add("Password is required");
            result.Strength = PasswordStrength.VeryWeak;
            return result;
        }

        var score = 0;
        var minLength = _settings.PasswordMinLength ?? 8;
        var requireUppercase = _settings.PasswordRequireUppercase ?? true;
        var requireLowercase = _settings.PasswordRequireLowercase ?? true;
        var requireDigit = _settings.PasswordRequireDigit ?? true;
        var requireSpecialChar = _settings.PasswordRequireSpecialChar ?? true;

        // Check length
        if (password.Length < minLength)
        {
            result.Errors.Add($"Password must be at least {minLength} characters long");
        }
        else
        {
            score += 20;
            if (password.Length >= 12) score += 10;
            if (password.Length >= 16) score += 10;
        }

        // Check uppercase
        var hasUppercase = password.Any(char.IsUpper);
        if (requireUppercase && !hasUppercase)
        {
            result.Errors.Add("Password must contain at least one uppercase letter");
        }
        else if (hasUppercase)
        {
            score += 15;
        }

        // Check lowercase
        var hasLowercase = password.Any(char.IsLower);
        if (requireLowercase && !hasLowercase)
        {
            result.Errors.Add("Password must contain at least one lowercase letter");
        }
        else if (hasLowercase)
        {
            score += 15;
        }

        // Check digit
        var hasDigit = password.Any(char.IsDigit);
        if (requireDigit && !hasDigit)
        {
            result.Errors.Add("Password must contain at least one digit");
        }
        else if (hasDigit)
        {
            score += 15;
        }

        // Check special character
        var hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));
        if (requireSpecialChar && !hasSpecialChar)
        {
            result.Errors.Add("Password must contain at least one special character");
        }
        else if (hasSpecialChar)
        {
            score += 15;
        }

        // Check for common patterns
        if (ContainsCommonPattern(password))
        {
            score -= 20;
            result.Suggestions.Add("Avoid common patterns like '123', 'abc', or 'qwerty'");
        }

        // Check for repeated characters
        if (HasExcessiveRepeatedCharacters(password))
        {
            score -= 10;
            result.Suggestions.Add("Avoid excessive repeated characters");
        }

        // Set final score and strength
        result.Score = Math.Max(0, Math.Min(100, score));
        result.IsValid = result.Errors.Count == 0;

        result.Strength = result.Score switch
        {
            >= 80 => PasswordStrength.VeryStrong,
            >= 60 => PasswordStrength.Strong,
            >= 40 => PasswordStrength.Fair,
            >= 20 => PasswordStrength.Weak,
            _ => PasswordStrength.VeryWeak
        };

        _logger.LogDebug("Password validation completed with strength: {Strength}", result.Strength);
        return result;
    }

    public bool ContainsSqlInjectionPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var suspicious = SqlInjectionRegex.IsMatch(input.ToLowerInvariant());

        if (suspicious)
        {
            _logger.LogWarning("Potential SQL injection pattern detected");
        }

        return suspicious;
    }

    public bool ContainsXssPattern(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        var suspicious = XssRegex.IsMatch(input.ToLowerInvariant());

        if (suspicious)
        {
            _logger.LogWarning("Potential XSS pattern detected");
        }

        return suspicious;
    }

    public string SanitizeForXss(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // HTML encode the input
        var sanitized = HttpUtility.HtmlEncode(input);

        // Additional sanitization for JavaScript contexts
        sanitized = sanitized.Replace("'", "&#39;");
        sanitized = sanitized.Replace("\"", "&quot;");

        return sanitized;
    }

    public bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
            return false;
            
        // Check for consecutive dots
        if (email.Contains(".."))
            return false;

        return EmailRegex.IsMatch(email);
    }

    public bool IsValidUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;

        return UrlRegex.IsMatch(url) && Uri.TryCreate(url, UriKind.Absolute, out _);
    }

    public bool IsAllowedFileType(string fileName, IEnumerable<string> allowedExtensions)
    {
        if (string.IsNullOrEmpty(fileName))
            return false;
        
        if (allowedExtensions == null || !allowedExtensions.Any())
            return true; // No restrictions

        var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(extension))
            return false;

        var allowed = allowedExtensions.Select(e => e.ToLowerInvariant().StartsWith(".") ? e : $".{e}");
        return allowed.Contains(extension);
    }

    public bool IsValidApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey))
            return false;

        // Check format (alphanumeric with dashes, minimum length)
        return ApiKeyRegex.IsMatch(apiKey) && apiKey.Length >= 32;
    }

    private static bool ContainsCommonPattern(string password)
    {
        var commonPatterns = new[]
        {
            "123", "abc", "qwerty", "password", "admin", "letmein",
            "111", "000", "aaa", "xyz", "test", "demo"
        };

        var lowerPassword = password.ToLowerInvariant();
        return commonPatterns.Any(pattern => lowerPassword.Contains(pattern));
    }

    private static bool HasExcessiveRepeatedCharacters(string password)
    {
        for (int i = 0; i < password.Length - 2; i++)
        {
            if (password[i] == password[i + 1] && password[i] == password[i + 2])
            {
                return true;
            }
        }
        return false;
    }

    // Source generators for compiled regex
    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9._+%-]*[a-zA-Z0-9])?@[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?\.[a-zA-Z]{2,}$", RegexOptions.Compiled)]
    private static partial Regex MyEmailRegex();

    [GeneratedRegex(@"^https?://(([\w\-]+\.)+[\w\-]+|localhost)(:[0-9]+)?(/[\w\-._~:/?#[\]@!$&'()*+,;=]*)?$", RegexOptions.Compiled)]
    private static partial Regex MyUrlRegex();

    [GeneratedRegex(@"(\bunion\b|\bselect\b|\binsert\b|\bupdate\b|\bdelete\b|\bdrop\b|\bcreate\b|\balter\b|\bexec\b|\bscript\b|--|;|'|""|/\*|\*/|xp_|sp_|<script|javascript:|onerror=|onload=)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MySqlInjectionRegex();

    [GeneratedRegex(@"(<script|<iframe|<object|<embed|<form|javascript:|onerror=|onload=|onclick=|<svg|<img\s+src)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex MyXssRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9\-_]+$", RegexOptions.Compiled)]
    private static partial Regex MyApiKeyRegex();
}