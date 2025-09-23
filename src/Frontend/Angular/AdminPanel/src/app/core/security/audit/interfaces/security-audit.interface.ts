/**
 * Enterprise Security Audit Interfaces
 * Comprehensive security auditing and penetration testing framework
 */

export interface SecurityAuditConfig {
  /** Audit execution mode */
  mode: AuditMode;
  /** Target environment */
  environment: AuditEnvironment;
  /** Audit scope and coverage */
  scope: AuditScope;
  /** Security standards to validate against */
  standards: SecurityStandard[];
  /** Audit execution settings */
  execution: AuditExecution;
  /** Reporting and notification settings */
  reporting: AuditReporting;
}

export type AuditMode = 'passive' | 'active' | 'aggressive' | 'stealth';
export type AuditEnvironment = 'development' | 'staging' | 'production' | 'testing';
export type SecurityStandard = 'OWASP' | 'ISO27001' | 'SOC2' | 'PCI_DSS' | 'GDPR' | 'NIST';

export interface AuditScope {
  /** Include frontend security checks */
  frontend: boolean;
  /** Include backend API security checks */
  backend: boolean;
  /** Include infrastructure security checks */
  infrastructure: boolean;
  /** Include authentication/authorization checks */
  authentication: boolean;
  /** Include data protection checks */
  dataProtection: boolean;
  /** Include network security checks */
  network: boolean;
  /** Specific URLs/endpoints to audit */
  targets: string[];
  /** URLs/endpoints to exclude from audit */
  exclusions: string[];
}

export interface AuditExecution {
  /** Maximum execution time in minutes */
  timeout: number;
  /** Concurrent scan threads */
  concurrency: number;
  /** Request delay between scans (ms) */
  delay: number;
  /** Maximum retries for failed tests */
  retries: number;
  /** Enable real-time monitoring during audit */
  realTimeMonitoring: boolean;
  /** Stop on first critical finding */
  stopOnCritical: boolean;
}

export interface AuditReporting {
  /** Generate detailed HTML report */
  htmlReport: boolean;
  /** Generate JSON report for automation */
  jsonReport: boolean;
  /** Generate PDF executive summary */
  pdfReport: boolean;
  /** Send email notifications */
  emailNotifications: string[];
  /** Slack webhook for alerts */
  slackWebhook?: string;
  /** Microsoft Teams webhook */
  teamsWebhook?: string;
  /** Include remediation recommendations */
  includeRemediation: boolean;
}

export interface SecurityAuditResult {
  /** Audit execution metadata */
  metadata: AuditMetadata;
  /** Overall security score (0-100) */
  securityScore: number;
  /** Risk level assessment */
  overallRiskLevel: SecurityRiskLevel;
  /** Categorized findings */
  findings: SecurityFinding[];
  /** Compliance assessment results */
  compliance: ComplianceResult[];
  /** Performance metrics */
  performance: AuditPerformanceMetrics;
  /** Remediation roadmap */
  remediation: RemediationPlan;
}

export interface AuditMetadata {
  /** Unique audit ID */
  auditId: string;
  /** Audit start timestamp */
  startTime: number;
  /** Audit end timestamp */
  endTime: number;
  /** Total execution duration (ms) */
  duration: number;
  /** Audit configuration used */
  config: SecurityAuditConfig;
  /** Auditor information */
  auditor: AuditorInfo;
  /** Target system information */
  target: TargetSystemInfo;
}

export interface AuditorInfo {
  /** Auditor name/identifier */
  name: string;
  /** Audit tool version */
  version: string;
  /** Execution environment */
  environment: string;
  /** User agent used for scanning */
  userAgent: string;
}

export interface TargetSystemInfo {
  /** Target hostname/URL */
  hostname: string;
  /** Detected technology stack */
  technologies: string[];
  /** Server response headers */
  serverHeaders: Record<string, string>;
  /** Security headers present */
  securityHeaders: string[];
  /** SSL/TLS information */
  ssl: SSLInfo;
}

export interface SSLInfo {
  /** SSL enabled */
  enabled: boolean;
  /** SSL certificate details */
  certificate?: {
    issuer: string;
    subject: string;
    validFrom: number;
    validTo: number;
    algorithm: string;
    keySize: number;
  };
  /** SSL configuration score */
  configScore: number;
  /** SSL vulnerabilities */
  vulnerabilities: string[];
}

