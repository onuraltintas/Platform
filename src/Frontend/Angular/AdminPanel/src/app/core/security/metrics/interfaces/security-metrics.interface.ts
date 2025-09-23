/**
 * Enterprise Security Metrics and Reporting Dashboard Interfaces
 * Real-time security metrics, KPIs, and comprehensive reporting system
 */

// Base metric types
export type MetricType = 'counter' | 'gauge' | 'histogram' | 'summary' | 'rate' | 'percentage' | 'ratio' | 'duration';
export type MetricCategory = 'security' | 'compliance' | 'performance' | 'business' | 'operational' | 'financial' | 'risk' | 'quality';
export type MetricPriority = 'low' | 'medium' | 'high' | 'critical';
export type MetricStatus = 'healthy' | 'warning' | 'critical' | 'unknown' | 'disabled';
export type AggregationType = 'sum' | 'average' | 'min' | 'max' | 'count' | 'percentile' | 'rate' | 'trend';
export type TimeGranularity = 'second' | 'minute' | 'hour' | 'day' | 'week' | 'month' | 'quarter' | 'year';
export type DashboardType = 'executive' | 'operational' | 'technical' | 'compliance' | 'security_ops' | 'custom';
export type VisualizationType = 'line_chart' | 'bar_chart' | 'pie_chart' | 'gauge' | 'heatmap' | 'table' | 'counter' | 'trend' | 'sparkline' | 'sankey';

// Core metric interfaces
export interface SecurityMetric {
  id: string;
  name: string;
  displayName: string;
  description: string;
  category: MetricCategory;
  type: MetricType;
  priority: MetricPriority;
  status: MetricStatus;
  unit: string;
  value: MetricValue;
  target?: MetricTarget;
  thresholds: MetricThreshold[];
  tags: string[];
  dimensions: MetricDimension[];
  aggregation: MetricAggregation;
  collection: MetricCollection;
  retention: MetricRetention;
  alerts: MetricAlert[];
  metadata: MetricMetadata;
  createdAt: Date;
  updatedAt: Date;
  lastCollected: Date;
  enabled: boolean;
}

export interface MetricValue {
  current: number;
  previous: number;
  change: number;
  changePercent: number;
  trend: MetricTrend;
  confidence: number;
  quality: MetricQuality;
  timestamp: Date;
  source: string;
  attributes: Record<string, any>;
}

export interface MetricTrend {
  direction: 'up' | 'down' | 'stable' | 'volatile';
  magnitude: number;
  duration: number;
  prediction: TrendPrediction;
  seasonality: SeasonalityInfo;
  anomalies: AnomalyInfo[];
}

export interface TrendPrediction {
  shortTerm: PredictionPoint[];
  mediumTerm: PredictionPoint[];
  longTerm: PredictionPoint[];
  confidence: number;
  factors: PredictionFactor[];
  methodology: string;
}

export interface PredictionPoint {
  timestamp: Date;
  value: number;
  confidence: number;
  upperBound: number;
  lowerBound: number;
  scenario: string;
}

export interface PredictionFactor {
  name: string;
  impact: number;
  correlation: number;
  significance: number;
  controllable: boolean;
  description: string;
}

export interface SeasonalityInfo {
  detected: boolean;
  pattern: string;
  cycle: number;
  strength: number;
  peaks: SeasonalPeak[];
  adjustments: SeasonalAdjustment[];
}

export interface SeasonalPeak {
  period: string;
  value: number;
  recurrence: number;
  confidence: number;
  description: string;
}

export interface SeasonalAdjustment {
  period: string;
  factor: number;
  applied: boolean;
  description: string;
}

export interface AnomalyInfo {
  timestamp: Date;
  value: number;
  expected: number;
  deviation: number;
  severity: 'low' | 'medium' | 'high' | 'critical';
  type: 'spike' | 'drop' | 'trend_change' | 'seasonal_anomaly' | 'outlier';
  cause: string;
  confidence: number;
  resolved: boolean;
}

export interface MetricQuality {
  completeness: number;
  accuracy: number;
  timeliness: number;
  consistency: number;
  validity: number;
  overall: number;
  issues: QualityIssue[];
  lastAssessed: Date;
}

export interface QualityIssue {
  type: 'missing_data' | 'inconsistent_data' | 'delayed_data' | 'invalid_data' | 'duplicate_data';
  description: string;
  impact: 'low' | 'medium' | 'high';
  occurrences: number;
  firstSeen: Date;
  lastSeen: Date;
  resolved: boolean;
  resolution: string;
}

export interface MetricTarget {
  value: number;
  unit: string;
  type: 'minimum' | 'maximum' | 'exact' | 'range';
  priority: MetricPriority;
  deadline: Date;
  progress: TargetProgress;
  rationale: string;
  stakeholder: string;
  reviews: TargetReview[];
}

export interface TargetProgress {
  current: number;
  target: number;
  progress: number;
  onTrack: boolean;
  eta: Date;
  velocity: number;
  trajectory: TrajectoryInfo;
}

export interface TrajectoryInfo {
  predicted: number;
  confidence: number;
  factors: string[];
  risks: string[];
  opportunities: string[];
}

export interface TargetReview {
  date: Date;
  reviewer: string;
  outcome: 'on_track' | 'at_risk' | 'missed' | 'exceeded' | 'revised';
  notes: string;
  adjustments: TargetAdjustment[];
}

export interface TargetAdjustment {
  type: 'value' | 'deadline' | 'scope' | 'methodology';
  oldValue: any;
  newValue: any;
  reason: string;
  impact: string;
  approver: string;
}

export interface MetricThreshold {
  id: string;
  name: string;
  level: 'info' | 'warning' | 'critical' | 'emergency';
  operator: 'gt' | 'gte' | 'lt' | 'lte' | 'eq' | 'neq' | 'between' | 'outside';
  value: number | number[];
  duration: number;
  enabled: boolean;
  actions: ThresholdAction[];
  conditions: ThresholdCondition[];
  suppressions: ThresholdSuppression[];
  history: ThresholdTrigger[];
}

export interface ThresholdAction {
  type: 'alert' | 'notification' | 'escalation' | 'automation' | 'remediation';
  config: Record<string, any>;
  priority: MetricPriority;
  enabled: boolean;
  conditions: string[];
}

export interface ThresholdCondition {
  field: string;
  operator: string;
  value: any;
  required: boolean;
  description: string;
}

export interface ThresholdSuppression {
  reason: string;
  startTime: Date;
  endTime: Date;
  conditions: string[];
  approver: string;
  active: boolean;
}

export interface ThresholdTrigger {
  timestamp: Date;
  value: number;
  level: string;
  duration: number;
  resolved: boolean;
  resolvedAt?: Date;
  actions: ActionExecution[];
}

export interface ActionExecution {
  action: string;
  timestamp: Date;
  status: 'pending' | 'executing' | 'completed' | 'failed' | 'cancelled';
  result: any;
  duration: number;
  error?: string;
}

export interface MetricDimension {
  name: string;
  description: string;
  type: 'categorical' | 'numerical' | 'temporal' | 'geographical' | 'hierarchical';
  values: DimensionValue[];
  cardinality: number;
  nullable: boolean;
  indexed: boolean;
}

export interface DimensionValue {
  value: any;
  label: string;
  description?: string;
  metadata: Record<string, any>;
  frequency: number;
  lastSeen: Date;
}

export interface MetricAggregation {
  method: AggregationType;
  window: TimeWindow;
  groupBy: string[];
  filters: AggregationFilter[];
  interpolation: InterpolationConfig;
  sampling: SamplingConfig;
  weights: AggregationWeight[];
}

export interface TimeWindow {
  size: number;
  unit: TimeGranularity;
  alignment: 'start' | 'end' | 'center';
  sliding: boolean;
  bufferSize: number;
}

export interface AggregationFilter {
  dimension: string;
  operator: string;
  value: any;
  exclude: boolean;
}

export interface InterpolationConfig {
  method: 'linear' | 'cubic' | 'nearest' | 'forward_fill' | 'backward_fill' | 'none';
  maxGap: number;
  confidence: number;
  enabled: boolean;
}

export interface SamplingConfig {
  method: 'uniform' | 'random' | 'stratified' | 'systematic';
  rate: number;
  minSamples: number;
  maxSamples: number;
  seed?: number;
}

export interface AggregationWeight {
  dimension: string;
  value: any;
  weight: number;
  description: string;
}

export interface MetricCollection {
  source: DataSource;
  frequency: CollectionFrequency;
  method: CollectionMethod;
  validation: ValidationConfig;
  enrichment: EnrichmentConfig;
  pipeline: ProcessingPipeline;
  backup: BackupConfig;
}

export interface DataSource {
  type: 'database' | 'api' | 'file' | 'stream' | 'log' | 'sensor' | 'synthetic';
  connection: ConnectionConfig;
  query: QueryConfig;
  authentication: AuthenticationConfig;
  reliability: ReliabilityConfig;
}

export interface ConnectionConfig {
  url: string;
  timeout: number;
  retries: number;
  pool: PoolConfig;
  security: SecurityConfig;
  monitoring: MonitoringConfig;
}

export interface PoolConfig {
  min: number;
  max: number;
  idle: number;
  acquire: number;
  evict: number;
}

export interface SecurityConfig {
  encryption: boolean;
  authentication: string[];
  authorization: string[];
  audit: boolean;
  compliance: string[];
}

export interface MonitoringConfig {
  health: boolean;
  performance: boolean;
  errors: boolean;
  usage: boolean;
  alerts: string[];
}

export interface QueryConfig {
  statement: string;
  parameters: QueryParameter[];
  optimization: QueryOptimization;
  caching: CachingConfig;
  pagination: PaginationConfig;
}

export interface QueryParameter {
  name: string;
  type: string;
  value: any;
  required: boolean;
  validation: string[];
}

export interface QueryOptimization {
  enabled: boolean;
  hints: string[];
  indexes: string[];
  explain: boolean;
  timeout: number;
}

export interface CachingConfig {
  enabled: boolean;
  ttl: number;
  maxSize: number;
  strategy: 'lru' | 'lfu' | 'ttl' | 'none';
  invalidation: string[];
}

export interface PaginationConfig {
  enabled: boolean;
  pageSize: number;
  maxPages: number;
  strategy: 'offset' | 'cursor' | 'keyset';
}

