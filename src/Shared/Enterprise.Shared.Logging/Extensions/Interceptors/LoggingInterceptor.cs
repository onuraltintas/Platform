using Enterprise.Shared.Logging.Extensions.Attributes;
using Enterprise.Shared.Logging.Interfaces;

namespace Enterprise.Shared.Logging.Extensions.Interceptors;

/// <summary>
/// Castle DynamicProxy interceptor for automatic logging
/// </summary>
public class LoggingInterceptor : IInterceptor
{
    private readonly IEnterpriseLoggerFactory _loggerFactory;

    public LoggingInterceptor(IEnterpriseLoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    public void Intercept(IInvocation invocation)
    {
        var method = invocation.Method;
        var targetType = invocation.TargetType;
        var logger = _loggerFactory.CreateLogger(targetType.Name);

        // Check for logging attributes
        var performanceAttribute = method.GetCustomAttribute<LogPerformanceAttribute>() ??
                                 targetType.GetCustomAttribute<LogPerformanceAttribute>();
        var businessEventAttribute = method.GetCustomAttribute<LogBusinessEventAttribute>();
        var securityEventAttribute = method.GetCustomAttribute<LogSecurityEventAttribute>();

        if (performanceAttribute == null && businessEventAttribute == null && securityEventAttribute == null)
        {
            invocation.Proceed();
            return;
        }

        var operationName = performanceAttribute?.OperationName ?? 
                           $"{targetType.Name}.{method.Name}";

        var properties = new Dictionary<string, object>
        {
            ["ClassName"] = targetType.Name,
            ["MethodName"] = method.Name,
            ["Assembly"] = targetType.Assembly.GetName().Name ?? "Unknown"
        };

        // Add parameters if requested
        if (performanceAttribute?.LogParameters == true || 
            businessEventAttribute?.IncludeParameters == true ||
            securityEventAttribute?.IncludeParameters == true)
        {
            AddMethodParameters(properties, invocation, method);
        }

        var stopwatch = Stopwatch.StartNew();
        Exception? thrownException = null;
        object? result = null;

        try
        {
            invocation.Proceed();
            result = invocation.ReturnValue;
        }
        catch (Exception ex)
        {
            thrownException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();

            // Add result if requested and available
            if (result != null && (performanceAttribute?.LogResult == true || 
                                 businessEventAttribute?.IncludeResult == true))
            {
                properties["Result"] = SerializeResult(result);
            }

            // Add exception information if occurred
            if (thrownException != null)
            {
                properties["Exception"] = thrownException.Message;
                properties["ExceptionType"] = thrownException.GetType().Name;
                properties["Success"] = false;

                if (performanceAttribute?.LogExceptions == true)
                {
                    logger.LogException(thrownException, operationName, properties);
                }
            }
            else
            {
                properties["Success"] = true;
            }

            // Log performance if attribute is present
            if (performanceAttribute != null && 
                stopwatch.Elapsed.TotalMilliseconds >= performanceAttribute.MinimumDurationMs)
            {
                logger.LogPerformance(operationName, stopwatch.Elapsed, properties);
            }

            // Log business event if attribute is present
            if (businessEventAttribute != null && thrownException == null)
            {
                logger.LogBusinessEvent(businessEventAttribute.EventName, properties);
            }

            // Log security event if attribute is present
            if (securityEventAttribute != null && 
                (securityEventAttribute.AlwaysLog || thrownException == null))
            {
                logger.LogSecurityEvent(securityEventAttribute.EventType, properties);
            }
        }
    }

    private static void AddMethodParameters(Dictionary<string, object> properties, IInvocation invocation, MethodInfo method)
    {
        if (invocation.Arguments.Length == 0) return;

        var parameters = method.GetParameters();
        var parameterData = new Dictionary<string, object>();

        for (int i = 0; i < Math.Min(parameters.Length, invocation.Arguments.Length); i++)
        {
            var paramName = parameters[i].Name ?? $"param{i}";
            var paramValue = invocation.Arguments[i];
            
            // Serialize parameter value safely
            parameterData[paramName] = SerializeParameter(paramValue);
        }

        if (parameterData.Count > 0)
        {
            properties["Parameters"] = parameterData;
        }
    }

    private static object SerializeParameter(object? value)
    {
        if (value == null) return "null";
        
        var type = value.GetType();
        
        // Handle primitive types and strings
        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || 
            type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid))
        {
            return value;
        }

        // Handle enums
        if (type.IsEnum)
        {
            return value.ToString() ?? "null";
        }

        // Handle collections (limit size to prevent huge logs)
        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var items = enumerable.Cast<object>().Take(10).ToList();
            return items.Count > 0 ? items : "empty_collection";
        }

        // For complex objects, just return the type name to avoid serialization issues
        return $"[{type.Name}]";
    }

    private static object SerializeResult(object? result)
    {
        return SerializeParameter(result);
    }
}