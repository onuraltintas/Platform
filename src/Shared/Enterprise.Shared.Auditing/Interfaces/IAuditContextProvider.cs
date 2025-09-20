namespace Enterprise.Shared.Auditing.Interfaces;

/// <summary>
/// Provides audit context information for the current operation
/// </summary>
public interface IAuditContextProvider
{
    /// <summary>
    /// Gets the current user ID
    /// </summary>
    string? GetCurrentUserId();

    /// <summary>
    /// Gets the current username
    /// </summary>
    string? GetCurrentUsername();

    /// <summary>
    /// Gets the current session ID
    /// </summary>
    string? GetCurrentSessionId();

    /// <summary>
    /// Gets the current IP address
    /// </summary>
    string? GetCurrentIpAddress();

    /// <summary>
    /// Gets the current user agent
    /// </summary>
    string? GetCurrentUserAgent();

    /// <summary>
    /// Gets the current correlation ID
    /// </summary>
    string? GetCurrentCorrelationId();

    /// <summary>
    /// Gets the current trace ID
    /// </summary>
    string? GetCurrentTraceId();

    /// <summary>
    /// Gets the current service name
    /// </summary>
    string? GetCurrentServiceName();

    /// <summary>
    /// Gets the current environment
    /// </summary>
    string? GetCurrentEnvironment();

    /// <summary>
    /// Gets the current security context
    /// </summary>
    SecurityContext? GetCurrentSecurityContext();

    /// <summary>
    /// Gets the current claims principal
    /// </summary>
    ClaimsPrincipal? GetCurrentUser();

    /// <summary>
    /// Gets additional context properties
    /// </summary>
    Dictionary<string, object> GetContextProperties();

    /// <summary>
    /// Sets a context property
    /// </summary>
    void SetContextProperty(string key, object value);

    /// <summary>
    /// Removes a context property
    /// </summary>
    void RemoveContextProperty(string key);

    /// <summary>
    /// Clears all context properties
    /// </summary>
    void ClearContextProperties();
}