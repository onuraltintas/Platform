/**
 * Enterprise Security Monitoring and Alerting Interfaces
 * Real-time security monitoring, threat detection, and incident response
 */

// Base monitoring types
export type AlertSeverity = 'info' | 'low' | 'medium' | 'high' | 'critical';
export type AlertStatus = 'new' | 'acknowledged' | 'investigating' | 'resolved' | 'false_positive';
export type MonitoringScope = 'authentication' | 'authorization' | 'input_validation' | 'api' | 'network' | 'application' | 'database' | 'infrastructure';
export type ThreatLevel = 'none' | 'low' | 'medium' | 'high' | 'critical';
export type IncidentType = 'security_breach' | 'data_leak' | 'unauthorized_access' | 'malware' | 'ddos' | 'phishing' | 'insider_threat' | 'compliance_violation';

// Security event interfaces
export interface SecurityEvent {
  id: string;
  timestamp: Date;
  source: string;
  scope: MonitoringScope;
  severity: AlertSeverity;
  type: string;
  message: string;
  details: Record<string, any>;
  userAgent?: string;
  ipAddress?: string;
  userId?: string;
  sessionId?: string;
  requestId?: string;
  url?: string;
  method?: string;
  statusCode?: number;
  responseTime?: number;
  payload?: any;
  headers?: Record<string, string>;
  fingerprint: string;
  tags: string[];
  metadata: SecurityEventMetadata;
}

export interface SecurityEventMetadata {
  location?: GeoLocation;
  device?: DeviceInfo;
  network?: NetworkInfo;
  threat?: ThreatInfo;
  context?: SecurityContext;
  correlationId?: string;
  parentEventId?: string;
  childEventIds?: string[];
}

export interface GeoLocation {
  country: string;
  region: string;
  city: string;
  latitude: number;
  longitude: number;
  timezone: string;
  isp: string;
  organization?: string;
  asn?: string;
}

export interface DeviceInfo {
  platform: string;
  browser: string;
  version: string;
  mobile: boolean;
  fingerprint: string;
  capabilities: string[];
  language: string;
  timezone: string;
}

export interface NetworkInfo {
  protocol: string;
  port: number;
  encrypted: boolean;
  proxy: boolean;
  tor: boolean;
  vpn: boolean;
  cdn: boolean;
  reputation: 'clean' | 'suspicious' | 'malicious';
  asn: string;
  organization: string;
}

export interface ThreatInfo {
  level: ThreatLevel;
  category: string[];
  indicators: ThreatIndicator[];
  confidence: number;
  source: string;
  lastSeen: Date;
  reputation: number;
  malwareFamily?: string;
  attackVector?: string[];
  mitreAttack?: string[];
}

export interface ThreatIndicator {
  type: 'ip' | 'domain' | 'url' | 'hash' | 'email' | 'file' | 'pattern';
  value: string;
  confidence: number;
  source: string;
  firstSeen: Date;
  lastSeen: Date;
  description: string;
  tags: string[];
}

export interface SecurityContext {
  environment: 'development' | 'staging' | 'production';
  application: string;
  module: string;
  feature: string;
  action: string;
  resource: string;
  permissions: string[];
  roles: string[];
  sensitivity: 'public' | 'internal' | 'confidential' | 'restricted' | 'top_secret';
}

// Alert and notification interfaces
export interface SecurityAlert {
  id: string;
  title: string;
  description: string;
  severity: AlertSeverity;
  status: AlertStatus;
  type: string;
  scope: MonitoringScope;
  createdAt: Date;
  updatedAt: Date;
  resolvedAt?: Date;
  assignedTo?: string;
  assignedBy?: string;
  events: SecurityEvent[];
  indicators: AlertIndicator[];
  timeline: AlertTimelineEntry[];
  actions: AlertAction[];
  suppressions: AlertSuppression[];
  escalations: AlertEscalation[];
  tags: string[];
  priority: number;
  confidence: number;
  falsePositiveRisk: number;
  impactAssessment: ImpactAssessment;
  remediationSteps: RemediationStep[];
  relatedAlerts: string[];
  externalReferences: ExternalReference[];
}