export interface SecurityFinding {
  /** Unique finding ID */
  id: string;
  /** Finding category */
  category: FindingCategory;
  /** Severity level */
  severity: FindingSeverity;
  /** Risk level */
  riskLevel: SecurityRiskLevel;
  /** Finding title */
  title: string;
  /** Detailed description */
  description: string;
  /** Evidence and proof of concept */
  evidence: FindingEvidence;
  /** OWASP classification */
  owaspCategory?: OWASPCategory;
  /** CWE (Common Weakness Enumeration) ID */
  cweId?: number;
  /** CVE (Common Vulnerabilities and Exposures) ID */
  cveId?: string;
  /** CVSS (Common Vulnerability Scoring System) score */
  cvssScore?: number;
  /** Affected endpoints/components */
  affectedComponents: string[];
  /** Remediation steps */
  remediation: RemediationStep[];
  /** Discovery timestamp */
  discoveredAt: number;
  /** Verification status */
  verified: boolean;
}

export type FindingCategory =
  | 'authentication'
  | 'authorization'
  | 'injection'
  | 'xss'
  | 'csrf'
  | 'exposure'
  | 'cryptography'
  | 'configuration'
  | 'session'
  | 'input_validation'
  | 'business_logic'
  | 'security_headers'
  | 'infrastructure'
  | 'data_protection'
  | 'compliance';

export type FindingSeverity = 'info' | 'low' | 'medium' | 'high' | 'critical';
export type SecurityRiskLevel = 'none' | 'low' | 'medium' | 'high' | 'critical';

export interface FindingEvidence {
  /** HTTP request that triggered the finding */
  request?: {
    method: string;
    url: string;
    headers: Record<string, string>;
    body?: string;
  };
  /** HTTP response that contains the vulnerability */
  response?: {
    status: number;
    headers: Record<string, string>;
    body?: string;
  };
  /** Screenshots or visual evidence */
  screenshots?: string[];
  /** Code snippets showing vulnerability */
  codeSnippets?: string[];
  /** Additional technical details */
  technicalDetails: Record<string, any>;
}

export type OWASPCategory =
  | 'A01_BrokenAccessControl'
  | 'A02_CryptographicFailures'
  | 'A03_Injection'
  | 'A04_InsecureDesign'
  | 'A05_SecurityMisconfiguration'
  | 'A06_VulnerableComponents'
  | 'A07_IdentificationAuthFailures'
  | 'A08_SoftwareDataIntegrityFailures'
  | 'A09_SecurityLoggingMonitoringFailures'
  | 'A10_ServerSideRequestForgery';

export interface RemediationStep {
  /** Step sequence number */
  order: number;
  /** Action description */
  action: string;
  /** Technical implementation details */
  implementation: string;
  /** Priority level */
  priority: 'low' | 'medium' | 'high' | 'critical';
  /** Estimated effort (hours) */
  effort: number;
  /** Required skills/roles */
  requiredSkills: string[];
  /** Verification method */
  verification: string;
}

export interface ComplianceResult {
  /** Compliance standard */
  standard: SecurityStandard;
  /** Overall compliance score (0-100) */
  score: number;
  /** Compliance status */
  status: ComplianceStatus;
  /** Individual requirement results */
  requirements: ComplianceRequirement[];
  /** Non-compliance findings */
  gaps: ComplianceGap[];
}

export type ComplianceStatus = 'compliant' | 'partially_compliant' | 'non_compliant' | 'not_applicable';

export interface ComplianceRequirement {
  /** Requirement ID */
  id: string;
  /** Requirement description */
  description: string;
  /** Compliance status */
  status: ComplianceStatus;
  /** Evidence of compliance */
  evidence: string[];
  /** Associated security controls */
  controls: string[];
}

export interface ComplianceGap {
  /** Gap ID */
  id: string;
  /** Requirement that failed */
  requirement: string;
  /** Gap description */
  description: string;
  /** Impact assessment */
  impact: string;
  /** Remediation recommendations */
  remediation: string[];
}

export interface AuditPerformanceMetrics {
  /** Total tests executed */
  totalTests: number;
  /** Tests passed */
  testsPassed: number;
  /** Tests failed */
  testsFailed: number;
  /** Tests skipped */
  testsSkipped: number;
  /** Average response time (ms) */
  averageResponseTime: number;
  /** Total requests made */
  totalRequests: number;
  /** Requests per second */
  requestsPerSecond: number;
  /** Data transferred (bytes) */
  dataTransferred: number;
  /** Error rate percentage */
  errorRate: number;
}

