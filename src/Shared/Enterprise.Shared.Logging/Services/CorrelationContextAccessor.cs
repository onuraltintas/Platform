using Enterprise.Shared.Logging.Interfaces;
using Enterprise.Shared.Logging.Models;

namespace Enterprise.Shared.Logging.Services;

/// <summary>
/// Thread-safe correlation context accessor using AsyncLocal
/// </summary>
public class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<CorrelationContext?> _correlationContext = new();

    /// <summary>
    /// Gets the current correlation context
    /// </summary>
    public CorrelationContext? CorrelationContext => _correlationContext.Value;

    /// <summary>
    /// Sets the current correlation context
    /// </summary>
    /// <param name="context">Correlation context to set</param>
    public void SetCorrelationContext(CorrelationContext context)
    {
        _correlationContext.Value = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Clears the current correlation context
    /// </summary>
    public void ClearCorrelationContext()
    {
        _correlationContext.Value = null;
    }

    /// <summary>
    /// Creates a new correlation context and sets it as current
    /// </summary>
    /// <param name="parentId">Parent correlation ID</param>
    /// <returns>New correlation context</returns>
    public CorrelationContext CreateAndSetContext(string? parentId = null)
    {
        var context = CorrelationContext.Create(parentId);
        SetCorrelationContext(context);
        return context;
    }
}