export interface AuthenticationConfig {
  type: 'none' | 'basic' | 'bearer' | 'oauth2' | 'api_key' | 'mtls';
  credentials: Record<string, string>;
  refresh: RefreshConfig;
  validation: ValidationRule[];
}

export interface RefreshConfig {
  enabled: boolean;
  interval: number;
  threshold: number;
  strategy: 'proactive' | 'reactive';
}

export interface ValidationRule {
  field: string;
  rule: string;
  message: string;
  severity: 'error' | 'warning' | 'info';
}

export interface ReliabilityConfig {
  availability: number;
  consistency: 'strong' | 'eventual' | 'weak';
  partitioning: 'available' | 'consistent';
  durability: number;
  backup: string[];
}

export interface CollectionFrequency {
  interval: number;
  unit: TimeGranularity;
  schedule: ScheduleConfig;
  adaptive: AdaptiveConfig;
  burst: BurstConfig;
}

export interface ScheduleConfig {
  cron?: string;
  timezone: string;
  holidays: HolidayConfig;
  maintenance: MaintenanceWindow[];
}

export interface HolidayConfig {
  calendar: string;
  skip: boolean;
  delay: boolean;
  advance: boolean;
}

export interface MaintenanceWindow {
  start: string;
  end: string;
  days: number[];
  action: 'skip' | 'delay' | 'continue';
  notification: boolean;
}

export interface AdaptiveConfig {
  enabled: boolean;
  factors: AdaptiveFactor[];
  bounds: AdaptiveBounds;
  algorithm: 'threshold' | 'ml' | 'rule_based';
}

export interface AdaptiveFactor {
  name: string;
  weight: number;
  threshold: number;
  action: 'increase' | 'decrease' | 'maintain';
}

export interface AdaptiveBounds {
  minInterval: number;
  maxInterval: number;
  maxChange: number;
  stability: number;
}

export interface BurstConfig {
  enabled: boolean;
  trigger: string[];
  multiplier: number;
  duration: number;
  cooldown: number;
}

export interface CollectionMethod {
  type: 'pull' | 'push' | 'stream' | 'batch' | 'hybrid';
  protocol: string;
  format: string;
  compression: CompressionConfig;
  encryption: EncryptionConfig;
  transformation: TransformationConfig[];
}

export interface CompressionConfig {
  enabled: boolean;
  algorithm: 'gzip' | 'lz4' | 'snappy' | 'zstd';
  level: number;
  threshold: number;
}

export interface EncryptionConfig {
  enabled: boolean;
  algorithm: string;
  keySize: number;
  mode: string;
  padding: string;
}

export interface TransformationConfig {
  name: string;
  type: 'filter' | 'map' | 'reduce' | 'aggregate' | 'enrich' | 'validate';
  config: Record<string, any>;
  order: number;
  enabled: boolean;
}

export interface ValidationConfig {
  rules: DataValidationRule[];
  onError: 'reject' | 'quarantine' | 'correct' | 'ignore';
  reporting: ValidationReporting;
  thresholds: ValidationThreshold[];
}

export interface DataValidationRule {
  name: string;
  type: 'schema' | 'range' | 'format' | 'business' | 'consistency';
  rule: string;
  severity: 'error' | 'warning' | 'info';
  enabled: boolean;
  description: string;
}

export interface ValidationReporting {
  enabled: boolean;
  frequency: string;
  recipients: string[];
  format: string;
  retention: number;
}

export interface ValidationThreshold {
  rule: string;
  threshold: number;
  action: 'alert' | 'stop' | 'degrade';
  window: number;
}

export interface EnrichmentConfig {
  sources: EnrichmentSource[];
  rules: EnrichmentRule[];
  caching: EnrichmentCaching;
  fallback: FallbackConfig;
}

export interface EnrichmentSource {
  name: string;
  type: 'lookup' | 'api' | 'ml' | 'calculation';
  config: Record<string, any>;
  priority: number;
  timeout: number;
  reliability: number;
}

export interface EnrichmentRule {
  name: string;
  condition: string;
  enrichments: string[];
  priority: number;
  enabled: boolean;
}

export interface EnrichmentCaching {
  enabled: boolean;
  ttl: number;
  maxSize: number;
  strategy: string;
}

export interface FallbackConfig {
  enabled: boolean;
  strategy: 'default' | 'previous' | 'interpolate' | 'skip';
  values: Record<string, any>;
  timeout: number;
}

export interface ProcessingPipeline {
  stages: PipelineStage[];
  parallelism: number;
  batching: BatchingConfig;
  errorHandling: ErrorHandlingConfig;
  monitoring: PipelineMonitoring;
}

export interface PipelineStage {
  name: string;
  type: 'transform' | 'validate' | 'enrich' | 'aggregate' | 'filter';
  config: Record<string, any>;
  parallelism: number;
  timeout: number;
  retries: number;
  enabled: boolean;
}

export interface BatchingConfig {
  enabled: boolean;
  size: number;
  timeout: number;
  strategy: 'size' | 'time' | 'adaptive';
}

export interface ErrorHandlingConfig {
  strategy: 'fail_fast' | 'continue' | 'circuit_breaker';
  retries: number;
  backoff: 'linear' | 'exponential' | 'random';
  deadLetter: boolean;
  notification: string[];
}

export interface PipelineMonitoring {
  enabled: boolean;
  metrics: string[];
  alerts: string[];
  dashboard: string;
  retention: number;
}

export interface BackupConfig {
  enabled: boolean;
  frequency: string;
  retention: number;
  compression: boolean;
  encryption: boolean;
  verification: boolean;
  storage: StorageConfig;
}

export interface StorageConfig {
  type: 'local' | 'cloud' | 'distributed';
  location: string;
  redundancy: number;
  consistency: string;
  durability: number;
}

export interface MetricRetention {
  policy: RetentionPolicy;
  archival: ArchivalConfig;
  compression: RetentionCompression;
  lifecycle: LifecycleRule[];
}

export interface RetentionPolicy {
  rawData: RetentionLevel;
  aggregated: RetentionLevel[];
  metadata: RetentionLevel;
  alerts: RetentionLevel;
}

export interface RetentionLevel {
  granularity: TimeGranularity;
  duration: number;
  storage: string;
  compression: number;
  access: 'hot' | 'warm' | 'cold' | 'archive';
}

export interface ArchivalConfig {
  enabled: boolean;
  threshold: number;
  destination: string;
  format: string;
  compression: boolean;
  encryption: boolean;
  verification: boolean;
}

export interface RetentionCompression {
  algorithm: string;
  level: number;
  threshold: number;
  savings: number;
}

export interface LifecycleRule {
  name: string;
  condition: string;
  action: 'archive' | 'delete' | 'compress' | 'migrate';
  schedule: string;
  enabled: boolean;
}

export interface MetricAlert {
  id: string;
  name: string;
  description: string;
  condition: AlertCondition;
  severity: 'info' | 'warning' | 'critical' | 'emergency';
  channels: AlertChannel[];
  suppression: AlertSuppression;
  escalation: AlertEscalation;
  history: AlertTrigger[];
  enabled: boolean;
}

export interface AlertCondition {
  expression: string;
  threshold: number;
  operator: string;
  duration: number;
  evaluation: EvaluationConfig;
  dependencies: string[];
}

export interface EvaluationConfig {
  frequency: number;
  window: number;
  aggregation: string;
  interpolation: boolean;
  missingData: 'ignore' | 'treat_as_zero' | 'no_data';
}

export interface AlertChannel {
  type: 'email' | 'slack' | 'webhook' | 'sms' | 'pagerduty';
  config: Record<string, any>;
  template: string;
  throttle: ThrottleConfig;
  enabled: boolean;
}

export interface ThrottleConfig {
  enabled: boolean;
  window: number;
  maxAlerts: number;
  strategy: 'drop' | 'aggregate' | 'delay';
}

export interface AlertSuppression {
  rules: SuppressionRule[];
  schedules: SuppressionSchedule[];
  dependencies: string[];
}

export interface SuppressionRule {
  name: string;
  condition: string;
  duration: number;
  reason: string;
  enabled: boolean;
}

export interface SuppressionSchedule {
  name: string;
  cron: string;
  timezone: string;
  duration: number;
  reason: string;
  enabled: boolean;
}

export interface AlertEscalation {
  enabled: boolean;
  levels: EscalationLevel[];
  autoResolve: boolean;
  maxLevel: number;
}

export interface EscalationLevel {
  level: number;
  delay: number;
  channels: string[];
  condition?: string;
  autoResolve: boolean;
}

export interface AlertTrigger {
  id: string;
  timestamp: Date;
  value: number;
  condition: string;
  severity: string;
  resolved: boolean;
  resolvedAt?: Date;
  duration: number;
  escalationLevel: number;
  notifications: NotificationDelivery[];
}

export interface NotificationDelivery {
  channel: string;
  timestamp: Date;
  status: 'pending' | 'delivered' | 'failed' | 'throttled';
  attempts: number;
  error?: string;
}

export interface MetricMetadata {
  version: string;
  schema: string;
  lineage: DataLineage;
  classification: DataClassification;
  governance: DataGovernance;
  quality: QualityMetadata;
  usage: UsageMetadata;
  compliance: ComplianceMetadata;
  custom: Record<string, any>;
}

export interface DataLineage {
  sources: LineageSource[];
  transformations: LineageTransformation[];
  dependencies: LineageDependency[];
  impact: LineageImpact[];
}

export interface LineageSource {
  id: string;
  name: string;
  type: string;
  location: string;
  schema: string;
  lastUpdated: Date;
  owner: string;
}

export interface LineageTransformation {
  id: string;
  name: string;
  type: string;
  logic: string;
  inputs: string[];
  outputs: string[];
  timestamp: Date;
}

export interface LineageDependency {
  upstream: string;
  downstream: string;
  type: 'direct' | 'indirect';
  strength: number;
  latency: number;
}

export interface LineageImpact {
  change: string;
  affected: string[];
  severity: 'low' | 'medium' | 'high';
  mitigation: string[];
}

export interface DataClassification {
  sensitivity: 'public' | 'internal' | 'confidential' | 'restricted' | 'top_secret';
  category: string[];
  regulations: string[];
  retention: number;
  disposal: string;
  access: AccessControl[];
}

export interface AccessControl {
  principal: string;
  permissions: string[];
  conditions: string[];
  expiry?: Date;
  justification: string;
}

export interface DataGovernance {
  owner: string;
  steward: string;
  custodian: string;
  policies: string[];
  procedures: string[];
  controls: string[];
  audits: AuditTrail[];
}