export interface RemediationPlan {
  /** Plan overview */
  overview: string;
  /** Immediate actions (0-1 days) */
  immediate: RemediationStep[];
  /** Short-term actions (1-7 days) */
  shortTerm: RemediationStep[];
  /** Medium-term actions (1-4 weeks) */
  mediumTerm: RemediationStep[];
  /** Long-term actions (1+ months) */
  longTerm: RemediationStep[];
  /** Total estimated effort (hours) */
  totalEffort: number;
  /** Estimated cost range */
  estimatedCost: {
    min: number;
    max: number;
    currency: string;
  };
}

export interface PenetrationTestConfig {
  /** Test methodology */
  methodology: PenTestMethodology;
  /** Attack vectors to test */
  attackVectors: AttackVector[];
  /** Authentication bypass attempts */
  authBypass: boolean;
  /** Privilege escalation tests */
  privilegeEscalation: boolean;
  /** Data exfiltration simulation */
  dataExfiltration: boolean;
  /** Social engineering simulation */
  socialEngineering: boolean;
  /** Physical security testing */
  physicalSecurity: boolean;
  /** Wireless security testing */
  wirelessSecurity: boolean;
}

export type PenTestMethodology = 'OWASP' | 'NIST' | 'PTES' | 'ISSAF' | 'OSSTMM';

export type AttackVector =
  | 'sql_injection'
  | 'xss'
  | 'csrf'
  | 'xxe'
  | 'lfi'
  | 'rfi'
  | 'directory_traversal'
  | 'command_injection'
  | 'ldap_injection'
  | 'xpath_injection'
  | 'ssti'
  | 'ssrf'
  | 'deserialization'
  | 'race_condition'
  | 'business_logic'
  | 'authentication_bypass'
  | 'session_hijacking'
  | 'clickjacking'
  | 'cors_misconfiguration';

export interface VulnerabilityAssessment {
  /** Asset inventory */
  assets: AssetInventory[];
  /** Vulnerability database */
  vulnerabilities: KnownVulnerability[];
  /** Risk matrix */
  riskMatrix: RiskAssessment[];
  /** Threat modeling results */
  threatModel: ThreatModelingResult;
  /** Attack surface analysis */
  attackSurface: AttackSurfaceAnalysis;
}

export interface AssetInventory {
  /** Asset ID */
  id: string;
  /** Asset name */
  name: string;
  /** Asset type */
  type: AssetType;
  /** Asset location/URL */
  location: string;
  /** Asset owner/responsible team */
  owner: string;
  /** Business criticality */
  criticality: AssetCriticality;
  /** Security controls in place */
  controls: string[];
  /** Last assessment date */
  lastAssessed: number;
}

export type AssetType = 'web_application' | 'api' | 'database' | 'server' | 'network_device' | 'endpoint';
export type AssetCriticality = 'low' | 'medium' | 'high' | 'critical';

export interface KnownVulnerability {
  /** Vulnerability ID */
  id: string;
  /** CVE identifier */
  cveId?: string;
  /** Vulnerability name */
  name: string;
  /** Description */
  description: string;
  /** CVSS score */
  cvssScore: number;
  /** Affected components */
  affectedComponents: string[];
  /** Discovery date */
  discoveryDate: number;
  /** Patch availability */
  patchAvailable: boolean;
  /** Patch information */
  patchInfo?: {
    version: string;
    releaseDate: number;
    description: string;
  };
}

export interface RiskAssessment {
  /** Risk ID */
  id: string;
  /** Risk description */
  description: string;
  /** Probability of occurrence */
  probability: RiskProbability;
  /** Impact if exploited */
  impact: RiskImpact;
  /** Overall risk score */
  riskScore: number;
  /** Risk level */
  riskLevel: SecurityRiskLevel;
  /** Mitigation strategies */
  mitigations: string[];
}

export type RiskProbability = 'very_low' | 'low' | 'medium' | 'high' | 'very_high';
export type RiskImpact = 'negligible' | 'minor' | 'moderate' | 'major' | 'catastrophic';

export interface ThreatModelingResult {
  /** Threat actors identified */
  threatActors: ThreatActor[];
  /** Attack scenarios */
  attackScenarios: AttackScenario[];
  /** Trust boundaries */
  trustBoundaries: TrustBoundary[];
  /** Data flow analysis */
  dataFlows: DataFlow[];
}

export interface ThreatActor {
  /** Actor ID */
  id: string;
  /** Actor name/type */
  name: string;
  /** Motivation */
  motivation: string[];
  /** Capabilities */
  capabilities: string[];
  /** Resources */
  resources: string[];
  /** Typical attack patterns */
  attackPatterns: string[];
}

