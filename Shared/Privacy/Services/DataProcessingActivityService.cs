using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EgitimPlatform.Shared.Privacy.Configuration;
using EgitimPlatform.Shared.Privacy.Enums;
using EgitimPlatform.Shared.Privacy.Models;

namespace EgitimPlatform.Shared.Privacy.Services;

public class DataProcessingActivityService : IDataProcessingActivityService
{
    private readonly ILogger<DataProcessingActivityService> _logger;
    private readonly PrivacyOptions _options;
    private readonly List<DataProcessingActivity> _activities; // In-memory storage for demo

    public DataProcessingActivityService(ILogger<DataProcessingActivityService> logger, IOptions<PrivacyOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _activities = new List<DataProcessingActivity>();
    }

    public async Task<DataProcessingActivity> CreateActivityAsync(string name, string description, string controller,
        DataProcessingLawfulBasis lawfulBasis, List<PersonalDataCategory> dataCategories,
        List<string> processingPurposes, List<string> dataSubjectCategories)
    {
        var activity = new DataProcessingActivity
        {
            Name = name,
            Description = description,
            Controller = controller,
            LawfulBasis = lawfulBasis,
            DataCategories = dataCategories,
            ProcessingPurposes = processingPurposes,
            DataSubjectCategories = dataSubjectCategories,
            RequiresDataProtectionImpactAssessment = DetermineIfDPIARequired(dataCategories, processingPurposes)
        };

        _activities.Add(activity);

        _logger.LogInformation("Data processing activity created: {ActivityId} - {Name}", activity.Id, name);

        return await Task.FromResult(activity);
    }

    public async Task<DataProcessingActivity?> GetActivityAsync(string activityId)
    {
        var activity = _activities.FirstOrDefault(a => a.Id == activityId);
        return await Task.FromResult(activity);
    }

    public async Task<List<DataProcessingActivity>> GetAllActivitiesAsync()
    {
        return await Task.FromResult(_activities.ToList());
    }

    public async Task<List<ProcessingActivitySummary>> GetActivitySummariesAsync()
    {
        var summaries = _activities.Select(a => new ProcessingActivitySummary
        {
            Id = a.Id,
            Name = a.Name,
            LawfulBasis = a.LawfulBasis,
            DataCategoriesCount = a.DataCategories.Count,
            DataSubjectsCount = a.DataSubjectCategories.Count,
            RequiresDPIA = a.RequiresDataProtectionImpactAssessment,
            IsActive = a.IsActive,
            LastUpdated = a.UpdatedAt ?? a.CreatedAt
        }).ToList();

        return await Task.FromResult(summaries);
    }

    public async Task<DataProcessingActivity> UpdateActivityAsync(string activityId, Dictionary<string, object> updates)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException("Activity not found");

        foreach (var update in updates)
        {
            var property = typeof(DataProcessingActivity).GetProperty(update.Key);
            if (property != null && property.CanWrite)
            {
                property.SetValue(activity, update.Value);
            }
        }

        activity.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Data processing activity {ActivityId} updated", activityId);

