using System.Security.Cryptography;
using System.Text;
using Enterprise.Shared.Observability.Interfaces;
using Enterprise.Shared.Observability.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Enterprise.Shared.Observability.Services;

public class BusinessMetricsCollector : IBusinessMetricsCollector
{
    private readonly IMetricsService _metricsService;
    private readonly IBusinessMetricsRepository? _repository;
    private readonly ILogger<BusinessMetricsCollector> _logger;
    private readonly ObservabilitySettings _settings;

    public BusinessMetricsCollector(
        IMetricsService metricsService,
        ILogger<BusinessMetricsCollector> logger,
        IOptions<ObservabilitySettings> settings,
        IBusinessMetricsRepository? repository = null)
    {
        _metricsService = metricsService ?? throw new ArgumentNullException(nameof(metricsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _repository = repository;
    }

    public async Task RecordUserRegistrationAsync(string userId, string registrationSource, 
        Dictionary<string, object>? metadata = null)
    {
        if (!_settings.BusinessMetrics.EnableUserMetrics)
            return;

        try
        {
            // Increment Prometheus counter
            _metricsService.IncrementCounter("user_registrations_total", 1,
                new KeyValuePair<string, object>("source", registrationSource));

            // Store detailed data for analytics
            if (_repository != null)
            {
                var metricData = new BusinessMetricData
                {
                    MetricName = "UserRegistration",
                    UserId = userId,
                    Timestamp = DateTime.UtcNow,
                    Value = 1,
                    Dimensions = new Dictionary<string, object>
                    {
                        ["source"] = registrationSource,
                        ["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                };

                if (metadata != null)
                {
                    foreach (var kvp in metadata)
                    {
                        metricData.Dimensions[kvp.Key] = kvp.Value;
                    }
                }

                await _repository.StoreMetricAsync(metricData);
            }

            _logger.LogInformation("Recorded user registration metric for user {HashedUserId} from {Source}", 
                HashUserId(userId), registrationSource);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record user registration metric for user {HashedUserId}", HashUserId(userId));
        }
    }

    public async Task RecordUserLoginAsync(string userId, bool success, string? failureReason = null)
    {
        if (!_settings.BusinessMetrics.EnableUserMetrics)
            return;

        try
        {
            var tags = new List<KeyValuePair<string, object>>
            {
                new("success", success.ToString())
            };

            if (!success && !string.IsNullOrEmpty(failureReason))
            {
                tags.Add(new("failure_reason", failureReason));
            }

            _metricsService.IncrementCounter("user_logins_total", 1, tags.ToArray());

            if (_repository != null)
            {
                var metricData = new BusinessMetricData
                {
                    MetricName = "UserLogin",
                    UserId = userId,
                    Value = 1,
                    Dimensions = new Dictionary<string, object>
                    {
                        ["success"] = success,
                        ["timestamp"] = DateTime.UtcNow
                    }
                };

                if (!string.IsNullOrEmpty(failureReason))
                {
                    metricData.Dimensions["failure_reason"] = failureReason;
                }

                await _repository.StoreMetricAsync(metricData);
            }

            _logger.LogInformation("Recorded user login metric for user {HashedUserId} (Success: {Success})", 
                HashUserId(userId), success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record user login metric for user {HashedUserId}", HashUserId(userId));
        }
    }

    public async Task RecordOrderCreatedAsync(string orderId, decimal amount, string currency, string userId)
    {
        if (!_settings.BusinessMetrics.EnableOrderMetrics)
            return;

        try
        {
            _metricsService.IncrementCounter("orders_created_total", 1,
                new KeyValuePair<string, object>("currency", currency));

            _metricsService.RecordHistogram("order_amount", (double)amount,
                new KeyValuePair<string, object>("currency", currency));

            if (_repository != null)
            {
                var metricData = new BusinessMetricData
                {
                    MetricName = "OrderCreated",
                    UserId = userId,
                    Value = (double)amount,
                    Dimensions = new Dictionary<string, object>
                    {
                        ["order_id"] = orderId,
                        ["currency"] = currency,
                        ["timestamp"] = DateTime.UtcNow
                    }
                };

                await _repository.StoreMetricAsync(metricData);
            }

            _logger.LogInformation("Recorded order created metric: OrderId={OrderId}, Amount={Amount} {Currency}", 
                orderId, amount, currency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record order created metric for order {OrderId}", orderId);
        }
    }

    public async Task RecordPaymentProcessedAsync(string paymentId, decimal amount, string paymentMethod, bool success)
    {
        if (!_settings.BusinessMetrics.EnablePaymentMetrics)
            return;

        try
        {
            _metricsService.IncrementCounter("payments_processed_total", 1,
                new KeyValuePair<string, object>("method", paymentMethod),
                new KeyValuePair<string, object>("success", success.ToString()));

            if (success)
            {
                _metricsService.RecordHistogram("payment_amount", (double)amount,
                    new KeyValuePair<string, object>("method", paymentMethod));
            }

            if (_repository != null)
            {
                var metricData = new BusinessMetricData
                {
                    MetricName = "PaymentProcessed",
                    Value = (double)amount,
                    Dimensions = new Dictionary<string, object>
                    {
                        ["payment_id"] = paymentId,
                        ["method"] = paymentMethod,
                        ["success"] = success,
                        ["timestamp"] = DateTime.UtcNow
                    }
                };

                await _repository.StoreMetricAsync(metricData);
            }

            _logger.LogInformation("Recorded payment processed metric: PaymentId={PaymentId}, Amount={Amount}, Success={Success}", 
                paymentId, amount, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record payment processed metric for payment {PaymentId}", paymentId);
        }
    }

    public async Task RecordFeatureUsageAsync(string featureName, string userId, Dictionary<string, object>? context = null)
    {
        if (!_settings.BusinessMetrics.EnableFeatureUsageMetrics)
            return;

        try
        {
            _metricsService.IncrementCounter("feature_usage_total", 1,
                new KeyValuePair<string, object>("feature", featureName));

            if (_repository != null)
            {
                var metricData = new BusinessMetricData
                {
                    MetricName = "FeatureUsage",
                    UserId = userId,
                    Value = 1,
                    Dimensions = new Dictionary<string, object>
                    {
                        ["feature"] = featureName,
                        ["timestamp"] = DateTime.UtcNow
                    }
                };

                if (context != null)
                {
                    foreach (var kvp in context)
                    {
                        var sanitizedKey = SanitizeString(kvp.Key, 50);
                        var sanitizedValue = SanitizeValue(kvp.Value);
                        metricData.Metadata[sanitizedKey] = sanitizedValue;
                    }
                }

                await _repository.StoreMetricAsync(metricData);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record feature usage metric for feature {Feature}", featureName);
        }
    }

    public async Task RecordCustomEventAsync(string eventName, Dictionary<string, object> properties)
    {
        try
        {
            _metricsService.RecordBusinessMetric(eventName, 1, properties);

            if (_repository != null)
            {
                var metricData = new BusinessMetricData
                {
                    MetricName = eventName,
                    Value = 1,
                    Dimensions = properties,
                    Timestamp = DateTime.UtcNow
                };

                await _repository.StoreMetricAsync(metricData);
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record custom event {EventName}", eventName);
        }
    }

    public async Task<BusinessMetricsReport> GenerateReportAsync(DateTime from, DateTime to)
    {
        try
        {
            if (_repository == null)
            {
                _logger.LogWarning("Cannot generate report without repository");
                return new BusinessMetricsReport
                {
                    FromDate = from,
                    ToDate = to
                };
            }

            var report = new BusinessMetricsReport
            {
                FromDate = from,
                ToDate = to,
                TotalMetrics = new Dictionary<string, double>(),
                TimeSeries = new Dictionary<string, List<TimeSeriesDataPoint>>(),
                DimensionBreakdown = new Dictionary<string, Dictionary<string, double>>()
            };

            // Get aggregated metrics
            var metricTypes = new[] { "UserRegistration", "UserLogin", "OrderCreated", "PaymentProcessed", "FeatureUsage" };
            
            foreach (var metricType in metricTypes)
            {
                var metrics = await _repository.GetMetricsAsync(metricType, from, to);
                report.TotalMetrics[metricType] = metrics.Sum(m => m.Value);

                // Create time series
                var timeSeries = metrics
                    .GroupBy(m => m.Timestamp.Date)
                    .Select(g => new TimeSeriesDataPoint
                    {
                        Timestamp = g.Key,
                        Value = g.Sum(m => m.Value)
                    })
                    .OrderBy(p => p.Timestamp)
                    .ToList();

                report.TimeSeries[metricType] = timeSeries;

                // Dimension breakdown (example: by source for registrations)
                if (metricType == "UserRegistration")
                {
                    var breakdown = metrics
                        .Where(m => m.Dimensions.ContainsKey("source"))
                        .GroupBy(m => m.Dimensions["source"].ToString())
                        .ToDictionary(g => g.Key ?? "unknown", g => g.Sum(m => m.Value));
                    
                    report.DimensionBreakdown["RegistrationBySource"] = breakdown;
                }
            }

            _logger.LogInformation("Generated business metrics report from {From} to {To}", from, to);
            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate business metrics report");
            return new BusinessMetricsReport
            {
                FromDate = from,
                ToDate = to
            };
        }
    }
    
    private static string HashUserId(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return "anonymous";
            
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(hashBytes)[..8]; // Take first 8 chars for brevity
    }
    
    private static string SanitizeString(string? input, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "unknown";
            
        // Remove potentially dangerous characters and limit length
        var sanitized = new string(input.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.').ToArray());
        return sanitized.Length > maxLength ? sanitized[..maxLength] : sanitized;
    }
    
    private static object SanitizeValue(object? value)
    {
        return value switch
        {
            null => "null",
            string str => SanitizeString(str, 500),
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            bool b => b.ToString().ToLower(),
            _ => value.ToString() ?? "unknown"
        };
    }
}