// Incident Response Automation Interface Definitions
// Enterprise-grade incident response and automation framework

export enum IncidentSeverity {
  CRITICAL = 'critical',
  HIGH = 'high',
  MEDIUM = 'medium',
  LOW = 'low',
  INFORMATIONAL = 'informational'
}

export enum IncidentStatus {
  NEW = 'new',
  ACKNOWLEDGED = 'acknowledged',
  IN_PROGRESS = 'in_progress',
  CONTAINED = 'contained',
  ERADICATED = 'eradicated',
  RECOVERED = 'recovered',
  CLOSED = 'closed',
  FALSE_POSITIVE = 'false_positive'
}

export enum IncidentType {
  MALWARE = 'malware',
  RANSOMWARE = 'ransomware',
  PHISHING = 'phishing',
  DATA_BREACH = 'data_breach',
  DDoS = 'ddos',
  UNAUTHORIZED_ACCESS = 'unauthorized_access',
  INSIDER_THREAT = 'insider_threat',
  SUPPLY_CHAIN = 'supply_chain',
  ZERO_DAY = 'zero_day',
  APT = 'apt',
  SOCIAL_ENGINEERING = 'social_engineering',
  PHYSICAL_SECURITY = 'physical_security',
  MISCONFIGURATION = 'misconfiguration',
  VULNERABILITY_EXPLOITATION = 'vulnerability_exploitation',
  OTHER = 'other'
}

export enum ResponseAction {
  ISOLATE_SYSTEM = 'isolate_system',
  BLOCK_IP = 'block_ip',
  DISABLE_ACCOUNT = 'disable_account',
  QUARANTINE_FILE = 'quarantine_file',
  TERMINATE_PROCESS = 'terminate_process',
  RESET_PASSWORD = 'reset_password',
  REVOKE_ACCESS = 'revoke_access',
  BACKUP_DATA = 'backup_data',
  RESTORE_DATA = 'restore_data',
  COLLECT_EVIDENCE = 'collect_evidence',
  NOTIFY_TEAM = 'notify_team',
  ESCALATE = 'escalate',
  RUN_SCAN = 'run_scan',
  APPLY_PATCH = 'apply_patch',
  UPDATE_FIREWALL = 'update_firewall',
  ENABLE_MFA = 'enable_mfa',
  REVIEW_LOGS = 'review_logs',
  CUSTOM_SCRIPT = 'custom_script'
}

export enum PlaybookTrigger {
  MANUAL = 'manual',
  AUTOMATIC = 'automatic',
  SCHEDULED = 'scheduled',
  THRESHOLD = 'threshold',
  PATTERN = 'pattern',
  ALERT = 'alert',
  API = 'api'
}

export enum EscalationLevel {
  L1_SUPPORT = 'l1_support',
  L2_SUPPORT = 'l2_support',
  L3_SUPPORT = 'l3_support',
  SECURITY_TEAM = 'security_team',
  SOC_ANALYST = 'soc_analyst',
  INCIDENT_COMMANDER = 'incident_commander',
  MANAGEMENT = 'management',
  EXECUTIVE = 'executive',
  EXTERNAL_VENDOR = 'external_vendor',
  LAW_ENFORCEMENT = 'law_enforcement'
}

export interface SecurityIncident {
  id: string;
  title: string;
  description: string;
  type: IncidentType;
  severity: IncidentSeverity;
  status: IncidentStatus;
  source: IncidentSource;
  affectedAssets: AffectedAsset[];
  timeline: IncidentTimeline[];
  indicators: IndicatorOfCompromise[];
  responseActions: ResponseActionRecord[];
  assignedTo?: string;
  escalationLevel?: EscalationLevel;
  tags: string[];
  relatedIncidents?: string[];
  rootCause?: string;
  lessonsLearned?: string;
  estimatedImpact?: IncidentImpact;
  actualImpact?: IncidentImpact;
  createdAt: Date;
  updatedAt: Date;
  closedAt?: Date;
}

