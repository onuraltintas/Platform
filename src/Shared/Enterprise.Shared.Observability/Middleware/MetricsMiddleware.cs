using System.Diagnostics;
using Enterprise.Shared.Observability.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Enterprise.Shared.Observability.Middleware;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricsMiddleware> _logger;

    public MetricsMiddleware(
        RequestDelegate next,
        ILogger<MetricsMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, IMetricsService metricsService)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? "/";
        
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            
            // Record API metrics
            metricsService.RecordApiCall(
                method,
                path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
            
            // Log slow requests
            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning(
                    "Slow request detected: {Method} {Path} took {ElapsedMs}ms with status {StatusCode}",
                    method, path, stopwatch.ElapsedMilliseconds, context.Response.StatusCode);
            }
        }
    }
}