        return activity;
    }

    public async Task<bool> DeactivateActivityAsync(string activityId)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            return false;

        activity.IsActive = false;
        activity.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Data processing activity {ActivityId} deactivated", activityId);

        return true;
    }

    public async Task<bool> ActivateActivityAsync(string activityId)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            return false;

        activity.IsActive = true;
        activity.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Data processing activity {ActivityId} activated", activityId);

        return true;
    }

    public async Task<List<DataProcessingActivity>> GetActivitiesByLawfulBasisAsync(DataProcessingLawfulBasis lawfulBasis)
    {
        var activities = _activities.Where(a => a.LawfulBasis == lawfulBasis).ToList();
        return await Task.FromResult(activities);
    }

    public async Task<List<DataProcessingActivity>> GetActivitiesByDataCategoryAsync(PersonalDataCategory dataCategory)
    {
        var activities = _activities.Where(a => a.DataCategories.Contains(dataCategory)).ToList();
        return await Task.FromResult(activities);
    }

    public async Task<List<DataProcessingActivity>> GetActivitiesRequiringDPIAAsync()
    {
        var activities = _activities.Where(a => a.RequiresDataProtectionImpactAssessment).ToList();
        return await Task.FromResult(activities);
    }

    public async Task<DataProcessingActivity> AddThirdCountryTransferAsync(string activityId, string country, string safeguards)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException("Activity not found");

        if (!activity.ThirdCountryTransfers.Contains(country))
        {
            activity.ThirdCountryTransfers.Add(country);
            activity.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Third country transfer added to activity {ActivityId}: {Country}", activityId, country);
        }

        return activity;
    }

    public async Task<DataProcessingActivity> RemoveThirdCountryTransferAsync(string activityId, string country)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException("Activity not found");

        if (activity.ThirdCountryTransfers.Remove(country))
        {
            activity.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Third country transfer removed from activity {ActivityId}: {Country}", activityId, country);
        }

        return activity;
    }

    public async Task<DataProcessingActivity> UpdateRetentionPeriodAsync(string activityId, PersonalDataCategory dataCategory, int retentionDays)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException("Activity not found");

        activity.RetentionPeriods[dataCategory] = retentionDays;
        activity.UpdatedAt = DateTime.UtcNow;

        _logger.LogInformation("Retention period updated for activity {ActivityId}, category {Category}: {Days} days",
            activityId, dataCategory, retentionDays);

        return activity;
    }

    public async Task<DataProcessingActivity> AddRecipientAsync(string activityId, string recipient)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException("Activity not found");

        if (!activity.Recipients.Contains(recipient))
        {
            activity.Recipients.Add(recipient);
            activity.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Recipient added to activity {ActivityId}: {Recipient}", activityId, recipient);
        }

        return activity;
    }

    public async Task<DataProcessingActivity> RemoveRecipientAsync(string activityId, string recipient)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            throw new InvalidOperationException("Activity not found");

        if (activity.Recipients.Remove(recipient))
        {
            activity.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Recipient removed from activity {ActivityId}: {Recipient}", activityId, recipient);
        }

        return activity;
    }

    public async Task<Dictionary<DataProcessingLawfulBasis, int>> GetActivitiesCountByLawfulBasisAsync()
    {
        var counts = _activities
            .GroupBy(a => a.LawfulBasis)
            .ToDictionary(g => g.Key, g => g.Count());

        return await Task.FromResult(counts);
    }

    public async Task<Dictionary<PersonalDataCategory, int>> GetActivitiesCountByDataCategoryAsync()
    {
        var counts = new Dictionary<PersonalDataCategory, int>();

        foreach (var activity in _activities)
        {
            foreach (var category in activity.DataCategories)
            {
                counts[category] = counts.GetValueOrDefault(category, 0) + 1;
            }
        }

        return await Task.FromResult(counts);
    }

    public async Task<bool> ValidateActivityComplianceAsync(string activityId)
    {
        var activity = await GetActivityAsync(activityId);
        if (activity == null)
            return false;

        var issues = new List<string>();

        // Check if lawful basis is appropriate for data categories
        if (activity.DataCategories.Contains(PersonalDataCategory.SensitivePersonalData) &&
            activity.LawfulBasis != DataProcessingLawfulBasis.Consent &&
            activity.LawfulBasis != DataProcessingLawfulBasis.LegalObligation)
        {
            issues.Add("Sensitive personal data requires explicit consent or legal obligation");
        }

        // Check if DPIA is completed when required
        if (activity.RequiresDataProtectionImpactAssessment && 
            string.IsNullOrEmpty(activity.DataProtectionImpactAssessmentUrl))
        {
            issues.Add("DPIA is required but not completed");
        }

        // Check retention periods are set
        if (activity.DataCategories.Any() && !activity.RetentionPeriods.Any())
        {
            issues.Add("Retention periods not defined for data categories");
        }

        // Check third country transfers have safeguards
        if (activity.ThirdCountryTransfers.Any())
        {
            issues.Add("Third country transfers require adequate safeguards documentation");
        }

        foreach (var issue in issues)
        {
            _logger.LogWarning("Compliance issue for activity {ActivityId}: {Issue}", activityId, issue);
        }

        return !issues.Any();
    }

    private static bool DetermineIfDPIARequired(List<PersonalDataCategory> dataCategories, List<string> processingPurposes)
    {
        // DPIA is required for high-risk processing
        if (dataCategories.Contains(PersonalDataCategory.SensitivePersonalData) ||
            dataCategories.Contains(PersonalDataCategory.BiometricData) ||
            dataCategories.Contains(PersonalDataCategory.HealthInformation))
        {
            return true;
        }

        // Check for systematic monitoring or large scale processing
        var highRiskPurposes = new[] { "profiling", "automated decision making", "systematic monitoring", "large scale processing" };
        if (processingPurposes.Any(p => highRiskPurposes.Any(hrp => p.ToLowerInvariant().Contains(hrp))))
        {
            return true;
        }

        return false;
    }
}