export interface AlertIndicator {
  type: string;
  value: string;
  description: string;
  confidence: number;
  source: string;
  verified: boolean;
  malicious: boolean;
  whitelisted: boolean;
  reputation: number;
  firstSeen: Date;
  lastSeen: Date;
  occurrences: number;
  contexts: SecurityContext[];
}

export interface AlertTimelineEntry {
  timestamp: Date;
  action: string;
  user: string;
  description: string;
  details: Record<string, any>;
  automated: boolean;
}

export interface AlertAction {
  id: string;
  type: 'block' | 'allow' | 'monitor' | 'escalate' | 'notify' | 'quarantine' | 'investigate';
  description: string;
  automated: boolean;
  executedAt?: Date;
  executedBy?: string;
  result?: ActionResult;
  conditions: ActionCondition[];
  config: Record<string, any>;
}

export interface ActionResult {
  success: boolean;
  message: string;
  details: Record<string, any>;
  duration: number;
  sideEffects: string[];
}

export interface ActionCondition {
  field: string;
  operator: 'equals' | 'not_equals' | 'contains' | 'not_contains' | 'greater_than' | 'less_than' | 'regex' | 'in' | 'not_in';
  value: any;
  caseSensitive?: boolean;
}

export interface AlertSuppression {
  id: string;
  reason: string;
  suppressed: boolean;
  suppressedAt: Date;
  suppressedBy: string;
  expiresAt?: Date;
  conditions: ActionCondition[];
}

export interface AlertEscalation {
  id: string;
  level: number;
  triggeredAt: Date;
  escalatedTo: string[];
  escalatedBy: string;
  reason: string;
  acknowledged: boolean;
  acknowledgedAt?: Date;
  acknowledgedBy?: string;
}

export interface ImpactAssessment {
  scope: 'local' | 'module' | 'application' | 'system' | 'organization';
  severity: AlertSeverity;
  affectedUsers: number;
  affectedSystems: string[];
  dataAtRisk: DataRiskAssessment;
  businessImpact: BusinessImpact;
  technicalImpact: TechnicalImpact;
  complianceImpact: ComplianceImpact;
  reputationImpact: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
}

export interface DataRiskAssessment {
  classification: 'public' | 'internal' | 'confidential' | 'restricted' | 'top_secret';
  volume: number;
  types: string[];
  sensitivity: number;
  personalData: boolean;
  financialData: boolean;
  healthData: boolean;
  intellectualProperty: boolean;
  regulatedData: boolean;
}

export interface BusinessImpact {
  revenue: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  operations: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  reputation: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  legal: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  regulatory: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  estimatedCost?: number;
  estimatedDowntime?: number;
  customersAffected?: number;
}

export interface TechnicalImpact {
  availability: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  integrity: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  confidentiality: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  accountability: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  systemsAffected: string[];
  servicesAffected: string[];
  dataIntegrityRisk: boolean;
  dataLossRisk: boolean;
  systemCompromiseRisk: boolean;
}

export interface ComplianceImpact {
  frameworks: string[];
  violations: ComplianceViolation[];
  reportingRequired: boolean;
  timeframe?: number;
  authorities: string[];
  penalties: CompliancePenalty[];
}

export interface ComplianceViolation {
  framework: string;
  requirement: string;
  severity: AlertSeverity;
  description: string;
  evidence: string[];
  remediation: string[];
}

export interface CompliancePenalty {
  type: 'fine' | 'suspension' | 'revocation' | 'corrective_action' | 'monitoring';
  amount?: number;
  currency?: string;
  description: string;
  probability: number;
}

export interface RemediationStep {
  id: string;
  title: string;
  description: string;
  priority: number;
  category: 'immediate' | 'short_term' | 'long_term' | 'preventive';
  automated: boolean;
  estimatedTime: number;
  requiredSkills: string[];
  requiredTools: string[];
  dependencies: string[];
  status: 'pending' | 'in_progress' | 'completed' | 'skipped' | 'failed';
  assignedTo?: string;
  startedAt?: Date;
  completedAt?: Date;
  result?: ActionResult;
}