export interface AuditTrail {
  timestamp: Date;
  user: string;
  action: string;
  resource: string;
  result: string;
  details: Record<string, any>;
}

export interface QualityMetadata {
  dimensions: QualityDimension[];
  rules: QualityRule[];
  measurements: QualityMeasurement[];
  trends: QualityTrend[];
}

export interface QualityDimension {
  name: string;
  description: string;
  score: number;
  weight: number;
  threshold: number;
  status: 'pass' | 'warn' | 'fail';
}

export interface QualityRule {
  id: string;
  name: string;
  type: string;
  expression: string;
  threshold: number;
  severity: string;
  enabled: boolean;
}

export interface QualityMeasurement {
  timestamp: Date;
  dimension: string;
  score: number;
  details: Record<string, any>;
  issues: string[];
}

export interface QualityTrend {
  dimension: string;
  direction: 'improving' | 'stable' | 'degrading';
  rate: number;
  confidence: number;
  factors: string[];
}

export interface UsageMetadata {
  consumers: MetricConsumer[];
  patterns: UsagePattern[];
  performance: UsagePerformance;
  costs: UsageCost[];
}

export interface MetricConsumer {
  id: string;
  name: string;
  type: 'dashboard' | 'alert' | 'api' | 'report' | 'ml_model';
  frequency: string;
  volume: number;
  lastAccess: Date;
  criticality: 'low' | 'medium' | 'high' | 'critical';
}

export interface UsagePattern {
  type: 'temporal' | 'volume' | 'access' | 'geographic';
  pattern: string;
  strength: number;
  recurrence: string;
  anomalies: string[];
}

export interface UsagePerformance {
  latency: PerformanceMetric;
  throughput: PerformanceMetric;
  availability: PerformanceMetric;
  errors: PerformanceMetric;
}

export interface PerformanceMetric {
  current: number;
  target: number;
  trend: string;
  percentiles: Record<string, number>;
  sla: number;
}

export interface UsageCost {
  category: 'compute' | 'storage' | 'network' | 'license' | 'support';
  amount: number;
  currency: string;
  period: string;
  allocation: CostAllocation[];
  trends: CostTrend[];
}

export interface CostAllocation {
  consumer: string;
  percentage: number;
  amount: number;
  method: string;
}

export interface CostTrend {
  period: string;
  amount: number;
  change: number;
  drivers: string[];
  forecast: number;
}

export interface ComplianceMetadata {
  frameworks: ComplianceFramework[];
  requirements: ComplianceRequirement[];
  controls: ComplianceControl[];
  assessments: ComplianceAssessment[];
  violations: ComplianceViolation[];
}

export interface ComplianceFramework {
  name: string;
  version: string;
  applicability: string[];
  requirements: string[];
  lastAssessment: Date;
  nextAssessment: Date;
  status: 'compliant' | 'non_compliant' | 'pending';
}

export interface ComplianceRequirement {
  id: string;
  framework: string;
  description: string;
  criticality: 'low' | 'medium' | 'high' | 'critical';
  status: 'met' | 'partially_met' | 'not_met' | 'not_applicable';
  evidence: string[];
  gaps: string[];
}

export interface ComplianceControl {
  id: string;
  name: string;
  type: 'preventive' | 'detective' | 'corrective';
  effectiveness: number;
  testing: string;
  lastTest: Date;
  nextTest: Date;
  status: 'effective' | 'ineffective' | 'not_tested';
}

export interface ComplianceAssessment {
  id: string;
  framework: string;
  assessor: string;
  date: Date;
  scope: string[];
  findings: string[];
  recommendations: string[];
  score: number;
  status: 'in_progress' | 'completed' | 'remediation_required';
}

export interface ComplianceViolation {
  id: string;
  requirement: string;
  description: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  detected: Date;
  resolved?: Date;
  remediation: string[];
  recurrence: boolean;
}

// Dashboard and visualization interfaces
export interface SecurityDashboard {
  id: string;
  name: string;
  description: string;
  type: DashboardType;
  category: string;
  layout: DashboardLayout;
  widgets: DashboardWidget[];
  filters: DashboardFilter[];
  variables: DashboardVariable[];
  permissions: DashboardPermission[];
  sharing: DashboardSharing;
  refresh: DashboardRefresh;
  alerts: DashboardAlert[];
  metadata: DashboardMetadata;
  createdAt: Date;
  updatedAt: Date;
  createdBy: string;
  lastViewedAt: Date;
  viewCount: number;
  enabled: boolean;
}

export interface DashboardLayout {
  type: 'grid' | 'flow' | 'tabs' | 'accordion' | 'custom';
  columns: number;
  rows: number;
  spacing: number;
  responsive: boolean;
  breakpoints: LayoutBreakpoint[];
  themes: LayoutTheme[];
}

export interface LayoutBreakpoint {
  name: string;
  width: number;
  columns: number;
  spacing: number;
  hidden: string[];
}

export interface LayoutTheme {
  name: string;
  colors: ColorPalette;
  fonts: FontPalette;
  spacing: SpacingPalette;
  borders: BorderPalette;
  shadows: ShadowPalette;
}

export interface ColorPalette {
  primary: string;
  secondary: string;
  accent: string;
  background: string;
  surface: string;
  text: string;
  success: string;
  warning: string;
  error: string;
  info: string;
  neutral: string[];
}

export interface FontPalette {
  primary: FontDefinition;
  secondary: FontDefinition;
  monospace: FontDefinition;
  sizes: Record<string, number>;
  weights: Record<string, number>;
}

export interface FontDefinition {
  family: string;
  fallback: string[];
  variants: string[];
  weights: number[];
  styles: string[];
}

export interface SpacingPalette {
  xs: number;
  sm: number;
  md: number;
  lg: number;
  xl: number;
  xxl: number;
}

export interface BorderPalette {
  width: Record<string, number>;
  radius: Record<string, number>;
  style: Record<string, string>;
  color: Record<string, string>;
}

export interface ShadowPalette {
  xs: string;
  sm: string;
  md: string;
  lg: string;
  xl: string;
  inner: string;
}

export interface DashboardWidget {
  id: string;
  type: VisualizationType;
  title: string;
  description?: string;
  position: WidgetPosition;
  size: WidgetSize;
  metrics: string[];
  visualization: VisualizationConfig;
  data: WidgetData;
  interactions: WidgetInteraction[];
  alerts: WidgetAlert[];
  permissions: WidgetPermission[];
  refresh: WidgetRefresh;
  export: WidgetExport;
  metadata: WidgetMetadata;
  enabled: boolean;
}

export interface WidgetPosition {
  x: number;
  y: number;
  z: number;
  anchor: 'top-left' | 'top-right' | 'bottom-left' | 'bottom-right' | 'center';
}

export interface WidgetSize {
  width: number;
  height: number;
  minWidth: number;
  minHeight: number;
  maxWidth: number;
  maxHeight: number;
  aspectRatio?: number;
  resizable: boolean;
}

export interface VisualizationConfig {
  type: VisualizationType;
  options: VisualizationOptions;
  styling: VisualizationStyling;
  animation: AnimationConfig;
  interaction: InteractionConfig;
  accessibility: AccessibilityConfig;
}

export interface VisualizationOptions {
  // Chart-specific options
  chart?: ChartOptions;
  table?: TableOptions;
  gauge?: GaugeOptions;
  heatmap?: HeatmapOptions;
  counter?: CounterOptions;
  trend?: TrendOptions;
}

export interface ChartOptions {
  xAxis: AxisConfig;
  yAxis: AxisConfig;
  legend: LegendConfig;
  tooltip: TooltipConfig;
  zoom: ZoomConfig;
  pan: PanConfig;
  brush: BrushConfig;
  crossfilter: CrossfilterConfig;
}

export interface AxisConfig {
  type: 'linear' | 'log' | 'time' | 'category' | 'ordinal';
  domain: [number, number] | string[];
  range: [number, number];
  ticks: TickConfig;
  grid: GridConfig;
  label: LabelConfig;
  scale: ScaleConfig;
}

export interface TickConfig {
  count: number;
  format: string;
  rotation: number;
  size: number;
  color: string;
  font: FontDefinition;
}

export interface GridConfig {
  enabled: boolean;
  color: string;
  width: number;
  style: 'solid' | 'dashed' | 'dotted';
  opacity: number;
}

export interface LabelConfig {
  text: string;
  position: string;
  offset: number;
  rotation: number;
  font: FontDefinition;
  color: string;
}

export interface ScaleConfig {
  type: string;
  base: number;
  nice: boolean;
  zero: boolean;
  clamp: boolean;
}

export interface LegendConfig {
  enabled: boolean;
  position: 'top' | 'right' | 'bottom' | 'left' | 'inside';
  orientation: 'horizontal' | 'vertical';
  alignment: 'start' | 'center' | 'end';
  spacing: number;
  font: FontDefinition;
  symbols: SymbolConfig;
}

export interface SymbolConfig {
  type: 'circle' | 'square' | 'triangle' | 'diamond' | 'cross' | 'line';
  size: number;
  color: string;
  opacity: number;
}

export interface TooltipConfig {
  enabled: boolean;
  format: string;
  position: 'follow' | 'fixed';
  offset: [number, number];
  background: string;
  border: string;
  font: FontDefinition;
  animation: AnimationConfig;
}

export interface ZoomConfig {
  enabled: boolean;
  type: 'wheel' | 'drag' | 'pinch';
  extent: [number, number];
  constrain: boolean;
  interpolate: boolean;
}

export interface PanConfig {
  enabled: boolean;
  button: 'left' | 'right' | 'middle';
  extent: [number, number][];
  constrain: boolean;
  inertia: boolean;
}

export interface BrushConfig {
  enabled: boolean;
  type: 'x' | 'y' | 'xy';
  extent: [number, number][];
  handles: boolean;
  filter: boolean;
}

export interface CrossfilterConfig {
  enabled: boolean;
  dimensions: string[];
  groups: string[];
  filters: CrossfilterFilter[];
}

export interface CrossfilterFilter {
  dimension: string;
  type: 'range' | 'exact' | 'custom';
  value: any;
  active: boolean;
}

export interface TableOptions {
  columns: TableColumn[];
  pagination: PaginationOptions;
  sorting: SortingOptions;
  filtering: FilteringOptions;
  selection: SelectionOptions;
  export: ExportOptions;
}

