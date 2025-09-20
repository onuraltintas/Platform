using Enterprise.Shared.Logging.Models;

namespace Enterprise.Shared.Logging.Interfaces;

/// <summary>
/// Provides access to the current correlation context
/// </summary>
public interface ICorrelationContextAccessor
{
    /// <summary>
    /// Gets the current correlation context
    /// </summary>
    CorrelationContext? CorrelationContext { get; }

    /// <summary>
    /// Sets the current correlation context
    /// </summary>
    /// <param name="context">Correlation context to set</param>
    void SetCorrelationContext(CorrelationContext context);

    /// <summary>
    /// Clears the current correlation context
    /// </summary>
    void ClearCorrelationContext();

    /// <summary>
    /// Creates a new correlation context and sets it as current
    /// </summary>
    /// <param name="parentId">Parent correlation ID</param>
    /// <returns>New correlation context</returns>
    CorrelationContext CreateAndSetContext(string? parentId = null);
}