export interface ExternalReference {
  type: 'cve' | 'cwe' | 'mitre' | 'nist' | 'owasp' | 'vendor' | 'threat_intel' | 'blog' | 'research';
  id: string;
  url: string;
  title: string;
  description: string;
  relevance: number;
  verified: boolean;
}

// Monitoring configuration interfaces
export interface MonitoringRule {
  id: string;
  name: string;
  description: string;
  enabled: boolean;
  scope: MonitoringScope[];
  conditions: RuleCondition[];
  actions: AlertAction[];
  schedule?: RuleSchedule;
  suppressions: RuleSuppression[];
  tags: string[];
  priority: number;
  falsePositiveRate: number;
  effectiveness: number;
  lastUpdated: Date;
  createdBy: string;
  updatedBy: string;
  version: number;
  parentRuleId?: string;
  customFields: Record<string, any>;
}

export interface RuleCondition {
  id: string;
  field: string;
  operator: 'equals' | 'not_equals' | 'contains' | 'not_contains' | 'starts_with' | 'ends_with' | 'regex' | 'greater_than' | 'less_than' | 'between' | 'in' | 'not_in' | 'exists' | 'not_exists';
  value: any;
  caseSensitive?: boolean;
  weight: number;
  required: boolean;
  timeWindow?: TimeWindow;
  aggregation?: AggregationRule;
}

export interface TimeWindow {
  duration: number;
  unit: 'seconds' | 'minutes' | 'hours' | 'days';
  sliding: boolean;
}

export interface AggregationRule {
  function: 'count' | 'sum' | 'avg' | 'min' | 'max' | 'distinct' | 'rate' | 'percentile';
  field?: string;
  threshold: number;
  operator: 'greater_than' | 'less_than' | 'equals' | 'between';
  groupBy?: string[];
}

export interface RuleSchedule {
  timezone: string;
  active: boolean;
  activeHours?: TimeRange[];
  activeDays?: number[];
  exceptions?: ScheduleException[];
}

export interface TimeRange {
  start: string; // HH:MM format
  end: string;   // HH:MM format
}

export interface ScheduleException {
  date: Date;
  reason: string;
  active: boolean;
}

export interface RuleSuppression {
  id: string;
  conditions: ActionCondition[];
  duration?: number;
  reason: string;
  active: boolean;
  createdAt: Date;
  createdBy: string;
}

// Notification and communication interfaces
export interface NotificationChannel {
  id: string;
  name: string;
  type: 'email' | 'sms' | 'slack' | 'teams' | 'webhook' | 'pagerduty' | 'jira' | 'servicenow';
  enabled: boolean;
  config: NotificationConfig;
  filters: NotificationFilter[];
  rateLimits: RateLimit[];
  failover?: NotificationChannel[];
  escalation?: EscalationConfig;
  templates: NotificationTemplate[];
}

export interface NotificationConfig {
  endpoint?: string;
  apiKey?: string;
  token?: string;
  credentials?: Record<string, string>;
  headers?: Record<string, string>;
  timeout?: number;
  retries?: number;
  batchSize?: number;
  format?: 'json' | 'xml' | 'text' | 'html';
  encryption?: EncryptionConfig;
}

export interface EncryptionConfig {
  enabled: boolean;
  algorithm?: string;
  keyId?: string;
  signatureRequired?: boolean;
}

export interface NotificationFilter {
  id: string;
  name: string;
  conditions: ActionCondition[];
  action: 'include' | 'exclude' | 'priority_boost' | 'priority_reduce';
  priority?: number;
  enabled: boolean;
}

export interface RateLimit {
  window: TimeWindow;
  maxEvents: number;
  action: 'drop' | 'queue' | 'escalate';
  escalationChannel?: string;
}