export interface TableColumn {
  key: string;
  title: string;
  type: 'text' | 'number' | 'date' | 'boolean' | 'badge' | 'link' | 'custom';
  width: number;
  align: 'left' | 'center' | 'right';
  sortable: boolean;
  filterable: boolean;
  format: string;
  render?: (value: any, row: any) => string;
  visible: boolean;
}

export interface PaginationOptions {
  enabled: boolean;
  pageSize: number;
  pageSizes: number[];
  showTotal: boolean;
  showSizeChanger: boolean;
  position: 'top' | 'bottom' | 'both';
}

export interface SortingOptions {
  enabled: boolean;
  multiple: boolean;
  defaultSort: SortConfig[];
  serverSide: boolean;
}

export interface SortConfig {
  column: string;
  direction: 'asc' | 'desc';
  priority: number;
}

export interface FilteringOptions {
  enabled: boolean;
  position: 'header' | 'toolbar' | 'sidebar';
  global: boolean;
  serverSide: boolean;
  debounce: number;
}

export interface SelectionOptions {
  enabled: boolean;
  mode: 'single' | 'multiple';
  rowKey: string;
  preserve: boolean;
  actions: SelectionAction[];
}

export interface SelectionAction {
  key: string;
  label: string;
  icon: string;
  action: (rows: any[]) => void;
  disabled?: (rows: any[]) => boolean;
}

export interface ExportOptions {
  enabled: boolean;
  formats: ExportFormat[];
  filename: string;
  includeHeaders: boolean;
  visibleOnly: boolean;
}

export interface ExportFormat {
  type: 'csv' | 'excel' | 'pdf' | 'json' | 'xml';
  label: string;
  options: Record<string, any>;
}

export interface GaugeOptions {
  min: number;
  max: number;
  target?: number;
  thresholds: GaugeThreshold[];
  needle: NeedleConfig;
  arc: ArcConfig;
  labels: GaugeLabelConfig;
}

export interface GaugeThreshold {
  value: number;
  color: string;
  label?: string;
}

export interface NeedleConfig {
  color: string;
  width: number;
  length: number;
  cap: CapConfig;
}

export interface CapConfig {
  radius: number;
  color: string;
  border: string;
}

export interface ArcConfig {
  startAngle: number;
  endAngle: number;
  innerRadius: number;
  outerRadius: number;
  cornerRadius: number;
}

export interface GaugeLabelConfig {
  show: boolean;
  format: string;
  font: FontDefinition;
  color: string;
  position: 'inside' | 'outside';
}

export interface HeatmapOptions {
  colorScale: ColorScale;
  cells: CellConfig;
  axes: HeatmapAxes;
  clustering: ClusteringConfig;
}

export interface ColorScale {
  type: 'linear' | 'log' | 'sqrt' | 'quantile' | 'threshold';
  scheme: string;
  domain: [number, number];
  range: string[];
  interpolate: string;
  reverse: boolean;
}

export interface CellConfig {
  shape: 'rectangle' | 'circle' | 'hexagon';
  size: number;
  spacing: number;
  border: BorderConfig;
  hover: HoverConfig;
}

export interface BorderConfig {
  width: number;
  color: string;
  style: string;
  radius: number;
}

export interface HoverConfig {
  enabled: boolean;
  color: string;
  opacity: number;
  scale: number;
  tooltip: boolean;
}

export interface HeatmapAxes {
  x: HeatmapAxis;
  y: HeatmapAxis;
}

export interface HeatmapAxis {
  labels: string[];
  title: string;
  rotate: number;
  spacing: number;
  font: FontDefinition;
}

export interface ClusteringConfig {
  enabled: boolean;
  method: 'hierarchical' | 'kmeans' | 'dbscan';
  distance: 'euclidean' | 'manhattan' | 'cosine';
  linkage: 'single' | 'complete' | 'average' | 'ward';
  dendogram: boolean;
}

export interface CounterOptions {
  format: CounterFormat;
  comparison: ComparisonConfig;
  trend: CounterTrendConfig;
  threshold: CounterThresholdConfig;
}

export interface CounterFormat {
  type: 'number' | 'percentage' | 'currency' | 'duration' | 'bytes';
  precision: number;
  prefix: string;
  suffix: string;
  separator: string;
  locale: string;
}

export interface ComparisonConfig {
  enabled: boolean;
  type: 'previous' | 'target' | 'baseline';
  format: CounterFormat;
  showDifference: boolean;
  showPercentage: boolean;
  colors: ComparisonColors;
}

export interface ComparisonColors {
  positive: string;
  negative: string;
  neutral: string;
}

export interface CounterTrendConfig {
  enabled: boolean;
  period: string;
  sparkline: SparklineConfig;
  direction: DirectionConfig;
}

export interface SparklineConfig {
  enabled: boolean;
  height: number;
  color: string;
  area: boolean;
  smooth: boolean;
  points: boolean;
}

export interface DirectionConfig {
  enabled: boolean;
  icons: DirectionIcons;
  colors: DirectionColors;
}

export interface DirectionIcons {
  up: string;
  down: string;
  stable: string;
}

export interface DirectionColors {
  up: string;
  down: string;
  stable: string;
}

export interface CounterThresholdConfig {
  enabled: boolean;
  thresholds: CounterThreshold[];
  visualization: ThresholdVisualization;
}

export interface CounterThreshold {
  value: number;
  operator: 'gt' | 'gte' | 'lt' | 'lte' | 'eq';
  color: string;
  label: string;
  severity: string;
}

export interface ThresholdVisualization {
  type: 'color' | 'icon' | 'border' | 'background';
  position: 'value' | 'container' | 'indicator';
  animate: boolean;
}

export interface TrendOptions {
  timeRange: TimeRangeConfig;
  aggregation: TrendAggregation;
  smoothing: SmoothingConfig;
  forecasting: ForecastingConfig;
  annotations: AnnotationConfig[];
}

export interface TimeRangeConfig {
  start: Date | string;
  end: Date | string;
  relative: boolean;
  selector: boolean;
  presets: TimePreset[];
}

export interface TimePreset {
  label: string;
  value: string;
  relative: boolean;
  start: string;
  end: string;
}

export interface TrendAggregation {
  function: 'sum' | 'avg' | 'min' | 'max' | 'count' | 'rate';
  interval: string;
  offset: string;
  timezone: string;
}

export interface SmoothingConfig {
  enabled: boolean;
  method: 'moving_average' | 'exponential' | 'lowess' | 'spline';
  window: number;
  alpha: number;
}

export interface ForecastingConfig {
  enabled: boolean;
  method: 'linear' | 'arima' | 'exponential' | 'ml';
  horizon: number;
  confidence: number;
  intervals: boolean;
}

export interface AnnotationConfig {
  type: 'point' | 'line' | 'band' | 'text';
  value: any;
  label: string;
  color: string;
  style: string;
  position: string;
}

export interface VisualizationStyling {
  theme: string;
  colors: ColorConfig;
  fonts: FontConfig;
  spacing: SpacingConfig;
  borders: BorderStyling;
  shadows: ShadowConfig;
  background: BackgroundConfig;
}

export interface ColorConfig {
  scheme: string;
  palette: string[];
  opacity: number;
  gradients: GradientConfig[];
  overrides: ColorOverride[];
}

export interface GradientConfig {
  name: string;
  type: 'linear' | 'radial' | 'conic';
  direction: string;
  stops: GradientStop[];
}

export interface GradientStop {
  offset: number;
  color: string;
  opacity: number;
}

export interface ColorOverride {
  condition: string;
  color: string;
  scope: 'series' | 'point' | 'area' | 'line';
}

export interface FontConfig {
  family: string;
  size: number;
  weight: string;
  style: string;
  lineHeight: number;
  letterSpacing: number;
}

export interface SpacingConfig {
  padding: SpacingValue;
  margin: SpacingValue;
  gap: SpacingValue;
}

export interface SpacingValue {
  top: number;
  right: number;
  bottom: number;
  left: number;
}

export interface BorderStyling {
  width: number;
  style: string;
  color: string;
  radius: number;
  opacity: number;
}

export interface ShadowConfig {
  enabled: boolean;
  x: number;
  y: number;
  blur: number;
  spread: number;
  color: string;
  opacity: number;
}

export interface BackgroundConfig {
  type: 'solid' | 'gradient' | 'pattern' | 'image';
  value: string;
  opacity: number;
  repeat: string;
  position: string;
  size: string;
}

export interface AnimationConfig {
  enabled: boolean;
  duration: number;
  easing: string;
  delay: number;
  stagger: number;
  loop: boolean;
  direction: 'normal' | 'reverse' | 'alternate';
}

export interface InteractionConfig {
  hover: HoverInteraction;
  click: ClickInteraction;
  selection: SelectionInteraction;
  zoom: ZoomInteraction;
  pan: PanInteraction;
  brush: BrushInteraction;
}

export interface HoverInteraction {
  enabled: boolean;
  highlight: boolean;
  tooltip: boolean;
  crosshair: boolean;
  feedback: FeedbackConfig;
}

export interface ClickInteraction {
  enabled: boolean;
  action: 'select' | 'filter' | 'drill' | 'navigate' | 'custom';
  target: string;
  parameters: Record<string, any>;
  feedback: FeedbackConfig;
}

export interface SelectionInteraction {
  enabled: boolean;
  mode: 'single' | 'multiple' | 'range';
  persist: boolean;
  clear: boolean;
  feedback: FeedbackConfig;
}

export interface ZoomInteraction {
  enabled: boolean;
  type: 'wheel' | 'drag' | 'pinch' | 'double_click';
  axis: 'x' | 'y' | 'xy';
  extent: [number, number][];
  reset: boolean;
}

export interface PanInteraction {
  enabled: boolean;
  button: 'left' | 'right' | 'middle';
  axis: 'x' | 'y' | 'xy';
  extent: [number, number][];
  inertia: boolean;
}

export interface BrushInteraction {
  enabled: boolean;
  axis: 'x' | 'y' | 'xy';
  extent: [number, number][];
  handles: boolean;
  filter: boolean;
  clear: boolean;
}

export interface FeedbackConfig {
  visual: VisualFeedback;
  haptic: HapticFeedback;
  audio: AudioFeedback;
}

export interface VisualFeedback {
  enabled: boolean;
  type: 'highlight' | 'outline' | 'scale' | 'color' | 'opacity';
  duration: number;
  intensity: number;
}

