namespace Enterprise.Shared.Resilience.Models;

public class RetryPolicy
{
    public int MaxAttempts { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromMinutes(1);
    public string BackoffType { get; set; } = "Exponential";
    public bool UseJitter { get; set; } = true;
    public Func<Exception, bool>? ShouldRetry { get; set; }
}