export interface IncidentSource {
  type: 'siem' | 'ids' | 'edr' | 'user_report' | 'threat_intel' | 'vulnerability_scan' | 'other';
  system?: string;
  alertId?: string;
  reportedBy?: string;
  detectionMethod?: string;
  confidence: number;
}

export interface AffectedAsset {
  id: string;
  type: 'server' | 'workstation' | 'network' | 'application' | 'data' | 'user' | 'other';
  name: string;
  identifier: string;
  criticality: 'critical' | 'high' | 'medium' | 'low';
  owner?: string;
  location?: string;
  compromiseType?: string;
  remediationStatus?: 'pending' | 'in_progress' | 'completed';
}

export interface IncidentTimeline {
  timestamp: Date;
  event: string;
  actor?: string;
  action?: string;
  details?: string;
  evidence?: Evidence[];
}

export interface Evidence {
  id: string;
  type: 'file' | 'log' | 'screenshot' | 'memory_dump' | 'network_capture' | 'other';
  name: string;
  hash?: string;
  size?: number;
  location: string;
  collectedBy: string;
  collectedAt: Date;
  chainOfCustody: ChainOfCustody[];
}

export interface ChainOfCustody {
  timestamp: Date;
  action: string;
  actor: string;
  location: string;
  notes?: string;
}

export interface IndicatorOfCompromise {
  id: string;
  type: 'ip' | 'domain' | 'url' | 'hash' | 'email' | 'registry' | 'mutex' | 'yara' | 'other';
  value: string;
  confidence: number;
  source?: string;
  firstSeen?: Date;
  lastSeen?: Date;
  relatedIncidents?: string[];
  threatIntel?: ThreatIntelligence;
}

export interface ThreatIntelligence {
  source: string;
  reputation: number;
  malwareFamily?: string;
  attackPattern?: string;
  threatActor?: string;
  campaign?: string;
  ttps?: string[];
  references?: string[];
}

export interface ResponseActionRecord {
  id: string;
  action: ResponseAction;
  status: 'pending' | 'in_progress' | 'completed' | 'failed' | 'skipped';
  executedBy?: string;
  executedAt?: Date;
  result?: string;
  error?: string;
  rollbackAvailable?: boolean;
  automatedExecution: boolean;
}

export interface IncidentImpact {
  confidentiality: 'none' | 'low' | 'medium' | 'high';
  integrity: 'none' | 'low' | 'medium' | 'high';
  availability: 'none' | 'low' | 'medium' | 'high';
  financial?: number;
  reputational?: 'minimal' | 'moderate' | 'significant' | 'severe';
  regulatory?: string[];
  affectedUsers?: number;
  dataExposed?: boolean;
  downtime?: number;
}

// Playbook Definitions
export interface ResponsePlaybook {
  id: string;
  name: string;
  description: string;
  type: IncidentType;
  severity: IncidentSeverity[];
  trigger: PlaybookTrigger;
  enabled: boolean;
  steps: PlaybookStep[];
  variables?: PlaybookVariable[];
  conditions?: PlaybookCondition[];
  notifications?: NotificationConfig[];
  sla?: SLAConfig;
  version: string;
  createdBy: string;
  createdAt: Date;
  updatedAt: Date;
  lastExecuted?: Date;
  executionCount: number;
  successRate: number;
}

export interface PlaybookStep {
  id: string;
  name: string;
  description?: string;
  action: ResponseAction;
  parameters?: Record<string, any>;
  conditions?: StepCondition[];
  timeout?: number;
  retryCount?: number;
  onSuccess?: string[];
  onFailure?: string[];
  parallel?: boolean;
  manual?: boolean;
  approvalRequired?: boolean;
  approver?: string;
}

export interface StepCondition {
  field: string;
  operator: 'equals' | 'not_equals' | 'contains' | 'greater_than' | 'less_than' | 'regex';
  value: any;
  logic?: 'and' | 'or';
}

export interface PlaybookVariable {
  name: string;
  type: 'string' | 'number' | 'boolean' | 'array' | 'object';
  value?: any;
  source?: 'input' | 'incident' | 'step_output' | 'external';
  required?: boolean;
}