export interface EscalationConfig {
  levels: EscalationLevel[];
  autoEscalate: boolean;
  escalationDelay: number;
  maxEscalations: number;
}

export interface EscalationLevel {
  level: number;
  channels: string[];
  delay: number;
  conditions?: ActionCondition[];
}

export interface NotificationTemplate {
  id: string;
  name: string;
  type: 'email' | 'sms' | 'slack' | 'teams' | 'webhook';
  subject: string;
  body: string;
  variables: TemplateVariable[];
  formatting: TemplateFormatting;
  localization?: TemplateLocalization[];
}

export interface TemplateVariable {
  name: string;
  description: string;
  type: 'string' | 'number' | 'date' | 'boolean' | 'array' | 'object';
  required: boolean;
  defaultValue?: any;
  format?: string;
  validation?: VariableValidation;
}

export interface VariableValidation {
  pattern?: string;
  minLength?: number;
  maxLength?: number;
  minValue?: number;
  maxValue?: number;
  allowedValues?: any[];
}

export interface TemplateFormatting {
  dateFormat: string;
  timeFormat: string;
  timezone: string;
  numberFormat: string;
  currency?: string;
  truncateLength?: number;
  htmlEscape: boolean;
}

export interface TemplateLocalization {
  locale: string;
  subject: string;
  body: string;
  variables: Record<string, string>;
}

// Monitoring metrics and analytics interfaces
export interface SecurityMetrics {
  timestamp: Date;
  period: MetricPeriod;
  events: EventMetrics;
  alerts: AlertMetrics;
  threats: ThreatMetrics;
  performance: PerformanceMetrics;
  compliance: ComplianceMetrics;
  trends: TrendAnalysis;
  anomalies: AnomalyDetection[];
  predictions: PredictionResult[];
}

export interface MetricPeriod {
  start: Date;
  end: Date;
  duration: number;
  unit: 'minutes' | 'hours' | 'days' | 'weeks' | 'months';
}

export interface EventMetrics {
  total: number;
  byScope: Record<MonitoringScope, number>;
  bySeverity: Record<AlertSeverity, number>;
  byType: Record<string, number>;
  bySource: Record<string, number>;
  rate: number;
  peakRate: number;
  averageRate: number;
  distribution: TimeSeriesPoint[];
}

export interface AlertMetrics {
  total: number;
  new: number;
  acknowledged: number;
  investigating: number;
  resolved: number;
  falsePositives: number;
  bySeverity: Record<AlertSeverity, number>;
  byType: Record<string, number>;
  resolutionTime: StatisticalMetrics;
  escalationRate: number;
  automationRate: number;
  accuracy: number;
}

export interface ThreatMetrics {
  total: number;
  byLevel: Record<ThreatLevel, number>;
  byCategory: Record<string, number>;
  bySource: Record<string, number>;
  blocked: number;
  allowed: number;
  investigated: number;
  newThreats: number;
  evolvedThreats: number;
  threatActors: ThreatActorMetrics[];
  campaigns: ThreatCampaignMetrics[];
}

export interface PerformanceMetrics {
  processingTime: StatisticalMetrics;
  throughput: number;
  latency: StatisticalMetrics;
  availability: number;
  errorRate: number;
  resourceUsage: ResourceUsageMetrics;
  queueDepth: number;
  batchProcessingEfficiency: number;
}

export interface ComplianceMetrics {
  score: number;
  frameworks: Record<string, ComplianceFrameworkMetrics>;
  violations: number;
  resolvedViolations: number;
  coverage: number;
  maturity: ComplianceMaturityMetrics;
  gaps: ComplianceGap[];
}

export interface StatisticalMetrics {
  min: number;
  max: number;
  avg: number;
  median: number;
  p95: number;
  p99: number;
  stdDev: number;
  samples: number;
}

export interface TimeSeriesPoint {
  timestamp: Date;
  value: number;
  metadata?: Record<string, any>;
}

