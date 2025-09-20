namespace Enterprise.Shared.Security.Interfaces;

/// <summary>
/// Service for validating security-related inputs
/// </summary>
public interface ISecurityValidator
{
    /// <summary>
    /// Validates password strength
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <returns>Validation result with details</returns>
    PasswordValidationResult ValidatePassword(string password);

    /// <summary>
    /// Checks if an input contains SQL injection patterns
    /// </summary>
    /// <param name="input">The input to check</param>
    /// <returns>True if suspicious patterns detected</returns>
    bool ContainsSqlInjectionPattern(string input);

    /// <summary>
    /// Checks if an input contains XSS patterns
    /// </summary>
    /// <param name="input">The input to check</param>
    /// <returns>True if XSS patterns detected</returns>
    bool ContainsXssPattern(string input);

    /// <summary>
    /// Sanitizes input to prevent XSS
    /// </summary>
    /// <param name="input">The input to sanitize</param>
    /// <returns>Sanitized input</returns>
    string SanitizeForXss(string input);

    /// <summary>
    /// Validates an email address
    /// </summary>
    /// <param name="email">The email to validate</param>
    /// <returns>True if valid email format</returns>
    bool IsValidEmail(string email);

    /// <summary>
    /// Validates a URL
    /// </summary>
    /// <param name="url">The URL to validate</param>
    /// <returns>True if valid URL format</returns>
    bool IsValidUrl(string url);

    /// <summary>
    /// Checks if a file type is allowed
    /// </summary>
    /// <param name="fileName">The file name to check</param>
    /// <param name="allowedExtensions">Allowed file extensions</param>
    /// <returns>True if file type is allowed</returns>
    bool IsAllowedFileType(string fileName, IEnumerable<string> allowedExtensions);

    /// <summary>
    /// Validates API key format
    /// </summary>
    /// <param name="apiKey">The API key to validate</param>
    /// <returns>True if valid format</returns>
    bool IsValidApiKey(string apiKey);
}

/// <summary>
/// Result of password validation
/// </summary>
public class PasswordValidationResult
{
    public bool IsValid { get; set; }
    public int Score { get; set; } // 0-100
    public List<string> Errors { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public PasswordStrength Strength { get; set; }
}

/// <summary>
/// Password strength levels
/// </summary>
public enum PasswordStrength
{
    VeryWeak = 0,
    Weak = 1,
    Fair = 2,
    Strong = 3,
    VeryStrong = 4
}