export interface PlaybookCondition {
  name: string;
  expression: string;
  action: 'skip' | 'stop' | 'branch';
  target?: string;
}

export interface NotificationConfig {
  channel: 'email' | 'sms' | 'slack' | 'teams' | 'webhook' | 'pagerduty';
  recipients: string[];
  template?: string;
  conditions?: NotificationCondition[];
  priority?: 'low' | 'normal' | 'high' | 'urgent';
}

export interface NotificationCondition {
  event: 'incident_created' | 'status_change' | 'escalation' | 'sla_breach' | 'completion';
  filters?: Record<string, any>;
}

export interface SLAConfig {
  responseTime: SLAThreshold[];
  resolutionTime: SLAThreshold[];
  escalationTime?: SLAThreshold[];
  notificationTime?: number;
}

export interface SLAThreshold {
  severity: IncidentSeverity;
  threshold: number;
  unit: 'minutes' | 'hours' | 'days';
  businessHours?: boolean;
}

// Automation Engine
export interface AutomationRule {
  id: string;
  name: string;
  description: string;
  enabled: boolean;
  priority: number;
  triggers: AutomationTrigger[];
  conditions: AutomationCondition[];
  actions: AutomationAction[];
  schedule?: ScheduleConfig;
  rateLimiting?: RateLimitConfig;
  createdAt: Date;
  updatedAt: Date;
  lastTriggered?: Date;
  triggerCount: number;
}

export interface AutomationTrigger {
  type: 'event' | 'threshold' | 'pattern' | 'schedule' | 'manual';
  source?: string;
  eventType?: string;
  pattern?: string;
  threshold?: ThresholdConfig;
  schedule?: string;
}

export interface ThresholdConfig {
  metric: string;
  operator: 'greater_than' | 'less_than' | 'equals' | 'between';
  value: number;
  secondValue?: number;
  duration?: number;
  unit?: string;
}

export interface AutomationCondition {
  field: string;
  operator: string;
  value: any;
  logic: 'and' | 'or';
  negate?: boolean;
}

export interface AutomationAction {
  type: ResponseAction;
  parameters: Record<string, any>;
  delay?: number;
  timeout?: number;
  retryOnFailure?: boolean;
  maxRetries?: number;
  continueOnError?: boolean;
}

export interface ScheduleConfig {
  type: 'cron' | 'interval' | 'once';
  expression?: string;
  interval?: number;
  unit?: 'seconds' | 'minutes' | 'hours' | 'days';
  startTime?: Date;
  endTime?: Date;
  timezone?: string;
}

export interface RateLimitConfig {
  maxExecutions: number;
  timeWindow: number;
  unit: 'seconds' | 'minutes' | 'hours';
  action: 'queue' | 'drop' | 'alert';
}

// Forensics and Investigation
export interface ForensicInvestigation {
  id: string;
  incidentId: string;
  investigator: string;
  status: 'pending' | 'in_progress' | 'completed' | 'archived';
  scope: InvestigationScope;
  findings: ForensicFinding[];
  evidence: Evidence[];
  timeline: ForensicTimeline[];
  recommendations: string[];
  report?: InvestigationReport;
  createdAt: Date;
  completedAt?: Date;
}

export interface InvestigationScope {
  systems: string[];
  timeRange: {
    start: Date;
    end: Date;
  };
  dataTypes: string[];
  objectives: string[];
  limitations?: string[];
}

export interface ForensicFinding {
  id: string;
  type: 'malware' | 'persistence' | 'lateral_movement' | 'data_exfiltration' | 'privilege_escalation' | 'other';
  description: string;
  evidence: string[];
  confidence: number;
  impact: 'low' | 'medium' | 'high' | 'critical';
  recommendations?: string[];
}

export interface ForensicTimeline {
  timestamp: Date;
  source: string;
  activity: string;
  actor?: string;
  system?: string;
  artifacts?: string[];
  suspicious: boolean;
}

