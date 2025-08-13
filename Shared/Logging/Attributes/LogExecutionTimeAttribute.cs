using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using EgitimPlatform.Shared.Logging.Services;

namespace EgitimPlatform.Shared.Logging.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class LogExecutionTimeAttribute : Attribute, IAsyncActionFilter
{
    private readonly bool _logArguments;
    private readonly bool _logResult;
    
    public LogExecutionTimeAttribute(bool logArguments = false, bool logResult = false)
    {
        _logArguments = logArguments;
        _logResult = logResult;
    }
    
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<IStructuredLogger>();
        var stopwatch = Stopwatch.StartNew();
        
        var actionName = $"{context.Controller.GetType().Name}.{context.ActionDescriptor.DisplayName}";
        
        var contextLogger = logger.ForContext("ActionName", actionName)
                                 .ForContext("TraceId", Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);
        
        if (_logArguments && context.ActionArguments.Any())
        {
            contextLogger = contextLogger.ForContext("Arguments", context.ActionArguments);
        }
        
        contextLogger.LogInformation("Action {ActionName} started", actionName);
        
        var executedContext = await next();
        
        stopwatch.Stop();
        
        var logData = new
        {
            ActionName = actionName,
            Duration = stopwatch.ElapsedMilliseconds,
            Success = executedContext.Exception == null,
            StatusCode = context.HttpContext.Response.StatusCode
        };
        
        if (_logResult && executedContext.Result != null)
        {
            contextLogger = contextLogger.ForContext("Result", executedContext.Result);
        }
        
        if (executedContext.Exception != null)
        {
            contextLogger.LogError(executedContext.Exception, 
                "Action {ActionName} failed after {Duration}ms", 
                actionName, stopwatch.ElapsedMilliseconds);
        }
        else
        {
            contextLogger.LogPerformance(actionName, stopwatch.Elapsed, logData);
        }
    }
}