export interface AttackScenario {
  /** Scenario ID */
  id: string;
  /** Scenario name */
  name: string;
  /** Threat actor */
  actor: string;
  /** Attack vector */
  vector: AttackVector;
  /** Attack steps */
  steps: string[];
  /** Likelihood */
  likelihood: RiskProbability;
  /** Impact */
  impact: RiskImpact;
  /** Countermeasures */
  countermeasures: string[];
}

export interface TrustBoundary {
  /** Boundary ID */
  id: string;
  /** Boundary name */
  name: string;
  /** Source zone */
  sourceZone: string;
  /** Target zone */
  targetZone: string;
  /** Security controls */
  controls: string[];
  /** Trust level */
  trustLevel: number;
}

export interface DataFlow {
  /** Flow ID */
  id: string;
  /** Data source */
  source: string;
  /** Data destination */
  destination: string;
  /** Data classification */
  classification: DataClassification;
  /** Transport method */
  transport: string;
  /** Encryption status */
  encrypted: boolean;
  /** Access controls */
  accessControls: string[];
}

export type DataClassification = 'public' | 'internal' | 'confidential' | 'restricted';

export interface AttackSurfaceAnalysis {
  /** External attack surface */
  external: ExternalAttackSurface;
  /** Internal attack surface */
  internal: InternalAttackSurface;
  /** API attack surface */
  api: APIAttackSurface;
  /** Web application attack surface */
  webApp: WebAppAttackSurface;
}

export interface ExternalAttackSurface {
  /** Exposed ports and services */
  exposedServices: ExposedService[];
  /** Public web applications */
  publicWebApps: string[];
  /** DNS information */
  dnsInfo: DNSInfo;
  /** SSL/TLS configuration */
  sslConfig: SSLConfiguration;
}

export interface ExposedService {
  /** Port number */
  port: number;
  /** Protocol */
  protocol: string;
  /** Service name */
  service: string;
  /** Service version */
  version?: string;
  /** Service banner */
  banner?: string;
  /** Security status */
  securityStatus: 'secure' | 'vulnerable' | 'unknown';
}

export interface DNSInfo {
  /** Domain name */
  domain: string;
  /** DNS records */
  records: DNSRecord[];
  /** Subdomain enumeration results */
  subdomains: string[];
  /** DNS security extensions */
  dnssec: boolean;
}

export interface DNSRecord {
  /** Record type */
  type: string;
  /** Record value */
  value: string;
  /** TTL */
  ttl: number;
}

export interface SSLConfiguration {
  /** SSL/TLS version */
  version: string;
  /** Cipher suites */
  cipherSuites: string[];
  /** Certificate chain */
  certificateChain: SSLCertificate[];
  /** HSTS enabled */
  hsts: boolean;
  /** Certificate transparency */
  certificateTransparency: boolean;
}

export interface SSLCertificate {
  /** Common name */
  commonName: string;
  /** Subject alternative names */
  subjectAltNames: string[];
  /** Issuer */
  issuer: string;
  /** Valid from */
  validFrom: number;
  /** Valid to */
  validTo: number;
  /** Signature algorithm */
  signatureAlgorithm: string;
  /** Key algorithm */
  keyAlgorithm: string;
  /** Key size */
  keySize: number;
}

export interface InternalAttackSurface {
  /** Internal network segments */
  networkSegments: NetworkSegment[];
  /** Internal services */
  internalServices: InternalService[];
  /** Active Directory information */
  activeDirectory?: ActiveDirectoryInfo;
  /** Database servers */
  databases: DatabaseInfo[];
}

export interface NetworkSegment {
  /** Segment ID */
  id: string;
  /** Network CIDR */
  cidr: string;
  /** Segment purpose */
  purpose: string;
  /** Security controls */
  controls: string[];
  /** Host discovery results */
  hosts: HostInfo[];
}

export interface HostInfo {
  /** IP address */
  ip: string;
  /** Hostname */
  hostname?: string;
  /** Operating system */
  os?: string;
  /** Open ports */
  openPorts: number[];
  /** Running services */
  services: string[];
}

export interface InternalService {
  /** Service name */
  name: string;
  /** Service location */
  location: string;
  /** Service type */
  type: string;
  /** Authentication required */
  authRequired: boolean;
  /** Access controls */
  accessControls: string[];
}

export interface ActiveDirectoryInfo {
  /** Domain name */
  domain: string;
  /** Domain controllers */
  domainControllers: string[];
  /** User accounts enumerated */
  userAccounts: ADUser[];
  /** Groups enumerated */
  groups: ADGroup[];
  /** GPO information */
  gpos: GroupPolicyObject[];
}