export interface ThreatActorMetrics {
  id: string;
  name: string;
  activity: number;
  sophistication: 'low' | 'medium' | 'high' | 'advanced';
  motivation: string[];
  targets: string[];
  techniques: string[];
  firstSeen: Date;
  lastSeen: Date;
}

export interface ThreatCampaignMetrics {
  id: string;
  name: string;
  description: string;
  active: boolean;
  startDate: Date;
  endDate?: Date;
  targets: number;
  success: number;
  techniques: string[];
  indicators: number;
  attribution: string[];
}

export interface ResourceUsageMetrics {
  cpu: number;
  memory: number;
  disk: number;
  network: number;
  database: number;
  cache: number;
  threads: number;
  connections: number;
}

export interface ComplianceFrameworkMetrics {
  name: string;
  version: string;
  score: number;
  requirements: number;
  implemented: number;
  compliant: number;
  violations: number;
  gaps: number;
  maturity: number;
  lastAssessment: Date;
}

export interface ComplianceMaturityMetrics {
  overall: number;
  governance: number;
  processes: number;
  technology: number;
  people: number;
  culture: number;
}

export interface ComplianceGap {
  framework: string;
  requirement: string;
  description: string;
  severity: AlertSeverity;
  priority: number;
  effort: 'low' | 'medium' | 'high';
  timeline: number;
  owner: string;
  dependencies: string[];
}

export interface TrendAnalysis {
  events: TrendMetric;
  alerts: TrendMetric;
  threats: TrendMetric;
  performance: TrendMetric;
  compliance: TrendMetric;
  predictions: TrendPrediction[];
}

export interface TrendMetric {
  current: number;
  previous: number;
  change: number;
  changePercent: number;
  trend: 'increasing' | 'decreasing' | 'stable' | 'volatile';
  confidence: number;
  forecast: ForecastPoint[];
}

export interface TrendPrediction {
  metric: string;
  prediction: number;
  confidence: number;
  timeframe: number;
  factors: PredictionFactor[];
}

export interface ForecastPoint {
  timestamp: Date;
  value: number;
  confidence: number;
  upperBound: number;
  lowerBound: number;
}

export interface PredictionFactor {
  name: string;
  impact: number;
  confidence: number;
  description: string;
}

export interface AnomalyDetection {
  id: string;
  timestamp: Date;
  metric: string;
  value: number;
  expected: number;
  deviation: number;
  severity: AlertSeverity;
  confidence: number;
  context: AnomalyContext;
  rootCause?: AnomalyRootCause;
}

export interface AnomalyContext {
  timeframe: string;
  baseline: number;
  variance: number;
  seasonality: boolean;
  external: ExternalFactor[];
}

export interface ExternalFactor {
  name: string;
  impact: number;
  correlation: number;
  description: string;
}

export interface AnomalyRootCause {
  category: string;
  description: string;
  confidence: number;
  evidence: string[];
  recommendations: string[];
}

export interface PredictionResult {
  metric: string;
  timestamp: Date;
  predicted: number;
  actual?: number;
  accuracy?: number;
  model: PredictionModel;
  factors: PredictionFactor[];
}

export interface PredictionModel {
  name: string;
  version: string;
  algorithm: string;
  accuracy: number;
  lastTrained: Date;
  features: string[];
  parameters: Record<string, any>;
}

// Service interfaces
export interface ISecurityMonitoringService {
  // Event processing
  processEvent(event: SecurityEvent): Promise<void>;
  processEvents(events: SecurityEvent[]): Promise<void>;

  // Alert management
  createAlert(event: SecurityEvent | SecurityEvent[]): Promise<SecurityAlert>;
  updateAlert(alertId: string, updates: Partial<SecurityAlert>): Promise<SecurityAlert>;
  resolveAlert(alertId: string, resolution: string, resolvedBy: string): Promise<void>;
  acknowledgeAlert(alertId: string, acknowledgedBy: string): Promise<void>;
  escalateAlert(alertId: string, escalatedBy: string, reason: string): Promise<void>;
  suppressAlert(alertId: string, suppression: AlertSuppression): Promise<void>;