export interface HapticFeedback {
  enabled: boolean;
  pattern: 'click' | 'light' | 'medium' | 'heavy' | 'custom';
  duration: number;
}

export interface AudioFeedback {
  enabled: boolean;
  sound: string;
  volume: number;
  pitch: number;
}

export interface AccessibilityConfig {
  enabled: boolean;
  labels: AccessibilityLabels;
  keyboard: KeyboardConfig;
  screen: ScreenReaderConfig;
  contrast: ContrastConfig;
  motion: MotionConfig;
}

export interface AccessibilityLabels {
  title: string;
  description: string;
  axes: AxisLabels;
  series: SeriesLabels[];
  navigation: NavigationLabels;
}

export interface AxisLabels {
  x: string;
  y: string;
  units: string;
  format: string;
}

export interface SeriesLabels {
  name: string;
  description: string;
  color: string;
  pattern: string;
}

export interface NavigationLabels {
  previous: string;
  next: string;
  play: string;
  pause: string;
  reset: string;
}

export interface KeyboardConfig {
  enabled: boolean;
  shortcuts: KeyboardShortcut[];
  navigation: KeyboardNavigation;
  focus: FocusConfig;
}

export interface KeyboardShortcut {
  keys: string[];
  action: string;
  description: string;
  global: boolean;
}

export interface KeyboardNavigation {
  enabled: boolean;
  mode: 'tab' | 'arrow' | 'grid';
  wrap: boolean;
  skip: string[];
}

export interface FocusConfig {
  enabled: boolean;
  style: FocusStyle;
  indicator: FocusIndicator;
  trap: boolean;
}

export interface FocusStyle {
  outline: string;
  background: string;
  opacity: number;
  scale: number;
}

export interface FocusIndicator {
  type: 'outline' | 'shadow' | 'border' | 'background';
  color: string;
  width: number;
  style: string;
}

export interface ScreenReaderConfig {
  enabled: boolean;
  live: 'off' | 'polite' | 'assertive';
  atomic: boolean;
  descriptions: boolean;
  summaries: boolean;
}

export interface ContrastConfig {
  enabled: boolean;
  ratio: number;
  colors: HighContrastColors;
  patterns: ContrastPatterns;
}

export interface HighContrastColors {
  background: string;
  foreground: string;
  accent: string;
  success: string;
  warning: string;
  error: string;
}

export interface ContrastPatterns {
  enabled: boolean;
  types: PatternType[];
  size: number;
  opacity: number;
}

export interface PatternType {
  name: string;
  pattern: string;
  usage: string;
}

export interface MotionConfig {
  enabled: boolean;
  reduced: boolean;
  alternatives: MotionAlternative[];
}

export interface MotionAlternative {
  type: 'static' | 'fade' | 'slide' | 'scale';
  duration: number;
  easing: string;
}

export interface WidgetData {
  source: DataSourceConfig;
  query: QueryDefinition;
  transform: DataTransform[];
  cache: CacheConfig;
  refresh: RefreshConfig;
  fallback: FallbackData;
}

export interface DataSourceConfig {
  type: 'metric' | 'api' | 'static' | 'computed';
  connection: string;
  authentication: AuthConfig;
  timeout: number;
  retries: number;
}

export interface AuthConfig {
  type: 'none' | 'basic' | 'bearer' | 'oauth2' | 'api_key';
  credentials: Record<string, string>;
  refresh: boolean;
  cache: boolean;
}

export interface QueryDefinition {
  metrics: string[];
  filters: QueryFilter[];
  groupBy: string[];
  aggregation: QueryAggregation;
  timeRange: QueryTimeRange;
  limit: number;
  offset: number;
}

export interface QueryFilter {
  field: string;
  operator: 'eq' | 'ne' | 'gt' | 'gte' | 'lt' | 'lte' | 'in' | 'nin' | 'contains' | 'regex';
  value: any;
  caseSensitive: boolean;
}

export interface QueryAggregation {
  function: string;
  interval: string;
  offset: string;
  timezone: string;
  fillMode: 'null' | 'zero' | 'previous' | 'linear';
}

export interface QueryTimeRange {
  start: Date | string;
  end: Date | string;
  relative: boolean;
  timezone: string;
}

export interface DataTransform {
  type: 'filter' | 'map' | 'reduce' | 'sort' | 'group' | 'join' | 'pivot' | 'custom';
  config: Record<string, any>;
  order: number;
  enabled: boolean;
}

export interface CacheConfig {
  enabled: boolean;
  ttl: number;
  key: string;
  strategy: 'memory' | 'disk' | 'redis' | 'distributed';
  invalidation: string[];
}

export interface FallbackData {
  enabled: boolean;
  data: any;
  timeout: number;
  message: string;
}

export interface WidgetInteraction {
  type: 'click' | 'hover' | 'select' | 'filter' | 'drill' | 'export';
  trigger: InteractionTrigger;
  action: InteractionAction;
  conditions: InteractionCondition[];
  feedback: InteractionFeedback;
  enabled: boolean;
}

export interface InteractionTrigger {
  element: 'chart' | 'legend' | 'axis' | 'data_point' | 'area' | 'title';
  event: 'click' | 'double_click' | 'right_click' | 'hover' | 'key_press';
  modifier: 'none' | 'ctrl' | 'shift' | 'alt' | 'meta';
  debounce: number;
}

export interface InteractionAction {
  type: 'navigate' | 'filter' | 'drill' | 'export' | 'alert' | 'custom';
  target: string;
  parameters: ActionParameter[];
  async: boolean;
}

export interface ActionParameter {
  name: string;
  source: 'static' | 'data' | 'context' | 'user' | 'computed';
  value: any;
  transform: string;
}

export interface InteractionCondition {
  field: string;
  operator: string;
  value: any;
  required: boolean;
}

export interface InteractionFeedback {
  visual: boolean;
  haptic: boolean;
  audio: boolean;
  message: string;
  duration: number;
}

export interface WidgetAlert {
  id: string;
  name: string;
  condition: AlertConditionConfig;
  notification: AlertNotificationConfig;
  suppression: AlertSuppressionConfig;
  escalation: AlertEscalationConfig;
  enabled: boolean;
}

export interface AlertConditionConfig {
  metric: string;
  operator: string;
  threshold: number;
  duration: number;
  aggregation: string;
  evaluation: string;
}

export interface AlertNotificationConfig {
  channels: string[];
  template: string;
  throttle: number;
  grouping: string[];
}

export interface AlertSuppressionConfig {
  rules: string[];
  schedules: string[];
  duration: number;
}

export interface AlertEscalationConfig {
  enabled: boolean;
  delay: number;
  levels: string[];
  autoResolve: boolean;
}

export interface WidgetPermission {
  principal: string;
  type: 'user' | 'role' | 'group';
  permissions: string[];
  conditions: string[];
  inherited: boolean;
}

export interface WidgetRefresh {
  enabled: boolean;
  interval: number;
  manual: boolean;
  onVisible: boolean;
  onFocus: boolean;
  strategy: 'replace' | 'append' | 'merge';
}

export interface WidgetExport {
  enabled: boolean;
  formats: string[];
  include: string[];
  template: string;
  filename: string;
  watermark: boolean;
}

export interface WidgetMetadata {
  version: string;
  author: string;
  tags: string[];
  category: string;
  dependencies: string[];
  performance: PerformanceInfo;
  usage: WidgetUsage;
}

export interface PerformanceInfo {
  renderTime: number;
  dataSize: number;
  memoryUsage: number;
  updateFrequency: number;
  cacheHitRate: number;
}

export interface WidgetUsage {
  views: number;
  interactions: number;
  exports: number;
  errors: number;
  lastAccessed: Date;
}

export interface DashboardFilter {
  id: string;
  name: string;
  type: 'dropdown' | 'multiselect' | 'date_range' | 'text' | 'number' | 'boolean';
  field: string;
  options: FilterOption[];
  default: any;
  required: boolean;
  hidden: boolean;
  dependencies: FilterDependency[];
}

export interface FilterOption {
  label: string;
  value: any;
  description?: string;
  disabled: boolean;
  group?: string;
}

export interface FilterDependency {
  filter: string;
  condition: string;
  action: 'show' | 'hide' | 'enable' | 'disable' | 'update_options';
}

export interface DashboardVariable {
  id: string;
  name: string;
  type: 'query' | 'static' | 'computed' | 'global';
  value: any;
  query?: string;
  refresh: boolean;
  cascade: boolean;
  hidden: boolean;
}

export interface DashboardPermission {
  principal: string;
  type: 'user' | 'role' | 'group';
  permissions: DashboardPermissionType[];
  conditions: string[];
  inherited: boolean;
}

export type DashboardPermissionType = 'view' | 'edit' | 'delete' | 'share' | 'export' | 'admin';

export interface DashboardSharing {
  enabled: boolean;
  public: boolean;
  anonymous: boolean;
  expiry?: Date;
  password?: string;
  permissions: string[];
  tracking: boolean;
}

export interface DashboardRefresh {
  enabled: boolean;
  interval: number;
  autoStart: boolean;
  pauseOnInactive: boolean;
  showProgress: boolean;
  strategy: 'full' | 'incremental' | 'smart';
}

export interface DashboardAlert {
  id: string;
  name: string;
  widgets: string[];
  condition: string;
  notification: DashboardNotification;
  enabled: boolean;
}

export interface DashboardNotification {
  channels: string[];
  template: string;
  frequency: string;
  recipients: string[];
}

export interface DashboardMetadata {
  version: string;
  template: string;
  tags: string[];
  category: string;
  organization: string;
  team: string;
  project: string;
  environment: string;
  dependencies: string[];
  changelog: ChangelogEntry[];
  performance: DashboardPerformance;
  usage: DashboardUsage;
}

export interface ChangelogEntry {
  version: string;
  date: Date;
  author: string;
  changes: ChangeEntry[];
}

export interface ChangeEntry {
  type: 'added' | 'changed' | 'deprecated' | 'removed' | 'fixed' | 'security';
  description: string;
  impact: 'low' | 'medium' | 'high';
}

export interface DashboardPerformance {
  loadTime: number;
  renderTime: number;
  dataSize: number;
  widgetCount: number;
  cacheHitRate: number;
  errorRate: number;
}

export interface DashboardUsage {
  views: number;
  uniqueViewers: number;
  avgDuration: number;
  interactions: number;
  exports: number;
  shares: number;
  lastViewed: Date;
  popularity: number;
}

