using Microsoft.Extensions.Configuration;

namespace EgitimPlatform.Shared.Configuration.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly IConfigurationRoot? _configurationRoot;
    
    public ConfigurationService(IConfiguration configuration)
    {
        _configuration = configuration;
        _configurationRoot = configuration as IConfigurationRoot;
    }
    
    public T GetOptions<T>(string sectionName) where T : new()
    {
        var options = new T();
        _configuration.GetSection(sectionName).Bind(options);
        return options;
    }
    
    public string GetConnectionString(string name)
    {
        return _configuration.GetConnectionString(name) ?? string.Empty;
    }
    
    public bool IsFeatureEnabled(string featureName)
    {
        return _configuration.GetValue<bool>($"Features:{featureName}");
    }
    
    public void ReloadConfiguration()
    {
        _configurationRoot?.Reload();
    }
}