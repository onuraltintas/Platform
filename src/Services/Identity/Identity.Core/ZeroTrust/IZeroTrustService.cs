using Identity.Core.Entities;

namespace Identity.Core.ZeroTrust;

/// <summary>
/// Zero Trust security architecture service interface
/// </summary>
public interface IZeroTrustService
{
    /// <summary>
    /// Evaluate trust score for a user context
    /// </summary>
    Task<TrustScore> EvaluateTrustScoreAsync(ZeroTrustContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate device identity and compliance
    /// </summary>
    Task<DeviceComplianceResult> ValidateDeviceAsync(DeviceInfo deviceInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assess network security posture
    /// </summary>
    Task<NetworkSecurityAssessment> AssessNetworkSecurityAsync(NetworkContext networkContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform behavioral analysis
    /// </summary>
    Task<BehaviorAnalysisResult> AnalyzeBehaviorAsync(UserBehaviorContext behaviorContext, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute adaptive authentication based on risk level
    /// </summary>
    Task<AuthenticationRequirement> GetAuthenticationRequirementAsync(string userId, ZeroTrustContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Perform continuous security monitoring
    /// </summary>
    Task<SecurityMonitoringResult> MonitorSecurityAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Apply least privilege access controls
    /// </summary>
    Task<AccessDecision> EvaluateAccessAsync(AccessRequest accessRequest, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate session integrity
    /// </summary>
    Task<SessionValidationResult> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update trust score based on new information
    /// </summary>
    Task UpdateTrustScoreAsync(string userId, TrustScoreUpdate update, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get security recommendations
    /// </summary>
    Task<List<SecurityRecommendation>> GetSecurityRecommendationsAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Zero Trust context information
/// </summary>
public class ZeroTrustContext
{
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public DeviceInfo Device { get; set; } = new();
    public NetworkContext Network { get; set; } = new();
    public UserBehaviorContext Behavior { get; set; } = new();
    public List<string> RequestedPermissions { get; set; } = new();
    public string ResourceId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Device information
/// </summary>
public class DeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // Mobile, Desktop, Tablet, etc.
    public string OperatingSystem { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsTrusted { get; set; }
    public bool IsManaged { get; set; }
    public bool IsCompliant { get; set; }
    public DateTime LastSeen { get; set; }
    public string? CertificateFingerprint { get; set; }
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// Network context information
/// </summary>
public class NetworkContext
{
    public string IpAddress { get; set; } = string.Empty;
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public bool IsVpn { get; set; }
    public bool IsTor { get; set; }
    public bool IsKnownMalicious { get; set; }
    public string NetworkType { get; set; } = string.Empty; // Corporate, Public, Home
    public double RiskScore { get; set; }
    public List<string> ThreatCategories { get; set; } = new();
}

/// <summary>
/// User behavior context
/// </summary>
public class UserBehaviorContext
{
    public TimeSpan TypicalLoginTime { get; set; }
    public List<string> TypicalLocations { get; set; } = new();
    public List<string> TypicalDevices { get; set; } = new();
    public double LoginFrequency { get; set; }
    public List<string> RecentActivities { get; set; } = new();
    public bool IsAnomalousPattern { get; set; }
    public double BehaviorScore { get; set; }
}

/// <summary>
/// Trust score evaluation result
/// </summary>
public class TrustScore
{
    public double Score { get; set; } // 0-100
    public TrustLevel Level { get; set; }
    public List<TrustFactor> Factors { get; set; } = new();
    public List<string> Risks { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ValidFor { get; set; } = TimeSpan.FromMinutes(15);
}

/// <summary>
/// Trust level enumeration
/// </summary>
public enum TrustLevel
{
    None = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Maximum = 4
}

/// <summary>
/// Trust factor contributing to overall score
/// </summary>
public class TrustFactor
{
    public string Name { get; set; } = string.Empty;
    public double Weight { get; set; }
    public double Score { get; set; }
    public string Description { get; set; } = string.Empty;
    public FactorType Type { get; set; }
}

/// <summary>
/// Factor type enumeration
/// </summary>
public enum FactorType
{
    Device,
    Network,
    Behavior,
    Authentication,
    Location,
    Time,
    Risk
}

/// <summary>
/// Device compliance validation result
/// </summary>
public class DeviceComplianceResult
{
    public bool IsCompliant { get; set; }
    public List<ComplianceViolation> Violations { get; set; } = new();
    public Dictionary<string, bool> PolicyChecks { get; set; } = new();
    public double ComplianceScore { get; set; }
    public DateTime LastChecked { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Compliance violation information
/// </summary>
public class ComplianceViolation
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // Low, Medium, High, Critical
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Network security assessment result
/// </summary>
public class NetworkSecurityAssessment
{
    public double SecurityScore { get; set; }
    public List<NetworkThreat> Threats { get; set; } = new();
    public bool IsSecureNetwork { get; set; }
    public List<string> SecurityMeasures { get; set; } = new();
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Network threat information
/// </summary>
public class NetworkThreat
{
    public string ThreatType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string? MitigationAction { get; set; }
}

/// <summary>
/// Behavior analysis result
/// </summary>
public class BehaviorAnalysisResult
{
    public double AnomalyScore { get; set; }
    public List<BehaviorAnomaly> Anomalies { get; set; } = new();
    public bool IsNormalBehavior { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Behavior anomaly information
/// </summary>
public class BehaviorAnomaly
{
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Severity { get; set; }
    public string Context { get; set; } = string.Empty;
}

/// <summary>
/// Authentication requirement based on risk assessment
/// </summary>
public class AuthenticationRequirement
{
    public AuthenticationLevel RequiredLevel { get; set; }
    public List<string> RequiredFactors { get; set; } = new();
    public bool RequiresAdditionalVerification { get; set; }
    public TimeSpan SessionDuration { get; set; }
    public List<string> Restrictions { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Authentication level enumeration
/// </summary>
public enum AuthenticationLevel
{
    Basic = 1,
    TwoFactor = 2,
    Multi = 3,
    Enhanced = 4,
    Maximum = 5
}

/// <summary>
/// Security monitoring result
/// </summary>
public class SecurityMonitoringResult
{
    public string SessionId { get; set; } = string.Empty;
    public bool IsSecure { get; set; }
    public List<SecurityAlert> Alerts { get; set; } = new();
    public double RiskScore { get; set; }
    public DateTime MonitoredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Security alert information
/// </summary>
public class SecurityAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Access request information
/// </summary>
public class AccessRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public ZeroTrustContext Context { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Access decision result
/// </summary>
public class AccessDecision
{
    public bool IsAllowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public List<string> Conditions { get; set; } = new();
    public TimeSpan AccessDuration { get; set; }
    public List<string> RequiredActions { get; set; } = new();
    public double ConfidenceScore { get; set; }
}

/// <summary>
/// Session validation result
/// </summary>
public class SessionValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationFailures { get; set; } = new();
    public double IntegrityScore { get; set; }
    public DateTime LastValidated { get; set; } = DateTime.UtcNow;
    public bool RequiresReauthentication { get; set; }
}

/// <summary>
/// Trust score update information
/// </summary>
public class TrustScoreUpdate
{
    public string EventType { get; set; } = string.Empty;
    public double ScoreAdjustment { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Dictionary<string, object> EventData { get; set; } = new();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Security recommendation
/// </summary>
public class SecurityRecommendation
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public List<string> Actions { get; set; } = new();
    public string? Impact { get; set; }
}