export interface InvestigationReport {
  id: string;
  executive_summary: string;
  technical_details: string;
  timeline_analysis: string;
  root_cause_analysis: string;
  impact_assessment: string;
  recommendations: string[];
  lessons_learned: string[];
  attachments?: string[];
  generatedAt: Date;
  approvedBy?: string;
}

// Communication and Coordination
export interface IncidentCommunication {
  id: string;
  incidentId: string;
  type: 'internal' | 'external' | 'regulatory' | 'customer' | 'media';
  channel: string;
  recipients: string[];
  subject: string;
  content: string;
  attachments?: string[];
  sentAt: Date;
  sentBy: string;
  status: 'draft' | 'sent' | 'failed' | 'scheduled';
  template?: string;
}

export interface StakeholderNotification {
  stakeholderGroup: string;
  notificationType: 'initial' | 'update' | 'resolution' | 'post_mortem';
  template: string;
  frequency?: string;
  conditions?: NotificationCondition[];
  lastSent?: Date;
  nextScheduled?: Date;
}

export interface WarRoom {
  id: string;
  incidentId: string;
  name: string;
  participants: WarRoomParticipant[];
  status: 'active' | 'standby' | 'closed';
  bridge?: CommunicationBridge;
  sharedDocs?: string[];
  decisions: Decision[];
  actionItems: ActionItem[];
  createdAt: Date;
  closedAt?: Date;
}

export interface WarRoomParticipant {
  userId: string;
  name: string;
  role: string;
  joinedAt: Date;
  leftAt?: Date;
  status: 'active' | 'away' | 'offline';
}

export interface CommunicationBridge {
  type: 'phone' | 'video' | 'chat' | 'mixed';
  url?: string;
  dialIn?: string;
  accessCode?: string;
  backup?: string;
}

export interface Decision {
  id: string;
  timestamp: Date;
  decision: string;
  rationale: string;
  madeBy: string;
  approvedBy?: string[];
  impact?: string;
  reversible?: boolean;
}

export interface ActionItem {
  id: string;
  description: string;
  assignedTo: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
  dueDate?: Date;
  status: 'pending' | 'in_progress' | 'completed' | 'blocked';
  blockers?: string[];
  completedAt?: Date;
}

// Recovery and Business Continuity
export interface RecoveryPlan {
  id: string;
  name: string;
  description: string;
  type: 'backup_restore' | 'failover' | 'rebuild' | 'manual';
  systems: RecoverySystem[];
  dependencies: string[];
  sequence: RecoveryStep[];
  rto: number;
  rpo: number;
  testSchedule?: string;
  lastTested?: Date;
  testResults?: TestResult[];
}

export interface RecoverySystem {
  id: string;
  name: string;
  type: string;
  criticality: 'critical' | 'high' | 'medium' | 'low';
  backupLocation?: string;
  recoveryProcedure?: string;
  owner: string;
  alternateOwner?: string;
}

export interface RecoveryStep {
  order: number;
  name: string;
  description: string;
  system?: string;
  action: string;
  estimatedTime: number;
  responsible: string;
  validation?: string;
  rollback?: string;
}

export interface TestResult {
  id: string;
  testDate: Date;
  tester: string;
  status: 'success' | 'partial' | 'failed';
  actualRTO?: number;
  actualRPO?: number;
  issues?: string[];
  improvements?: string[];
}

// Threat Hunting
export interface ThreatHunt {
  id: string;
  name: string;
  hypothesis: string;
  scope: HuntScope;
  methodology: string;
  queries: HuntQuery[];
  findings: HuntFinding[];
  status: 'planning' | 'active' | 'analysis' | 'completed';
  hunter: string;
  team?: string[];
  startDate: Date;
  endDate?: Date;
  report?: HuntReport;
}

export interface HuntScope {
  systems: string[];
  timeRange: {
    start: Date;
    end: Date;
  };
  dataSources: string[];
  iocs?: string[];
  ttps?: string[];
}

export interface HuntQuery {
  id: string;
  description: string;
  query: string;
  dataSource: string;
  results?: any[];
  suspicious?: boolean;
  notes?: string;
}

export interface HuntFinding {
  id: string;
  description: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  confidence: number;
  evidence: string[];
  affectedSystems: string[];
  recommendations: string[];
  createIncident?: boolean;
}

