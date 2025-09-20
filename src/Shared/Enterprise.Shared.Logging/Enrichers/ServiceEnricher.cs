using Serilog.Core;
using Serilog.Events;

namespace Enterprise.Shared.Logging.Enrichers;

/// <summary>
/// Serilog enricher that adds service information to log events
/// </summary>
public class ServiceEnricher : ILogEventEnricher
{
    private readonly string _serviceName;
    private readonly string _serviceVersion;
    private readonly string _environment;
    private readonly string _machineName;

    public ServiceEnricher(string serviceName, string serviceVersion, string environment)
    {
        _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
        _serviceVersion = serviceVersion ?? throw new ArgumentNullException(nameof(serviceVersion));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _machineName = Environment.MachineName;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ServiceName", _serviceName));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ServiceVersion", _serviceVersion));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("Environment", _environment));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("MachineName", _machineName));
        
        // Add process information
        var process = Process.GetCurrentProcess();
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ProcessId", process.Id));
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ProcessName", process.ProcessName));
        
        // Add thread information
        logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("ThreadId", Thread.CurrentThread.ManagedThreadId));
    }
}