namespace Enterprise.Shared.Auditing.Tests.TestHelpers;

public class FakeLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _logs = new();

    public IReadOnlyList<LogEntry> Logs => _logs.AsReadOnly();

    public IDisposable BeginScope<TState>(TState state) => new FakeScope();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logs.Add(new LogEntry
        {
            LogLevel = logLevel,
            EventId = eventId,
            Message = formatter(state, exception),
            Exception = exception,
            State = state
        });
    }

    public void Clear() => _logs.Clear();

    private class FakeScope : IDisposable
    {
        public void Dispose() { }
    }
}

public class LogEntry
{
    public LogLevel LogLevel { get; set; }
    public EventId EventId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public object? State { get; set; }
}