// Report and export interfaces
export interface SecurityReport {
  id: string;
  name: string;
  description: string;
  type: 'scheduled' | 'ad_hoc' | 'alert_driven' | 'compliance' | 'executive';
  format: 'pdf' | 'html' | 'excel' | 'csv' | 'json' | 'xml';
  template: ReportTemplate;
  data: ReportData;
  schedule: ReportSchedule;
  distribution: ReportDistribution;
  generation: ReportGeneration;
  metadata: ReportMetadata;
  createdAt: Date;
  updatedAt: Date;
  lastGenerated?: Date;
  nextGeneration?: Date;
  enabled: boolean;
}

export interface ReportTemplate {
  id: string;
  name: string;
  layout: ReportLayout;
  sections: ReportSection[];
  styling: ReportStyling;
  variables: ReportVariable[];
  localization: ReportLocalization;
}

export interface ReportLayout {
  type: 'portrait' | 'landscape';
  size: 'a4' | 'letter' | 'legal' | 'tabloid' | 'custom';
  margins: ReportMargins;
  header: ReportHeader;
  footer: ReportFooter;
  watermark: ReportWatermark;
}

export interface ReportMargins {
  top: number;
  right: number;
  bottom: number;
  left: number;
  gutter: number;
}

export interface ReportHeader {
  enabled: boolean;
  height: number;
  content: HeaderContent[];
  style: HeaderStyle;
}

export interface HeaderContent {
  type: 'text' | 'image' | 'date' | 'page' | 'variable';
  value: string;
  position: 'left' | 'center' | 'right';
  style: ContentStyle;
}

export interface ContentStyle {
  font: FontDefinition;
  color: string;
  align: string;
  padding: SpacingValue;
}

export interface HeaderStyle {
  background: string;
  border: BorderStyling;
  shadow: ShadowConfig;
}

export interface ReportFooter {
  enabled: boolean;
  height: number;
  content: FooterContent[];
  style: FooterStyle;
}

export interface FooterContent {
  type: 'text' | 'image' | 'date' | 'page' | 'variable';
  value: string;
  position: 'left' | 'center' | 'right';
  style: ContentStyle;
}

export interface FooterStyle {
  background: string;
  border: BorderStyling;
  shadow: ShadowConfig;
}

export interface ReportWatermark {
  enabled: boolean;
  text: string;
  image?: string;
  opacity: number;
  rotation: number;
  position: 'center' | 'diagonal' | 'custom';
  style: WatermarkStyle;
}

export interface WatermarkStyle {
  font: FontDefinition;
  color: string;
  size: number;
  outline: boolean;
}

export interface ReportSection {
  id: string;
  name: string;
  title: string;
  type: 'summary' | 'metrics' | 'charts' | 'tables' | 'text' | 'custom';
  order: number;
  content: SectionContentItem[];
  layout: SectionLayout;
  styling: SectionStyling;
  conditions: SectionCondition[];
  pagination: SectionPagination;
  enabled: boolean;
}

export interface SectionContentItem {
  type: 'text' | 'metric' | 'chart' | 'table' | 'image' | 'widget' | 'html';
  source: string;
  config: ContentConfig;
  layout: ContentLayout;
  styling: ContentStyling;
  conditions: ContentCondition[];
}

export interface ContentConfig {
  data: ContentData;
  formatting: ContentFormatting;
  transformation: ContentTransformation[];
  validation: ContentValidation;
}

export interface ContentData {
  source: string;
  query: string;
  parameters: Record<string, any>;
  cache: boolean;
  timeout: number;
}

export interface ContentFormatting {
  numbers: NumberFormatting;
  dates: DateFormatting;
  text: TextFormatting;
  colors: ColorFormatting;
}

export interface NumberFormatting {
  decimals: number;
  thousands: string;
  currency: string;
  percentage: boolean;
  scientific: boolean;
  prefix: string;
  suffix: string;
}

export interface DateFormatting {
  format: string;
  timezone: string;
  locale: string;
  relative: boolean;
}

export interface TextFormatting {
  case: 'none' | 'upper' | 'lower' | 'title' | 'sentence';
  truncate: number;
  ellipsis: string;
  lineBreaks: boolean;
  htmlEscape: boolean;
}

export interface ColorFormatting {
  scheme: string;
  conditional: ConditionalColor[];
  thresholds: ColorThreshold[];
}

export interface ConditionalColor {
  condition: string;
  color: string;
  background: string;
  scope: 'cell' | 'row' | 'column' | 'text';
}

export interface ColorThreshold {
  value: number;
  operator: string;
  color: string;
  background: string;
}

export interface ContentTransformation {
  type: 'aggregate' | 'filter' | 'sort' | 'group' | 'pivot' | 'join';
  config: Record<string, any>;
  order: number;
}

export interface ContentValidation {
  rules: string[];
  onError: 'skip' | 'placeholder' | 'error' | 'empty';
  placeholder: string;
}

export interface ContentLayout {
  position: PositionConfig;
  size: SizeConfig;
  spacing: SpacingConfig;
  alignment: AlignmentConfig;
  overflow: OverflowConfig;
}

export interface PositionConfig {
  x: number;
  y: number;
  z: number;
  relative: boolean;
  anchor: string;
}

export interface SizeConfig {
  width: number | string;
  height: number | string;
  minWidth: number;
  minHeight: number;
  maxWidth: number;
  maxHeight: number;
  aspectRatio: number;
}

export interface AlignmentConfig {
  horizontal: 'left' | 'center' | 'right' | 'justify';
  vertical: 'top' | 'middle' | 'bottom' | 'baseline';
}

export interface OverflowConfig {
  horizontal: 'visible' | 'hidden' | 'scroll' | 'auto';
  vertical: 'visible' | 'hidden' | 'scroll' | 'auto';
  wrap: boolean;
}

export interface ContentStyling {
  background: BackgroundConfig;
  border: BorderStyling;
  shadow: ShadowConfig;
  opacity: number;
  visibility: 'visible' | 'hidden' | 'collapse';
}

export interface ContentCondition {
  expression: string;
  action: 'show' | 'hide' | 'highlight' | 'modify';
  parameters: Record<string, any>;
}

export interface SectionLayout {
  columns: number;
  rows: number;
  flow: 'horizontal' | 'vertical' | 'grid';
  spacing: SpacingConfig;
  breakPage: boolean;
}

export interface SectionStyling {
  background: BackgroundConfig;
  border: BorderStyling;
  padding: SpacingValue;
  margin: SpacingValue;
}

export interface SectionCondition {
  expression: string;
  action: 'include' | 'exclude' | 'conditional';
  parameters: Record<string, any>;
}

export interface SectionPagination {
  enabled: boolean;
  breakBefore: boolean;
  breakAfter: boolean;
  keepTogether: boolean;
  orphans: number;
  widows: number;
}

export interface ReportStyling {
  theme: string;
  colors: ColorPalette;
  fonts: FontPalette;
  spacing: SpacingPalette;
  branding: BrandingConfig;
  customCSS: string;
}

export interface BrandingConfig {
  logo: LogoConfig;
  colors: BrandColors;
  fonts: BrandFonts;
  footer: BrandFooter;
}

export interface LogoConfig {
  enabled: boolean;
  url: string;
  position: 'header' | 'footer' | 'watermark' | 'cover';
  size: SizeConfig;
  alignment: string;
}

export interface BrandColors {
  primary: string;
  secondary: string;
  accent: string;
  text: string;
  background: string;
}

export interface BrandFonts {
  primary: string;
  secondary: string;
  monospace: string;
}

export interface BrandFooter {
  enabled: boolean;
  text: string;
  links: BrandLink[];
  disclaimer: string;
}

export interface BrandLink {
  text: string;
  url: string;
  icon?: string;
}

export interface ReportVariable {
  name: string;
  type: 'string' | 'number' | 'date' | 'boolean' | 'array' | 'object';
  value: any;
  source: 'static' | 'parameter' | 'computed' | 'environment';
  format?: string;
  description: string;
}

export interface ReportLocalization {
  enabled: boolean;
  default: string;
  supported: string[];
  translations: Record<string, Record<string, string>>;
  dateFormats: Record<string, string>;
  numberFormats: Record<string, NumberFormatting>;
}

export interface ReportData {
  sources: ReportDataSource[];
  queries: ReportQuery[];
  transformations: ReportTransformation[];
  cache: ReportCacheConfig;
  validation: ReportValidationConfig;
}

export interface ReportDataSource {
  id: string;
  name: string;
  type: 'metrics' | 'database' | 'api' | 'file' | 'computed';
  connection: DataSourceConnection;
  schema: DataSchema;
  access: DataAccess;
}

export interface DataSourceConnection {
  url: string;
  credentials: Record<string, string>;
  timeout: number;
  pool: PoolConfig;
  ssl: SSLConfig;
}

export interface SSLConfig {
  enabled: boolean;
  cert: string;
  key: string;
  ca: string;
  verify: boolean;
}

export interface DataSchema {
  tables: TableSchema[];
  views: ViewSchema[];
  functions: FunctionSchema[];
  metadata: SchemaMetadata;
}

export interface TableSchema {
  name: string;
  columns: ColumnSchema[];
  indexes: IndexSchema[];
  constraints: ConstraintSchema[];
}

export interface ColumnSchema {
  name: string;
  type: string;
  nullable: boolean;
  default: any;
  description: string;
}

export interface IndexSchema {
  name: string;
  columns: string[];
  unique: boolean;
  type: string;
}

export interface ConstraintSchema {
  name: string;
  type: 'primary' | 'foreign' | 'unique' | 'check';
  columns: string[];
  reference?: TableReference;
}

export interface TableReference {
  table: string;
  columns: string[];
  onDelete: string;
  onUpdate: string;
}

export interface ViewSchema {
  name: string;
  definition: string;
  columns: ColumnSchema[];
  dependencies: string[];
}

export interface FunctionSchema {
  name: string;
  parameters: ParameterSchema[];
  returnType: string;
  language: string;
  definition: string;
}

export interface ParameterSchema {
  name: string;
  type: string;
  mode: 'in' | 'out' | 'inout';
  default: any;
}

export interface SchemaMetadata {
  version: string;
  description: string;
  tags: string[];
  owner: string;
  created: Date;
  updated: Date;
}

export interface DataAccess {
  permissions: DataPermission[];
  restrictions: DataRestriction[];
  audit: boolean;
  encryption: boolean;
}