  // Monitoring rules
  createRule(rule: Omit<MonitoringRule, 'id'>): Promise<MonitoringRule>;
  updateRule(ruleId: string, updates: Partial<MonitoringRule>): Promise<MonitoringRule>;
  deleteRule(ruleId: string): Promise<void>;
  evaluateRules(event: SecurityEvent): Promise<MonitoringRule[]>;

  // Notifications
  sendNotification(alert: SecurityAlert, channels: string[]): Promise<void>;
  testNotificationChannel(channelId: string): Promise<boolean>;

  // Metrics and analytics
  getMetrics(period: MetricPeriod, scopes?: MonitoringScope[]): Promise<SecurityMetrics>;
  detectAnomalies(metrics: SecurityMetrics): Promise<AnomalyDetection[]>;
  generatePredictions(historical: SecurityMetrics[]): Promise<PredictionResult[]>;

  // Search and retrieval
  searchEvents(query: EventSearchQuery): Promise<SecurityEvent[]>;
  searchAlerts(query: AlertSearchQuery): Promise<SecurityAlert[]>;
  getEventById(eventId: string): Promise<SecurityEvent | null>;
  getAlertById(alertId: string): Promise<SecurityAlert | null>;

  // Health and status
  getSystemHealth(): Promise<SystemHealth>;
  getProcessingStats(): Promise<ProcessingStats>;
}

export interface EventSearchQuery {
  query?: string;
  scopes?: MonitoringScope[];
  severities?: AlertSeverity[];
  types?: string[];
  sources?: string[];
  timeRange?: TimeRange;
  limit?: number;
  offset?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  filters?: Record<string, any>;
}

export interface AlertSearchQuery {
  query?: string;
  statuses?: AlertStatus[];
  severities?: AlertSeverity[];
  types?: string[];
  scopes?: MonitoringScope[];
  assignedTo?: string;
  timeRange?: TimeRange;
  limit?: number;
  offset?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  filters?: Record<string, any>;
}

export interface SystemHealth {
  status: 'healthy' | 'degraded' | 'unhealthy';
  components: ComponentHealth[];
  uptime: number;
  version: string;
  lastHealthCheck: Date;
  issues: HealthIssue[];
  metrics: HealthMetrics;
}

export interface ComponentHealth {
  name: string;
  status: 'healthy' | 'degraded' | 'unhealthy';
  response: number;
  lastCheck: Date;
  dependencies: string[];
  details: Record<string, any>;
}

export interface HealthIssue {
  component: string;
  severity: AlertSeverity;
  description: string;
  since: Date;
  resolved: boolean;
  impact: string[];
}

export interface HealthMetrics {
  eventsPerSecond: number;
  alertsPerMinute: number;
  processingLatency: number;
  queueDepth: number;
  errorRate: number;
  memoryUsage: number;
  cpuUsage: number;
  diskUsage: number;
}

export interface ProcessingStats {
  totalEvents: number;
  eventsPerSecond: number;
  alertsGenerated: number;
  rulesEvaluated: number;
  notificationsSent: number;
  processingTime: StatisticalMetrics;
  queueStats: QueueStats;
  errorStats: ErrorStats;
  performanceHistory: PerformanceSnapshot[];
}

export interface QueueStats {
  depth: number;
  maxDepth: number;
  processed: number;
  failed: number;
  retried: number;
  dropped: number;
  avgProcessingTime: number;
}

export interface ErrorStats {
  total: number;
  byType: Record<string, number>;
  recent: ProcessingError[];
  rate: number;
  resolved: number;
}

export interface ProcessingError {
  timestamp: Date;
  type: string;
  message: string;
  details: Record<string, any>;
  severity: AlertSeverity;
  component: string;
  recovered: boolean;
  attempts: number;
}

export interface PerformanceSnapshot {
  timestamp: Date;
  eventsPerSecond: number;
  processingLatency: number;
  queueDepth: number;
  errorRate: number;
  resourceUsage: ResourceUsageMetrics;
}