namespace Enterprise.Shared.Auditing.Interceptors;

/// <summary>
/// Castle DynamicProxy interceptor for method-level auditing
/// </summary>
public class AuditInterceptor : IInterceptor
{
    private readonly IAuditService _auditService;
    private readonly ILogger<AuditInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the AuditInterceptor
    /// </summary>
    public AuditInterceptor(IAuditService auditService, ILogger<AuditInterceptor> logger)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Intercepts method calls for auditing
    /// </summary>
    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        var targetType = invocation.TargetType ?? method.DeclaringType!;

        // Check if method or class has NoAudit attribute
        if (HasNoAuditAttribute(method) || HasNoAuditAttribute(targetType))
        {
            invocation.Proceed();
            return;
        }

        // Get audit attributes (method-level takes precedence)
        var auditAttribute = GetAuditAttribute(method) ?? GetAuditAttribute(targetType);
        var securityAttribute = GetSecurityAuditAttribute(method) ?? GetSecurityAuditAttribute(targetType);

        // If no audit attributes found, proceed without auditing
        if (auditAttribute == null && securityAttribute == null)
        {
            invocation.Proceed();
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? exception = null;
        object? result = null;

        try
        {
            invocation.Proceed();
            result = invocation.ReturnValue;
        }
        catch (Exception ex)
        {
            exception = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            Task.Run(async () =>
            {
                try
                {
                    await LogAuditEventAsync(
                        invocation,
                        auditAttribute,
                        securityAttribute,
                        stopwatch.ElapsedMilliseconds,
                        result,
                        exception);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to log audit event for method {Method}", 
                        $"{targetType.Name}.{method.Name}");
                }
            });
        }
    }

    /// <summary>
    /// Logs the audit event based on attributes and method execution
    /// </summary>
    private async Task LogAuditEventAsync(
        IInvocation invocation,
        AuditAttribute? auditAttribute,
        SecurityAuditAttribute? securityAttribute,
        long durationMs,
        object? result,
        Exception? exception)
    {
        var method = invocation.Method;
        var targetType = invocation.TargetType ?? method.DeclaringType!;

        // Use security attribute if available, otherwise audit attribute
        var attribute = (AuditAttribute?)securityAttribute ?? auditAttribute;
        if (attribute == null) return;

        // Skip if method should be excluded
        if (attribute.Exclude) return;

        var isSuccess = exception == null;
        var shouldAuditSuccess = attribute.AuditSuccess && isSuccess;
        var shouldAuditFailure = attribute.AuditFailure && !isSuccess;

        if (!shouldAuditSuccess && !shouldAuditFailure) return;

        // Create appropriate audit event
        if (securityAttribute != null)
        {
            var securityEvent = CreateSecurityAuditEvent(
                invocation, securityAttribute, durationMs, result, exception);
            await _auditService.LogSecurityEventAsync(securityEvent);
        }
        else
        {
            var auditEvent = CreateAuditEvent(
                invocation, auditAttribute!, durationMs, result, exception);
            await _auditService.LogEventAsync(auditEvent);
        }
    }

    /// <summary>
    /// Creates a standard audit event
    /// </summary>
    private static AuditEvent CreateAuditEvent(
        IInvocation invocation,
        AuditAttribute attribute,
        long durationMs,
        object? result,
        Exception? exception)
    {
        var method = invocation.Method;
        var targetType = invocation.TargetType ?? method.DeclaringType!;

        var action = attribute.Action ?? method.Name;
        var resource = attribute.Resource ?? targetType.Name;
        var eventResult = exception == null ? "Success" : "Failed";

        var auditEvent = AuditEvent.Create(action, resource, eventResult)
            .WithMetadata(new Dictionary<string, object>
            {
                ["MethodName"] = method.Name,
                ["ClassName"] = targetType.Name,
                ["Assembly"] = targetType.Assembly.GetName().Name ?? "Unknown"
            });

        auditEvent.Category = attribute.Category;
        auditEvent.Severity = exception != null ? AuditSeverity.Error : attribute.Severity;
        auditEvent.DurationMs = durationMs;
        auditEvent.Details = attribute.Details ?? (exception?.Message);
        auditEvent.Tags = attribute.Tags?.ToList() ?? new List<string>();

        // Add parameters if requested
        if (attribute.IncludeParameters && invocation.Arguments.Length > 0)
        {
            auditEvent.Properties["Parameters"] = GetSafeParameters(method, invocation.Arguments);
        }

        // Add return value if requested and available
        if (attribute.IncludeReturnValue && result != null && exception == null)
        {
            auditEvent.Properties["ReturnValue"] = GetSafeReturnValue(result) ?? "[null return value]";
        }

        // Add custom properties if specified
        if (!string.IsNullOrEmpty(attribute.Properties))
        {
            try
            {
                var customProps = JsonSerializer.Deserialize<Dictionary<string, object>>(attribute.Properties);
                if (customProps != null)
                {
                    foreach (var prop in customProps)
                    {
                        auditEvent.Properties[prop.Key] = prop.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                auditEvent.Properties["PropertiesParseError"] = ex.Message;
            }
        }

        if (exception != null)
        {
            auditEvent.Properties["Exception"] = exception.GetType().Name;
            auditEvent.Properties["ExceptionMessage"] = exception.Message;
        }

        return auditEvent;
    }

    /// <summary>
    /// Creates a security audit event
    /// </summary>
    private static SecurityAuditEvent CreateSecurityAuditEvent(
        IInvocation invocation,
        SecurityAuditAttribute attribute,
        long durationMs,
        object? result,
        Exception? exception)
    {
        var method = invocation.Method;
        var targetType = invocation.TargetType ?? method.DeclaringType!;

        var action = attribute.Action ?? method.Name;
        var resource = attribute.Resource ?? targetType.Name;

        var securityEvent = SecurityAuditEvent.Create(attribute.EventType, action, resource);

        securityEvent.Outcome = exception == null ? SecurityOutcome.Success : SecurityOutcome.Failed;
        securityEvent.RiskScore = attribute.RiskScore;
        securityEvent.IsAlert = attribute.GenerateAlert || attribute.RiskScore >= 75;
        securityEvent.DurationMs = durationMs;
        securityEvent.Details = attribute.Details ?? (exception?.Message);
        securityEvent.Tags = attribute.Tags?.ToList() ?? new List<string>();

        securityEvent.Properties["MethodName"] = method.Name;
        securityEvent.Properties["ClassName"] = targetType.Name;

        if (!string.IsNullOrEmpty(attribute.RequiredPermission))
        {
            securityEvent.Properties["RequiredPermission"] = attribute.RequiredPermission;
        }

        if (!string.IsNullOrEmpty(attribute.RequiredRole))
        {
            securityEvent.Properties["RequiredRole"] = attribute.RequiredRole;
        }

        if (exception != null)
        {
            securityEvent.Properties["Exception"] = exception.GetType().Name;
            securityEvent.Properties["ExceptionMessage"] = exception.Message;
        }

        return securityEvent;
    }

    /// <summary>
    /// Gets safe parameter values for auditing
    /// </summary>
    private static Dictionary<string, object?> GetSafeParameters(MethodInfo method, object[] arguments)
    {
        var parameters = method.GetParameters();
        var result = new Dictionary<string, object?>();

        for (int i = 0; i < parameters.Length && i < arguments.Length; i++)
        {
            var param = parameters[i];
            var value = arguments[i];

            // Check if parameter has sensitive data attribute
            var sensitiveAttr = param.GetCustomAttribute<SensitiveDataAttribute>();
            if (sensitiveAttr != null)
            {
                if (sensitiveAttr.ExcludeFromAudit)
                {
                    continue;
                }

                result[param.Name ?? $"arg{i}"] = MaskSensitiveData(value?.ToString(), sensitiveAttr.MaskingStrategy);
            }
            else
            {
                result[param.Name ?? $"arg{i}"] = GetSafeValue(value);
            }
        }

        return result;
    }

    /// <summary>
    /// Gets safe return value for auditing
    /// </summary>
    private static object? GetSafeReturnValue(object value)
    {
        return GetSafeValue(value);
    }

    /// <summary>
    /// Gets safe value for auditing (handles complex objects)
    /// </summary>
    private static object? GetSafeValue(object? value)
    {
        if (value == null) return null;

        var type = value.GetType();

        // Handle primitive types
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || 
            type == typeof(Guid) || type == typeof(decimal))
        {
            return value;
        }

        // Handle collections
        if (value is System.Collections.IEnumerable enumerable and not string)
        {
            return $"Collection({enumerable.Cast<object>().Count()} items)";
        }

        // For complex objects, return type name
        return $"Object({type.Name})";
    }

    /// <summary>
    /// Masks sensitive data according to the specified strategy
    /// </summary>
    private static string MaskSensitiveData(string? value, SensitiveDataMaskingStrategy strategy)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return strategy switch
        {
            SensitiveDataMaskingStrategy.FullMask => new string('*', value.Length),
            SensitiveDataMaskingStrategy.PartialMask => value.Length <= 4 
                ? new string('*', value.Length)
                : $"{value[..2]}{"".PadLeft(value.Length - 4, '*')}{value[^2..]}",
            SensitiveDataMaskingStrategy.ShowFirst => value.Length <= 3 
                ? new string('*', value.Length)
                : $"{value[..3]}{"".PadLeft(value.Length - 3, '*')}",
            SensitiveDataMaskingStrategy.ShowLast => value.Length <= 3 
                ? new string('*', value.Length)
                : $"{"".PadLeft(value.Length - 3, '*')}{value[^3..]}",
            SensitiveDataMaskingStrategy.Placeholder => "[REDACTED]",
            SensitiveDataMaskingStrategy.Hash => Convert.ToBase64String(
                SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)))[..8] + "...",
            _ => new string('*', value.Length)
        };
    }

    /// <summary>
    /// Checks if method has NoAudit attribute
    /// </summary>
    private static bool HasNoAuditAttribute(MemberInfo member)
    {
        return member.GetCustomAttribute<NoAuditAttribute>() != null;
    }

    /// <summary>
    /// Gets audit attribute from method or class
    /// </summary>
    private static AuditAttribute? GetAuditAttribute(MemberInfo member)
    {
        return member.GetCustomAttribute<AuditAttribute>();
    }

    /// <summary>
    /// Gets security audit attribute from method or class
    /// </summary>
    private static SecurityAuditAttribute? GetSecurityAuditAttribute(MemberInfo member)
    {
        return member.GetCustomAttribute<SecurityAuditAttribute>();
    }
}