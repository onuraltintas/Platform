namespace Enterprise.Shared.Auditing.Models;

/// <summary>
/// Represents the result of a validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Initializes a new instance of the ValidationResult class
    /// </summary>
    public ValidationResult(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the validation was successful
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the validation error message, if any
    /// </summary>
    public string? Error { get; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new(true);

    /// <summary>
    /// Creates a failed validation result with an error message
    /// </summary>
    public static ValidationResult Failure(string error) => new(false, error);
}