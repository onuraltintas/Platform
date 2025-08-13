using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using EgitimPlatform.Shared.Observability.Metrics;
using EgitimPlatform.Shared.Observability.Tracing;

namespace EgitimPlatform.Shared.Observability.Middleware;

public class MetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<MetricsMiddleware> _logger;
    private readonly ApplicationMetrics _metrics;

    public MetricsMiddleware(RequestDelegate next, ILogger<MetricsMiddleware> logger, ApplicationMetrics metrics)
    {
        _next = next;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestSize = GetRequestSize(context);
        var startTime = DateTime.UtcNow;

        // Track active connections
        _metrics.IncrementActiveConnections();

        try
        {
            // Track request size
            if (requestSize > 0)
            {
                _metrics.RecordHttpRequestSize(requestSize, context.Request.Method, GetEndpoint(context));
            }

            await _next(context);

            var duration = stopwatch.Elapsed.TotalSeconds;
            var statusCode = context.Response.StatusCode;
            var method = context.Request.Method;
            var endpoint = GetEndpoint(context);
            var responseSize = GetResponseSize(context);

            // Record metrics
            _metrics.IncrementHttpRequests(method, endpoint, statusCode);
            _metrics.RecordHttpRequestDuration(duration, method, endpoint, statusCode);

            if (responseSize > 0)
            {
                _metrics.RecordHttpResponseSize(responseSize, method, endpoint, statusCode);
            }

            // Track errors
            if (statusCode >= 400)
            {
                var errorType = GetErrorType(statusCode);
                _metrics.IncrementHttpErrors(method, endpoint, statusCode, errorType);
            }

            // Update Prometheus metrics
            PrometheusMetrics.HttpRequestsTotal
                .WithLabels(method, endpoint, statusCode.ToString())
                .Inc();

            PrometheusMetrics.HttpRequestDuration
                .WithLabels(method, endpoint, statusCode.ToString())
                .Observe(duration);

            if (requestSize > 0)
            {
                PrometheusMetrics.HttpRequestSizeBytes
                    .WithLabels(method, endpoint)
                    .Observe(requestSize);
            }

            if (responseSize > 0)
            {
                PrometheusMetrics.HttpResponseSizeBytes
                    .WithLabels(method, endpoint, statusCode.ToString())
                    .Observe(responseSize);
            }

            if (statusCode >= 400)
            {
                var errorType = GetErrorType(statusCode);
                PrometheusMetrics.HttpErrorsTotal
                    .WithLabels(method, endpoint, statusCode.ToString(), errorType)
                    .Inc();
            }

            _logger.LogDebug("HTTP {Method} {Endpoint} responded {StatusCode} in {Duration}ms",
                method, endpoint, statusCode, duration * 1000);
        }
        catch (Exception ex)
        {
            var duration = stopwatch.Elapsed.TotalSeconds;
            var method = context.Request.Method;
            var endpoint = GetEndpoint(context);
            var statusCode = context.Response.StatusCode != 200 ? context.Response.StatusCode : 500;

            // Record error metrics
            _metrics.IncrementHttpErrors(method, endpoint, statusCode, "exception");
            _metrics.RecordHttpRequestDuration(duration, method, endpoint, statusCode);

            PrometheusMetrics.HttpErrorsTotal
                .WithLabels(method, endpoint, statusCode.ToString(), "exception")
                .Inc();

            PrometheusMetrics.HttpRequestDuration
                .WithLabels(method, endpoint, statusCode.ToString())
                .Observe(duration);

            _logger.LogError(ex, "HTTP {Method} {Endpoint} failed with exception after {Duration}ms",
                method, endpoint, duration * 1000);

            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.DecrementActiveConnections();
        }
    }

    private static string GetEndpoint(HttpContext context)
    {
        var endpoint = context.GetEndpoint()?.DisplayName;
        if (!string.IsNullOrEmpty(endpoint))
        {
            return endpoint;
        }

        var path = context.Request.Path.Value ?? "/";
        
        // Normalize path by removing IDs and other variable parts
        var normalizedPath = NormalizePath(path);
        return normalizedPath;
    }

    private static string NormalizePath(string path)
    {
        // Replace GUIDs and numeric IDs with placeholders
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < segments.Length; i++)
        {
            // Check if segment is a GUID
            if (Guid.TryParse(segments[i], out _))
            {
                segments[i] = "{id}";
            }
            // Check if segment is numeric
            else if (long.TryParse(segments[i], out _))
            {
                segments[i] = "{id}";
            }
        }

        return "/" + string.Join("/", segments);
    }

    private static long GetRequestSize(HttpContext context)
    {
        if (context.Request.ContentLength.HasValue)
        {
            return context.Request.ContentLength.Value;
        }

        return 0;
    }

    private static long GetResponseSize(HttpContext context)
    {
        if (context.Response.ContentLength.HasValue)
        {
            return context.Response.ContentLength.Value;
        }

        return 0;
    }

    private static string GetErrorType(int statusCode)
    {
        return statusCode switch
        {
            400 => "bad_request",
            401 => "unauthorized",
            403 => "forbidden",
            404 => "not_found",
            408 => "timeout",
            409 => "conflict",
            422 => "validation_error",
            429 => "rate_limit",
            >= 400 and < 500 => "client_error",
            500 => "internal_server_error",
            502 => "bad_gateway",
            503 => "service_unavailable",
            504 => "gateway_timeout",
            >= 500 => "server_error",
            _ => "unknown"
        };
    }
}