export interface DataPermission {
  principal: string;
  operations: string[];
  resources: string[];
  conditions: string[];
}

export interface DataRestriction {
  type: 'row' | 'column' | 'value' | 'time';
  condition: string;
  action: 'filter' | 'mask' | 'deny';
  parameters: Record<string, any>;
}

export interface ReportQuery {
  id: string;
  name: string;
  source: string;
  statement: string;
  parameters: QueryParameter[];
  optimization: QueryOptimization;
  cache: QueryCacheConfig;
}

export interface QueryCacheConfig {
  enabled: boolean;
  ttl: number;
  key: string;
  invalidation: string[];
  compression: boolean;
}

export interface ReportTransformation {
  id: string;
  name: string;
  type: 'aggregate' | 'filter' | 'join' | 'pivot' | 'calculate' | 'custom';
  input: string[];
  output: string;
  logic: TransformationLogic;
  validation: TransformationValidation;
}

export interface TransformationLogic {
  expression: string;
  language: 'sql' | 'javascript' | 'python' | 'r';
  libraries: string[];
  environment: Record<string, any>;
}

export interface TransformationValidation {
  schema: string;
  rules: string[];
  onError: 'fail' | 'skip' | 'default';
  defaultValue: any;
}

export interface ReportCacheConfig {
  enabled: boolean;
  strategy: 'time' | 'dependency' | 'manual';
  ttl: number;
  maxSize: number;
  compression: boolean;
  encryption: boolean;
}

export interface ReportValidationConfig {
  enabled: boolean;
  rules: ReportValidationRule[];
  onError: 'fail' | 'warn' | 'continue';
  reporting: boolean;
}

export interface ReportValidationRule {
  name: string;
  type: 'schema' | 'business' | 'quality' | 'security';
  condition: string;
  severity: 'error' | 'warning' | 'info';
  message: string;
}

export interface ReportSchedule {
  enabled: boolean;
  frequency: 'once' | 'daily' | 'weekly' | 'monthly' | 'quarterly' | 'yearly' | 'custom';
  cron?: string;
  timezone: string;
  startDate: Date;
  endDate?: Date;
  exceptions: ScheduleException[];
  retry: RetryConfig;
}

export interface ScheduleException {
  date: Date;
  action: 'skip' | 'delay' | 'advance';
  reason: string;
}

export interface RetryConfig {
  enabled: boolean;
  attempts: number;
  delay: number;
  backoff: 'linear' | 'exponential';
  conditions: string[];
}

export interface ReportDistribution {
  enabled: boolean;
  channels: DistributionChannel[];
  recipients: ReportRecipient[];
  conditions: DistributionCondition[];
  tracking: DistributionTracking;
}

export interface DistributionChannel {
  type: 'email' | 'slack' | 'teams' | 'webhook' | 'sftp' | 'api' | 'portal';
  config: ChannelConfig;
  template: ChannelTemplate;
  retry: RetryConfig;
  enabled: boolean;
}

export interface ChannelConfig {
  endpoint: string;
  credentials: Record<string, string>;
  headers: Record<string, string>;
  timeout: number;
  compression: boolean;
  encryption: boolean;
}

export interface ChannelTemplate {
  subject: string;
  body: string;
  format: 'text' | 'html' | 'markdown';
  attachments: AttachmentConfig[];
  variables: TemplateVariable[];
}

export interface AttachmentConfig {
  name: string;
  type: 'report' | 'data' | 'summary';
  format: string;
  compression: boolean;
  encryption: boolean;
}

export interface TemplateVariable {
  name: string;
  source: string;
  format?: string;
  default?: any;
}

export interface ReportRecipient {
  id: string;
  name: string;
  email: string;
  role: string;
  permissions: string[];
  preferences: RecipientPreferences;
  active: boolean;
}

export interface RecipientPreferences {
  format: string[];
  frequency: string;
  time: string;
  timezone: string;
  language: string;
  notifications: boolean;
}

export interface DistributionCondition {
  expression: string;
  action: 'send' | 'skip' | 'modify';
  parameters: Record<string, any>;
}

export interface DistributionTracking {
  enabled: boolean;
  delivery: boolean;
  opens: boolean;
  clicks: boolean;
  downloads: boolean;
  retention: number;
}

export interface ReportGeneration {
  status: 'pending' | 'running' | 'completed' | 'failed' | 'cancelled';
  progress: GenerationProgress;
  performance: GenerationPerformance;
  errors: GenerationError[];
  artifacts: GenerationArtifact[];
  logs: GenerationLog[];
}

export interface GenerationProgress {
  stage: string;
  percentage: number;
  eta: Date;
  currentTask: string;
  completedTasks: string[];
  remainingTasks: string[];
}

export interface GenerationPerformance {
  startTime: Date;
  endTime?: Date;
  duration: number;
  dataSize: number;
  renderTime: number;
  memoryUsage: number;
  cpuUsage: number;
}

export interface GenerationError {
  timestamp: Date;
  stage: string;
  type: string;
  message: string;
  details: Record<string, any>;
  recoverable: boolean;
  resolved: boolean;
}

export interface GenerationArtifact {
  name: string;
  type: 'report' | 'data' | 'log' | 'metadata';
  format: string;
  size: number;
  location: string;
  checksum: string;
  encrypted: boolean;
}

export interface GenerationLog {
  timestamp: Date;
  level: 'debug' | 'info' | 'warn' | 'error';
  component: string;
  message: string;
  details: Record<string, any>;
}

export interface ReportMetadata {
  version: string;
  template: string;
  generator: string;
  parameters: Record<string, any>;
  statistics: ReportStatistics;
  dependencies: ReportDependency[];
  quality: ReportQuality;
  lineage: ReportLineage;
}

export interface ReportStatistics {
  dataPoints: number;
  charts: number;
  tables: number;
  pages: number;
  size: number;
  executionTime: number;
  errors: number;
  warnings: number;
}

export interface ReportDependency {
  type: 'data' | 'template' | 'service' | 'library';
  name: string;
  version: string;
  critical: boolean;
  available: boolean;
}

export interface ReportQuality {
  score: number;
  dimensions: QualityDimensionScore[];
  issues: QualityIssue[];
  improvements: QualityImprovement[];
}

export interface QualityDimensionScore {
  dimension: string;
  score: number;
  weight: number;
  status: 'pass' | 'warn' | 'fail';
}

export interface QualityImprovement {
  area: string;
  suggestion: string;
  impact: 'low' | 'medium' | 'high';
  effort: 'low' | 'medium' | 'high';
}

export interface ReportLineage {
  sources: LineageNode[];
  transformations: LineageNode[];
  outputs: LineageNode[];
  flow: LineageFlow[];
}

export interface LineageNode {
  id: string;
  name: string;
  type: string;
  metadata: Record<string, any>;
}

export interface LineageFlow {
  from: string;
  to: string;
  type: string;
  metadata: Record<string, any>;
}

// Service interfaces
export interface ISecurityMetricsService {
  // Metric management
  createMetric(metric: Omit<SecurityMetric, 'id' | 'createdAt' | 'updatedAt'>): Promise<SecurityMetric>;
  updateMetric(metricId: string, updates: Partial<SecurityMetric>): Promise<SecurityMetric>;
  getMetric(metricId: string): Promise<SecurityMetric | null>;
  listMetrics(filters?: MetricFilters): Promise<SecurityMetric[]>;
  deleteMetric(metricId: string): Promise<void>;

  // Metric data
  collectMetricData(metricId: string): Promise<void>;
  getMetricData(metricId: string, timeRange: TimeRange): Promise<MetricDataPoint[]>;
  aggregateMetricData(metricId: string, aggregation: MetricAggregation): Promise<AggregatedData>;

  // Dashboard management
  createDashboard(dashboard: Omit<SecurityDashboard, 'id' | 'createdAt' | 'updatedAt'>): Promise<SecurityDashboard>;
  updateDashboard(dashboardId: string, updates: Partial<SecurityDashboard>): Promise<SecurityDashboard>;
  getDashboard(dashboardId: string): Promise<SecurityDashboard | null>;
  listDashboards(filters?: DashboardFilters): Promise<SecurityDashboard[]>;
  deleteDashboard(dashboardId: string): Promise<void>;

  // Report management
  createReport(report: Omit<SecurityReport, 'id' | 'createdAt' | 'updatedAt'>): Promise<SecurityReport>;
  generateReport(reportId: string): Promise<GeneratedReport>;
  scheduleReport(reportId: string, schedule: ReportSchedule): Promise<void>;
  getReportStatus(reportId: string): Promise<ReportStatus>;

  // Analytics and insights
  getMetricInsights(metricId: string): Promise<MetricInsights>;
  detectAnomalies(metricId: string, config?: AnomalyConfig): Promise<AnomalyDetection[]>;
  generateForecast(metricId: string, horizon: number): Promise<ForecastResult>;

  // Export and integration
  exportData(config: ExportConfig): Promise<ExportResult>;
  importData(config: ImportConfig): Promise<ImportResult>;
  integrateWithSystem(config: IntegrationConfig): Promise<IntegrationResult>;
}

export interface MetricFilters {
  category?: MetricCategory[];
  type?: MetricType[];
  priority?: MetricPriority[];
  status?: MetricStatus[];
  tags?: string[];
  enabled?: boolean;
  search?: string;
}

export interface TimeRange {
  start: Date;
  end: Date;
  timezone?: string;
}

export interface MetricDataPoint {
  timestamp: Date;
  value: number;
  dimensions: Record<string, any>;
  metadata: Record<string, any>;
}

export interface AggregatedData {
  values: AggregatedValue[];
  metadata: AggregationMetadata;
}

export interface AggregatedValue {
  timestamp: Date;
  value: number;
  count: number;
  confidence: number;
}

export interface AggregationMetadata {
  method: AggregationType;
  window: TimeWindow;
  groupBy: string[];
  filters: AggregationFilter[];
  quality: number;
}

export interface DashboardFilters {
  type?: DashboardType[];
  category?: string[];
  tags?: string[];
  enabled?: boolean;
  search?: string;
}

export interface GeneratedReport {
  id: string;
  reportId: string;
  format: string;
  content: any;
  metadata: GenerationMetadata;
  artifacts: GenerationArtifact[];
}

export interface GenerationMetadata {
  generatedAt: Date;
  parameters: Record<string, any>;
  performance: GenerationPerformance;
  quality: number;
  errors: string[];
}