export interface HuntReport {
  summary: string;
  methodology: string;
  findings: string;
  recommendations: string[];
  nextSteps?: string[];
  lessonsLearned?: string[];
}

// Service Interface
export interface IIncidentResponseService {
  // Incident Management
  createIncident(incident: Omit<SecurityIncident, 'id' | 'createdAt' | 'updatedAt'>): Promise<SecurityIncident>;
  updateIncident(id: string, updates: Partial<SecurityIncident>): Promise<SecurityIncident>;
  getIncident(id: string): Promise<SecurityIncident>;
  listIncidents(filter?: IncidentFilter): Promise<SecurityIncident[]>;
  assignIncident(id: string, assignee: string): Promise<void>;
  escalateIncident(id: string, level: EscalationLevel): Promise<void>;
  closeIncident(id: string, resolution: string): Promise<void>;

  // Playbook Management
  createPlaybook(playbook: Omit<ResponsePlaybook, 'id' | 'createdAt' | 'updatedAt'>): Promise<ResponsePlaybook>;
  updatePlaybook(id: string, updates: Partial<ResponsePlaybook>): Promise<ResponsePlaybook>;
  executePlaybook(playbookId: string, incidentId: string, variables?: Record<string, any>): Promise<PlaybookExecution>;
  getPlaybookStatus(executionId: string): Promise<PlaybookExecutionStatus>;
  stopPlaybook(executionId: string): Promise<void>;

  // Automation
  createAutomationRule(rule: Omit<AutomationRule, 'id' | 'createdAt' | 'updatedAt'>): Promise<AutomationRule>;
  updateAutomationRule(id: string, updates: Partial<AutomationRule>): Promise<AutomationRule>;
  enableAutomation(id: string): Promise<void>;
  disableAutomation(id: string): Promise<void>;
  testAutomation(id: string, testData?: any): Promise<AutomationTestResult>;

  // Response Actions
  executeAction(incidentId: string, action: ResponseAction, parameters?: any): Promise<ResponseActionRecord>;
  batchExecuteActions(incidentId: string, actions: ResponseAction[]): Promise<ResponseActionRecord[]>;
  rollbackAction(actionId: string): Promise<void>;

  // Investigation
  startInvestigation(incidentId: string, scope: InvestigationScope): Promise<ForensicInvestigation>;
  updateInvestigation(id: string, findings: ForensicFinding[]): Promise<void>;
  collectEvidence(investigationId: string, evidence: Evidence): Promise<void>;
  generateInvestigationReport(investigationId: string): Promise<InvestigationReport>;

  // Communication
  createWarRoom(incidentId: string): Promise<WarRoom>;
  joinWarRoom(warRoomId: string, participant: string): Promise<void>;
  sendNotification(notification: IncidentCommunication): Promise<void>;
  broadcastUpdate(incidentId: string, update: string): Promise<void>;

  // Recovery
  initiateRecovery(incidentId: string, planId: string): Promise<RecoveryExecution>;
  updateRecoveryStatus(executionId: string, status: RecoveryStatus): Promise<void>;
  validateRecovery(executionId: string): Promise<ValidationResult>;

  // Threat Hunting
  createHunt(hunt: Omit<ThreatHunt, 'id' | 'startDate'>): Promise<ThreatHunt>;
  executeHuntQuery(huntId: string, query: HuntQuery): Promise<any[]>;
  reportHuntFinding(huntId: string, finding: HuntFinding): Promise<void>;
  completeHunt(huntId: string, report: HuntReport): Promise<void>;

  // Analytics and Reporting
  getIncidentMetrics(timeRange?: TimeRange): Promise<IncidentMetrics>;
  getMTTR(severity?: IncidentSeverity): Promise<number>;
  getMTTD(type?: IncidentType): Promise<number>;
  generateIncidentReport(incidentId: string): Promise<IncidentReport>;
  generatePostMortem(incidentId: string): Promise<PostMortemReport>;
  exportIncidentData(format: 'json' | 'csv' | 'pdf'): Promise<Blob>;
}

