using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;

namespace Enterprise.Shared.Observability.Services;

public class CorrelationContextAccessor : ICorrelationContextAccessor
{
    private static readonly AsyncLocal<CorrelationContext?> _correlationContext = new();

    public CorrelationContext? CorrelationContext
    {
        get => _correlationContext.Value;
        set => _correlationContext.Value = value;
    }
    
    public string? CorrelationId => CorrelationContext?.CorrelationId;
}