export interface ReportStatus {
  status: ReportGeneration['status'];
  progress: GenerationProgress;
  errors: GenerationError[];
  eta?: Date;
}

export interface MetricInsights {
  trends: TrendInsight[];
  patterns: PatternInsight[];
  correlations: CorrelationInsight[];
  anomalies: AnomalyInsight[];
  recommendations: InsightRecommendation[];
}

export interface TrendInsight {
  type: 'increasing' | 'decreasing' | 'stable' | 'seasonal' | 'cyclical';
  direction: 'up' | 'down' | 'stable';
  magnitude: number;
  confidence: number;
  duration: number;
  factors: string[];
  forecast: TrendForecast;
}

export interface TrendForecast {
  shortTerm: ForecastPoint[];
  mediumTerm: ForecastPoint[];
  longTerm: ForecastPoint[];
  confidence: number;
}

export interface ForecastPoint {
  timestamp: Date;
  value: number;
  confidence: number;
  upperBound: number;
  lowerBound: number;
}

export interface PatternInsight {
  type: 'periodic' | 'seasonal' | 'event_driven' | 'threshold_based';
  pattern: string;
  frequency: string;
  strength: number;
  occurrences: PatternOccurrence[];
  predictions: PatternPrediction[];
}

export interface PatternOccurrence {
  timestamp: Date;
  value: number;
  confidence: number;
  context: Record<string, any>;
}

export interface PatternPrediction {
  timestamp: Date;
  probability: number;
  confidence: number;
  conditions: string[];
}

export interface CorrelationInsight {
  metric: string;
  correlation: number;
  significance: number;
  lag: number;
  relationship: 'positive' | 'negative' | 'non_linear';
  strength: 'weak' | 'moderate' | 'strong' | 'very_strong';
  causality: CausalityInfo;
}

export interface CausalityInfo {
  direction: 'none' | 'x_to_y' | 'y_to_x' | 'bidirectional';
  confidence: number;
  evidence: string[];
  mechanisms: string[];
}

export interface AnomalyInsight {
  anomalies: AnomalyDetection[];
  patterns: AnomalyPattern[];
  causes: AnomalyCause[];
  impact: AnomalyImpact;
  recommendations: AnomalyRecommendation[];
}

export interface AnomalyDetection {
  timestamp: Date;
  value: number;
  expected: number;
  deviation: number;
  severity: 'low' | 'medium' | 'high' | 'critical';
  type: string;
  confidence: number;
}

export interface AnomalyPattern {
  type: string;
  frequency: string;
  characteristics: string[];
  examples: AnomalyExample[];
}

export interface AnomalyExample {
  timestamp: Date;
  description: string;
  severity: string;
  resolved: boolean;
}

export interface AnomalyCause {
  category: 'system' | 'user' | 'external' | 'data' | 'configuration';
  description: string;
  confidence: number;
  evidence: string[];
  frequency: number;
}

export interface AnomalyImpact {
  scope: 'local' | 'system' | 'organization';
  severity: 'low' | 'medium' | 'high' | 'critical';
  duration: number;
  cascading: boolean;
  affected: string[];
}

export interface AnomalyRecommendation {
  type: 'prevention' | 'detection' | 'response' | 'recovery';
  action: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
  effort: 'low' | 'medium' | 'high';
  timeline: string;
  dependencies: string[];
}

export interface InsightRecommendation {
  category: 'optimization' | 'alerting' | 'investigation' | 'automation' | 'governance';
  title: string;
  description: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
  impact: 'low' | 'medium' | 'high';
  effort: 'low' | 'medium' | 'high';
  timeline: string;
  actions: RecommendedAction[];
  benefits: string[];
  risks: string[];
}

export interface RecommendedAction {
  step: number;
  description: string;
  type: 'configuration' | 'process' | 'technology' | 'training';
  owner: string;
  timeline: string;
  dependencies: string[];
  verification: string[];
}

export interface AnomalyConfig {
  algorithms: string[];
  sensitivity: number;
  windowSize: number;
  seasonality: boolean;
  trend: boolean;
  thresholds: AnomalyThreshold[];
}

export interface AnomalyThreshold {
  type: 'statistical' | 'absolute' | 'relative';
  value: number;
  confidence: number;
}

export interface ForecastResult {
  predictions: ForecastPrediction[];
  confidence: number;
  accuracy: ForecastAccuracy;
  methodology: ForecastMethodology;
  assumptions: string[];
  limitations: string[];
}

export interface ForecastPrediction {
  timestamp: Date;
  value: number;
  confidence: number;
  interval: ForecastInterval;
  scenario: string;
}

export interface ForecastInterval {
  lower: number;
  upper: number;
  confidence: number;
}

export interface ForecastAccuracy {
  mae: number; // Mean Absolute Error
  mape: number; // Mean Absolute Percentage Error
  rmse: number; // Root Mean Square Error
  r2: number; // R-squared
}

export interface ForecastMethodology {
  algorithm: string;
  parameters: Record<string, any>;
  training: TrainingInfo;
  validation: ValidationInfo;
  features: string[];
}

export interface TrainingInfo {
  period: TimeRange;
  samples: number;
  features: string[];
  quality: number;
}

export interface ValidationInfo {
  method: 'holdout' | 'cross_validation' | 'time_series_split';
  accuracy: ForecastAccuracy;
  stability: number;
  robustness: number;
}

export interface ExportConfig {
  data: ExportDataConfig;
  format: ExportFormatConfig;
  destination: ExportDestination;
  schedule?: ExportSchedule;
}

export interface ExportDataConfig {
  metrics: string[];
  timeRange: TimeRange;
  filters: Record<string, any>;
  aggregation?: ExportAggregation;
  transformation?: ExportTransformation[];
}

export interface ExportAggregation {
  method: AggregationType;
  interval: string;
  groupBy: string[];
}

export interface ExportTransformation {
  type: string;
  config: Record<string, any>;
  order: number;
}

export interface ExportFormatConfig {
  type: 'csv' | 'json' | 'parquet' | 'avro' | 'xml' | 'excel';
  options: Record<string, any>;
  compression?: string;
  encryption?: EncryptionConfig;
}

export interface ExportDestination {
  type: 'file' | 'database' | 'api' | 'cloud' | 'email';
  location: string;
  credentials?: Record<string, string>;
  options?: Record<string, any>;
}

export interface ExportSchedule {
  frequency: string;
  timezone: string;
  enabled: boolean;
  notifications: string[];
}

export interface ExportResult {
  id: string;
  status: 'success' | 'partial' | 'failed';
  location: string;
  size: number;
  records: number;
  duration: number;
  errors: string[];
  metadata: Record<string, any>;
}

export interface ImportConfig {
  source: ImportSource;
  mapping: ImportMapping;
  validation: ImportValidation;
  transformation?: ImportTransformation[];
}

export interface ImportSource {
  type: 'file' | 'database' | 'api' | 'stream';
  location: string;
  format: string;
  credentials?: Record<string, string>;
  options?: Record<string, any>;
}

export interface ImportMapping {
  fields: FieldMapping[];
  defaults: Record<string, any>;
  required: string[];
  ignored: string[];
}

export interface FieldMapping {
  source: string;
  target: string;
  type: string;
  transformation?: string;
  validation?: string[];
}

export interface ImportValidation {
  schema: string;
  rules: ImportValidationRule[];
  onError: 'skip' | 'stop' | 'continue';
  reporting: boolean;
}

export interface ImportValidationRule {
  field: string;
  rule: string;
  severity: 'error' | 'warning';
  message: string;
}

export interface ImportTransformation {
  type: string;
  config: Record<string, any>;
  order: number;
  conditions?: string[];
}

export interface ImportResult {
  id: string;
  status: 'success' | 'partial' | 'failed';
  processed: number;
  imported: number;
  skipped: number;
  errors: ImportError[];
  duration: number;
  metadata: Record<string, any>;
}

export interface ImportError {
  row: number;
  field: string;
  error: string;
  value: any;
  severity: 'error' | 'warning';
}

export interface IntegrationConfig {
  system: string;
  type: 'import' | 'export' | 'sync' | 'realtime';
  connection: IntegrationConnection;
  mapping: IntegrationMapping;
  schedule?: IntegrationSchedule;
  monitoring: IntegrationMonitoring;
}

export interface IntegrationConnection {
  endpoint: string;
  protocol: string;
  authentication: AuthenticationConfig;
  timeout: number;
  retries: number;
  healthCheck: HealthCheckConfig;
}

export interface HealthCheckConfig {
  enabled: boolean;
  endpoint: string;
  interval: number;
  timeout: number;
  failureThreshold: number;
}

export interface IntegrationMapping {
  inbound: MappingRule[];
  outbound: MappingRule[];
  bidirectional: MappingRule[];
  conflicts: ConflictResolution;
}

export interface MappingRule {
  source: string;
  target: string;
  transformation: string;
  conditions: string[];
  priority: number;
}

export interface ConflictResolution {
  strategy: 'source_wins' | 'target_wins' | 'merge' | 'manual' | 'custom';
  rules: ConflictRule[];
  notification: boolean;
}

export interface ConflictRule {
  condition: string;
  action: string;
  priority: number;
}

export interface IntegrationSchedule {
  frequency: string;
  timezone: string;
  enabled: boolean;
  retry: RetryConfig;
  monitoring: boolean;
}

export interface IntegrationMonitoring {
  enabled: boolean;
  metrics: string[];
  alerts: string[];
  logging: LoggingConfig;
  dashboard: string;
}

export interface LoggingConfig {
  level: 'debug' | 'info' | 'warn' | 'error';
  format: string;
  destination: string[];
  retention: number;
  structured: boolean;
}

export interface IntegrationResult {
  id: string;
  status: 'success' | 'partial' | 'failed';
  operations: OperationResult[];
  performance: IntegrationPerformance;
  errors: IntegrationError[];
  metadata: Record<string, any>;
}

export interface OperationResult {
  type: 'create' | 'update' | 'delete' | 'sync';
  entity: string;
  count: number;
  success: number;
  errors: number;
  duration: number;
}

export interface IntegrationPerformance {
  totalDuration: number;
  throughput: number;
  latency: number;
  errorRate: number;
  resourceUsage: ResourceUsage;
}

export interface ResourceUsage {
  cpu: number;
  memory: number;
  network: number;
  storage: number;
}

export interface IntegrationError {
  timestamp: Date;
  operation: string;
  entity: string;
  error: string;
  details: Record<string, any>;
  recoverable: boolean;
}