export interface ADUser {
  /** Username */
  username: string;
  /** Display name */
  displayName: string;
  /** Account status */
  enabled: boolean;
  /** Last login */
  lastLogin?: number;
  /** Group memberships */
  groups: string[];
  /** Privileges */
  privileges: string[];
}

export interface ADGroup {
  /** Group name */
  name: string;
  /** Group type */
  type: string;
  /** Member count */
  memberCount: number;
  /** Privileges */
  privileges: string[];
}

export interface GroupPolicyObject {
  /** GPO name */
  name: string;
  /** GPO ID */
  id: string;
  /** Applied to */
  appliedTo: string[];
  /** Security settings */
  securitySettings: Record<string, any>;
}

export interface DatabaseInfo {
  /** Database type */
  type: string;
  /** Database version */
  version: string;
  /** Database name */
  name: string;
  /** Connection string */
  connectionString: string;
  /** Authentication method */
  authMethod: string;
  /** Encryption status */
  encrypted: boolean;
}

export interface APIAttackSurface {
  /** API endpoints discovered */
  endpoints: APIEndpoint[];
  /** Authentication mechanisms */
  authMechanisms: string[];
  /** API documentation */
  documentation?: string;
  /** Rate limiting */
  rateLimiting: boolean;
  /** Input validation */
  inputValidation: boolean;
}

export interface APIEndpoint {
  /** HTTP method */
  method: string;
  /** Endpoint path */
  path: string;
  /** Parameters */
  parameters: APIParameter[];
  /** Authentication required */
  authRequired: boolean;
  /** Response format */
  responseFormat: string;
  /** Potential vulnerabilities */
  vulnerabilities: string[];
}

export interface APIParameter {
  /** Parameter name */
  name: string;
  /** Parameter type */
  type: string;
  /** Required parameter */
  required: boolean;
  /** Validation rules */
  validation?: string[];
  /** Potential injection points */
  injectionPoints: string[];
}

export interface WebAppAttackSurface {
  /** Web pages discovered */
  pages: WebPage[];
  /** Forms discovered */
  forms: WebForm[];
  /** Input fields */
  inputFields: InputField[];
  /** Client-side technologies */
  clientTechnologies: string[];
  /** JavaScript frameworks */
  jsFrameworks: string[];
}

export interface WebPage {
  /** Page URL */
  url: string;
  /** Page title */
  title: string;
  /** HTTP status */
  status: number;
  /** Content type */
  contentType: string;
  /** Forms on page */
  forms: number;
  /** Links on page */
  links: string[];
  /** Potential vulnerabilities */
  vulnerabilities: string[];
}

export interface WebForm {
  /** Form action URL */
  action: string;
  /** HTTP method */
  method: string;
  /** Form fields */
  fields: FormField[];
  /** CSRF protection */
  csrfProtection: boolean;
  /** Input validation */
  inputValidation: boolean;
}

export interface FormField {
  /** Field name */
  name: string;
  /** Field type */
  type: string;
  /** Required field */
  required: boolean;
  /** Max length */
  maxLength?: number;
  /** Pattern validation */
  pattern?: string;
}

export interface InputField {
  /** Field location */
  location: string;
  /** Field name */
  name: string;
  /** Field type */
  type: string;
  /** Validation present */
  validated: boolean;
  /** Sanitization present */
  sanitized: boolean;
  /** Potential injection vectors */
  injectionVectors: string[];
}

export interface SecurityAuditService {
  /** Execute comprehensive security audit */
  executeAudit(config: SecurityAuditConfig): Promise<SecurityAuditResult>;

  /** Execute penetration test */
  executePenetrationTest(config: PenetrationTestConfig): Promise<SecurityAuditResult>;

  /** Perform vulnerability assessment */
  performVulnerabilityAssessment(): Promise<VulnerabilityAssessment>;

  /** Generate security report */
  generateReport(result: SecurityAuditResult, format: 'html' | 'json' | 'pdf'): Promise<string>;

  /** Continuous security monitoring */
  startContinuousMonitoring(config: SecurityAuditConfig): Promise<void>;

  /** Stop continuous monitoring */
  stopContinuousMonitoring(): Promise<void>;

  /** Get audit history */
  getAuditHistory(limit?: number): Promise<SecurityAuditResult[]>;

  /** Schedule automated audits */
  scheduleAudit(config: SecurityAuditConfig, schedule: string): Promise<string>;
}