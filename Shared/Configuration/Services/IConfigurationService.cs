namespace EgitimPlatform.Shared.Configuration.Services;

public interface IConfigurationService
{
    T GetOptions<T>(string sectionName) where T : new();
    string GetConnectionString(string name);
    bool IsFeatureEnabled(string featureName);
    void ReloadConfiguration();
}