// Supporting Types
export interface IncidentFilter {
  status?: IncidentStatus[];
  severity?: IncidentSeverity[];
  type?: IncidentType[];
  assignedTo?: string;
  dateRange?: {
    start: Date;
    end: Date;
  };
  tags?: string[];
}

export interface PlaybookExecution {
  id: string;
  playbookId: string;
  incidentId: string;
  status: 'running' | 'paused' | 'completed' | 'failed' | 'cancelled';
  currentStep?: string;
  completedSteps: string[];
  variables: Record<string, any>;
  startedAt: Date;
  completedAt?: Date;
  logs: ExecutionLog[];
}

export interface PlaybookExecutionStatus {
  executionId: string;
  status: string;
  progress: number;
  currentStep?: string;
  errors?: string[];
  warnings?: string[];
}

export interface ExecutionLog {
  timestamp: Date;
  stepId: string;
  action: string;
  status: string;
  details?: string;
  error?: string;
}

export interface AutomationTestResult {
  success: boolean;
  executedActions: string[];
  results: Record<string, any>;
  errors?: string[];
  warnings?: string[];
  duration: number;
}

export interface RecoveryExecution {
  id: string;
  planId: string;
  incidentId: string;
  status: 'preparing' | 'in_progress' | 'validating' | 'completed' | 'failed';
  currentStep: number;
  completedSteps: number[];
  startedAt: Date;
  estimatedCompletion?: Date;
  actualCompletion?: Date;
}

export interface RecoveryStatus {
  step: number;
  status: 'completed' | 'failed' | 'skipped';
  notes?: string;
  issues?: string[];
}

export interface ValidationResult {
  success: boolean;
  validatedSystems: string[];
  failedValidations: string[];
  warnings?: string[];
  recommendations?: string[];
}

export interface TimeRange {
  start: Date;
  end: Date;
}

export interface IncidentMetrics {
  totalIncidents: number;
  byStatus: Record<IncidentStatus, number>;
  bySeverity: Record<IncidentSeverity, number>;
  byType: Record<IncidentType, number>;
  mttr: number;
  mttd: number;
  mttrBySeverity: Record<IncidentSeverity, number>;
  escalationRate: number;
  falsePositiveRate: number;
  automationRate: number;
  trends: MetricTrend[];
}

export interface MetricTrend {
  date: Date;
  value: number;
  change: number;
  changePercent: number;
}

export interface IncidentReport {
  incidentId: string;
  summary: string;
  timeline: string;
  impact: string;
  responseActions: string;
  rootCause?: string;
  recommendations: string[];
  attachments?: string[];
  generatedAt: Date;
}

export interface PostMortemReport {
  incidentId: string;
  executiveSummary: string;
  timeline: string;
  rootCause: string;
  impact: string;
  whatWentWell: string[];
  whatWentWrong: string[];
  actionItems: string[];
  lessonsLearned: string[];
  preventiveMeasures: string[];
  generatedAt: Date;
}

// Configuration Types
export interface IncidentResponseConfig {
  autoEscalation: boolean;
  escalationThresholds: EscalationThreshold[];
  defaultAssignment: AssignmentRule[];
  slaConfig: SLAConfig;
  notificationChannels: NotificationChannel[];
  integrations: IntegrationConfig[];
  retentionPolicy: RetentionPolicy;
}

export interface EscalationThreshold {
  severity: IncidentSeverity;
  timeThreshold: number;
  escalateTo: EscalationLevel;
}

export interface AssignmentRule {
  condition: string;
  assignTo: string;
  priority: number;
}

export interface NotificationChannel {
  id: string;
  type: string;
  config: Record<string, any>;
  enabled: boolean;
}

export interface IntegrationConfig {
  name: string;
  type: string;
  endpoint: string;
  credentials?: Record<string, any>;
  mappings?: Record<string, string>;
  enabled: boolean;
}

export interface RetentionPolicy {
  incidentRetention: number;
  evidenceRetention: number;
  logRetention: number;
  archiveLocation?: string;
}