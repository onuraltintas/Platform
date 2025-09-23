/**
 * Enterprise Compliance Audit Interfaces
 * OWASP, ISO 27001, SOC 2, and other security compliance frameworks
 */

// Base compliance types
export type ComplianceFramework = 'owasp' | 'iso27001' | 'soc2' | 'pci_dss' | 'gdpr' | 'hipaa' | 'nist' | 'cis' | 'custom';
export type ComplianceStatus = 'compliant' | 'non_compliant' | 'partially_compliant' | 'not_applicable' | 'pending_review' | 'remediation_required';
export type AuditStatus = 'planned' | 'in_progress' | 'completed' | 'failed' | 'cancelled' | 'on_hold';
export type RiskLevel = 'low' | 'medium' | 'high' | 'critical';
export type RemediationPriority = 'low' | 'medium' | 'high' | 'critical' | 'immediate';
export type EvidenceType = 'document' | 'screenshot' | 'log_file' | 'configuration' | 'policy' | 'procedure' | 'training_record' | 'test_result' | 'certificate';

// Core compliance audit interfaces
export interface ComplianceAudit {
  id: string;
  name: string;
  description: string;
  framework: ComplianceFramework;
  version: string;
  scope: AuditScope;
  status: AuditStatus;
  scheduledDate: Date;
  startDate?: Date;
  endDate?: Date;
  dueDate: Date;
  auditor: AuditorInfo;
  assessments: ComplianceAssessment[];
  findings: ComplianceFinding[];
  recommendations: ComplianceRecommendation[];
  evidence: ComplianceEvidence[];
  metrics: ComplianceMetrics;
  timeline: AuditTimelineEntry[];
  configuration: AuditConfiguration;
  reports: AuditReport[];
  tags: string[];
  metadata: AuditMetadata;
  createdAt: Date;
  updatedAt: Date;
  createdBy: string;
  updatedBy: string;
}

export interface AuditScope {
  systems: string[];
  applications: string[];
  processes: string[];
  departments: string[];
  locations: string[];
  dataTypes: string[];
  includeThirdParty: boolean;
  exclusions: string[];
  justifications: Record<string, string>;
  boundaries: ScopeBoundary[];
}

export interface ScopeBoundary {
  type: 'technical' | 'organizational' | 'physical' | 'temporal';
  name: string;
  description: string;
  included: boolean;
  rationale: string;
}

export interface AuditorInfo {
  id: string;
  name: string;
  email: string;
  role: 'lead_auditor' | 'auditor' | 'technical_specialist' | 'subject_matter_expert';
  organization: string;
  certifications: AuditorCertification[];
  experience: AuditorExperience[];
  independence: IndependenceDeclaration;
}

export interface AuditorCertification {
  name: string;
  issuingBody: string;
  number: string;
  issueDate: Date;
  expiryDate: Date;
  status: 'active' | 'expired' | 'suspended';
}

export interface AuditorExperience {
  framework: ComplianceFramework;
  yearsExperience: number;
  industryExperience: string[];
  previousAudits: number;
  specializations: string[];
}

export interface IndependenceDeclaration {
  independent: boolean;
  conflicts: ConflictOfInterest[];
  declaration: string;
  declaredAt: Date;
  declaredBy: string;
}

export interface ConflictOfInterest {
  type: 'financial' | 'personal' | 'professional' | 'organizational';
  description: string;
  mitigation: string;
  severity: 'low' | 'medium' | 'high';
  resolved: boolean;
}

// Framework-specific interfaces
export interface OWASPAssessment extends ComplianceAssessment {
  framework: 'owasp';
  topTenCategory: OWASPTop10Category;
  risk: OWASPRiskRating;
  testingMethodology: OWASPTestingMethodology;
  vulnerabilityClass: string;
  attackVector: string[];
  impactAnalysis: OWASPImpactAnalysis;
}

export interface ISO27001Assessment extends ComplianceAssessment {
  framework: 'iso27001';
  controlFamily: ISO27001ControlFamily;
  controlObjective: string;
  implementationLevel: ISO27001ImplementationLevel;
  maturityLevel: ISO27001MaturityLevel;
  isms: ISMSImplementation;
  riskTreatment: RiskTreatmentPlan;
}

export interface SOC2Assessment extends ComplianceAssessment {
  framework: 'soc2';
  trustServicesPrinciple: SOC2TrustServicesPrinciple;
  criteriaType: SOC2CriteriaType;
  operatingEffectiveness: SOC2OperatingEffectiveness;
  designEffectiveness: SOC2DesignEffectiveness;
  controlTesting: SOC2ControlTesting;
  exceptions: SOC2Exception[];
}

// Base assessment interface
export interface ComplianceAssessment {
  id: string;
  framework: ComplianceFramework;
  requirement: ComplianceRequirement;
  status: ComplianceStatus;
  implementationScore: number;
  effectivenessScore: number;
  maturityScore: number;
  riskScore: number;
  findings: ComplianceFinding[];
  evidence: ComplianceEvidence[];
  controls: ImplementedControl[];
  gaps: ComplianceGap[];
  recommendations: ComplianceRecommendation[];
  testResults: TestResult[];
  reviewNotes: string;
  assessedBy: string;
  assessedAt: Date;
  reviewedBy?: string;
  reviewedAt?: Date;
  nextReviewDate: Date;
  metadata: AssessmentMetadata;
}

export interface ComplianceRequirement {
  id: string;
  framework: ComplianceFramework;
  section: string;
  subsection?: string;
  number: string;
  title: string;
  description: string;
  category: string;
  type: 'mandatory' | 'recommended' | 'optional';
  applicability: RequirementApplicability;
  references: ExternalReference[];
  relatedRequirements: string[];
  testingGuidance: TestingGuidance;
  acceptanceCriteria: AcceptanceCriteria[];
}

export interface RequirementApplicability {
  organizationSize: string[];
  industry: string[];
  riskLevel: RiskLevel[];
  systemTypes: string[];
  dataTypes: string[];
  geographicalRegions: string[];
  conditions: ApplicabilityCondition[];
}

export interface ApplicabilityCondition {
  condition: string;
  description: string;
  required: boolean;
  evaluation: string;
}

export interface TestingGuidance {
  methodology: string[];
  procedures: TestingProcedure[];
  sampleSizes: SampleSizeGuidance[];
  evidence: EvidenceRequirement[];
  tools: RecommendedTool[];
  skillsRequired: string[];
}

export interface TestingProcedure {
  id: string;
  name: string;
  description: string;
  steps: ProcedureStep[];
  expectedResults: string[];
  riskFactors: string[];
  frequency: string;
}

export interface ProcedureStep {
  stepNumber: number;
  description: string;
  action: string;
  expectedOutcome: string;
  evidenceToCollect: string[];
  tools: string[];
  skillLevel: 'basic' | 'intermediate' | 'advanced' | 'expert';
}

export interface SampleSizeGuidance {
  populationType: string;
  minimumSample: number;
  recommendedSample: number;
  samplingMethod: 'random' | 'systematic' | 'stratified' | 'judgmental';
  riskAdjustment: number;
}

export interface EvidenceRequirement {
  type: EvidenceType;
  description: string;
  mandatory: boolean;
  qualityRequirements: EvidenceQuality;
  retention: EvidenceRetention;
}

export interface EvidenceQuality {
  authenticity: 'required' | 'preferred' | 'optional';
  completeness: 'required' | 'preferred' | 'optional';
  accuracy: 'required' | 'preferred' | 'optional';
  timeliness: 'required' | 'preferred' | 'optional';
  independence: 'required' | 'preferred' | 'optional';
}

export interface EvidenceRetention {
  period: number;
  unit: 'days' | 'months' | 'years';
  location: string;
  format: string[];
  disposal: string;
}

export interface RecommendedTool {
  name: string;
  type: 'automated' | 'manual' | 'hybrid';
  vendor: string;
  version: string;
  purpose: string;
  effectiveness: number;
  cost: 'free' | 'low' | 'medium' | 'high';
}

export interface AcceptanceCriteria {
  id: string;
  description: string;
  type: 'quantitative' | 'qualitative' | 'binary';
  threshold: AcceptanceThreshold;
  measurement: MeasurementMethod;
  frequency: string;
}

export interface AcceptanceThreshold {
  operator: 'equals' | 'greater_than' | 'less_than' | 'between' | 'contains' | 'matches';
  value: any;
  unit?: string;
  tolerance?: number;
  conditions?: string[];
}

export interface MeasurementMethod {
  technique: string;
  formula?: string;
  dataSource: string[];
  frequency: string;
  automation: boolean;
  tools: string[];
}

// Finding and recommendation interfaces
export interface ComplianceFinding {
  id: string;
  auditId: string;
  assessmentId: string;
  requirementId: string;
  type: 'deficiency' | 'significant_deficiency' | 'material_weakness' | 'observation' | 'best_practice';
  severity: RiskLevel;
  title: string;
  description: string;
  impact: FindingImpact;
  rootCause: RootCauseAnalysis;
  evidence: ComplianceEvidence[];
  recommendations: ComplianceRecommendation[];
  status: 'open' | 'in_remediation' | 'resolved' | 'accepted_risk' | 'false_positive';
  assignedTo: string;
  dueDate: Date;
  resolvedAt?: Date;
  resolvedBy?: string;
  resolution: string;
  verificationEvidence: ComplianceEvidence[];
  recurrence: RecurrenceTracking;
  metadata: FindingMetadata;
  createdAt: Date;
  updatedAt: Date;
}

export interface FindingImpact {
  business: BusinessImpact;
  technical: TechnicalImpact;
  regulatory: RegulatoryImpact;
  reputation: ReputationImpact;
  financial: FinancialImpact;
  operational: OperationalImpact;
}

export interface BusinessImpact {
  severity: RiskLevel;
  description: string;
  affectedProcesses: string[];
  affectedStakeholders: string[];
  businessContinuity: 'none' | 'minor' | 'moderate' | 'major' | 'severe';
  customerImpact: 'none' | 'minor' | 'moderate' | 'major' | 'severe';
  revenueImpact: 'none' | 'minor' | 'moderate' | 'major' | 'severe';
}

export interface TechnicalImpact {
  severity: RiskLevel;
  description: string;
  affectedSystems: string[];
  dataIntegrity: 'none' | 'minor' | 'moderate' | 'major' | 'severe';
  availability: 'none' | 'minor' | 'moderate' | 'major' | 'severe';
  confidentiality: 'none' | 'minor' | 'moderate' | 'major' | 'severe';
  performance: 'none' | 'minor' | 'moderate' | 'major' | 'severe';
}

export interface RegulatoryImpact {
  severity: RiskLevel;
  description: string;
  frameworks: ComplianceFramework[];
  violations: RegulatoryViolation[];
  reportingRequired: boolean;
  penalties: RegulatoryPenalty[];
  timeframes: RegulatoryTimeframe[];
}

export interface RegulatoryViolation {
  framework: ComplianceFramework;
  requirement: string;
  violationType: 'procedural' | 'substantive' | 'disclosure' | 'reporting';
  severity: RiskLevel;
  precedent: boolean;
  frequency: 'isolated' | 'recurring' | 'systemic';
}

export interface RegulatoryPenalty {
  type: 'fine' | 'suspension' | 'revocation' | 'corrective_action' | 'consent_order';
  amount?: number;
  currency?: string;
  probability: number;
  precedent: boolean;
  mitigatingFactors: string[];
  aggravatingFactors: string[];
}

export interface RegulatoryTimeframe {
  action: string;
  deadline: Date;
  authority: string;
  consequences: string;
  extensions: TimeframeExtension[];
}

export interface TimeframeExtension {
  reason: string;
  duration: number;
  unit: 'days' | 'weeks' | 'months';
  approved: boolean;
  approvedBy?: string;
  approvedAt?: Date;
}

export interface ReputationImpact {
  severity: RiskLevel;
  description: string;
  stakeholders: string[];
  mediaAttention: 'none' | 'minimal' | 'moderate' | 'significant' | 'extensive';
  publicDisclosure: boolean;
  brandImpact: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
  trustImpact: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
}

export interface FinancialImpact {
  severity: RiskLevel;
  description: string;
  directCosts: CostCategory[];
  indirectCosts: CostCategory[];
  estimatedTotal: number;
  currency: string;
  timeframe: string;
  confidence: number;
}

export interface CostCategory {
  category: string;
  description: string;
  amount: number;
  recurring: boolean;
  frequency?: string;
  confidence: number;
}

export interface OperationalImpact {
  severity: RiskLevel;
  description: string;
  affectedOperations: string[];
  downtime: DowntimeEstimate;
  resourceRequirements: ResourceRequirement[];
  workflowDisruption: 'none' | 'minimal' | 'moderate' | 'significant' | 'severe';
}

export interface DowntimeEstimate {
  estimated: number;
  unit: 'minutes' | 'hours' | 'days';
  confidence: number;
  criticalPeriods: CriticalPeriod[];
}

export interface CriticalPeriod {
  name: string;
  start: Date;
  end: Date;
  impact: 'none' | 'low' | 'medium' | 'high' | 'critical';
  reason: string;
}

export interface ResourceRequirement {
  type: 'human' | 'technical' | 'financial' | 'infrastructure';
  description: string;
  quantity: number;
  unit: string;
  duration: number;
  durationUnit: 'hours' | 'days' | 'weeks' | 'months';
  availability: 'available' | 'limited' | 'unavailable';
}

export interface RootCauseAnalysis {
  methodology: 'fishbone' | '5_whys' | 'fault_tree' | 'barrier_analysis' | 'change_analysis' | 'custom';
  primaryCause: string;
  contributingFactors: ContributingFactor[];
  systemicIssues: SystemicIssue[];
  preventionOpportunities: PreventionOpportunity[];
  analysisEvidence: ComplianceEvidence[];
  analyst: string;
  analyzedAt: Date;
  reviewedBy?: string;
  reviewedAt?: Date;
}

export interface ContributingFactor {
  category: 'human' | 'process' | 'technology' | 'environment' | 'management';
  description: string;
  weight: number;
  likelihood: number;
  preventable: boolean;
  controls: string[];
}

export interface SystemicIssue {
  area: string;
  description: string;
  scope: 'local' | 'departmental' | 'organizational' | 'industry';
  recurrence: boolean;
  trend: 'increasing' | 'stable' | 'decreasing';
  underlying: string[];
}

export interface PreventionOpportunity {
  type: 'control' | 'process' | 'training' | 'technology' | 'policy';
  description: string;
  effectiveness: number;
  cost: 'low' | 'medium' | 'high';
  timeframe: string;
  dependencies: string[];
}

export interface RecurrenceTracking {
  isRecurring: boolean;
  previousOccurrences: PreviousOccurrence[];
  pattern: RecurrencePattern;
  preventionEffectiveness: number;
  escaltionNeeded: boolean;
}

export interface PreviousOccurrence {
  auditId: string;
  findingId: string;
  date: Date;
  severity: RiskLevel;
  resolved: boolean;
  resolution: string;
  effectiveness: number;
}

export interface RecurrencePattern {
  frequency: string;
  triggers: string[];
  conditions: string[];
  seasonality: boolean;
  correlation: PatternCorrelation[];
}

export interface PatternCorrelation {
  factor: string;
  correlation: number;
  significance: number;
  causal: boolean;
}

// Recommendation interfaces
export interface ComplianceRecommendation {
  id: string;
  findingId: string;
  title: string;
  description: string;
  type: 'immediate' | 'short_term' | 'long_term' | 'strategic';
  priority: RemediationPriority;
  category: 'control' | 'process' | 'policy' | 'training' | 'technology' | 'governance';
  implementation: ImplementationPlan;
  resources: ResourceEstimate;
  risks: ImplementationRisk[];
  benefits: ExpectedBenefit[];
  alternatives: Alternative[];
  dependencies: Dependency[];
  monitoring: MonitoringPlan;
  status: 'proposed' | 'approved' | 'in_progress' | 'completed' | 'rejected' | 'deferred';
  assignedTo: string;
  dueDate: Date;
  approvedBy?: string;
  approvedAt?: Date;
  completedAt?: Date;
  effectiveness: EffectivenessAssessment;
  metadata: RecommendationMetadata;
}

export interface ImplementationPlan {
  phases: ImplementationPhase[];
  timeline: ProjectTimeline;
  milestones: Milestone[];
  deliverables: Deliverable[];
  successCriteria: SuccessCriteria[];
  rollbackPlan: RollbackPlan;
}

export interface ImplementationPhase {
  phaseNumber: number;
  name: string;
  description: string;
  duration: number;
  durationUnit: 'days' | 'weeks' | 'months';
  startDate: Date;
  endDate: Date;
  prerequisites: string[];
  activities: PhaseActivity[];
  deliverables: string[];
  risks: string[];
  resources: PhaseResource[];
}

export interface PhaseActivity {
  id: string;
  name: string;
  description: string;
  duration: number;
  durationUnit: 'hours' | 'days' | 'weeks';
  dependencies: string[];
  assignedTo: string;
  skillsRequired: string[];
  tools: string[];
  outputs: string[];
}

export interface PhaseResource {
  type: 'human' | 'technical' | 'financial';
  description: string;
  quantity: number;
  unit: string;
  cost: number;
  currency: string;
  availability: string;
}

export interface ProjectTimeline {
  startDate: Date;
  endDate: Date;
  totalDuration: number;
  durationUnit: 'days' | 'weeks' | 'months';
  criticalPath: string[];
  bufferTime: number;
  contingencyTime: number;
}

export interface Milestone {
  id: string;
  name: string;
  description: string;
  dueDate: Date;
  type: 'phase_completion' | 'deliverable' | 'approval' | 'go_live' | 'review';
  criteria: string[];
  dependencies: string[];
  stakeholders: string[];
}

export interface Deliverable {
  id: string;
  name: string;
  description: string;
  type: 'document' | 'system' | 'process' | 'training' | 'policy' | 'report';
  format: string;
  quality: QualityRequirement[];
  acceptance: AcceptanceCriteria[];
  owner: string;
  reviewers: string[];
  dueDate: Date;
}

export interface QualityRequirement {
  aspect: string;
  description: string;
  measurement: string;
  threshold: string;
  verification: string;
}

export interface SuccessCriteria {
  criterion: string;
  description: string;
  measurement: MeasurementMethod;
  target: any;
  threshold: any;
  frequency: string;
  owner: string;
}

export interface RollbackPlan {
  triggers: RollbackTrigger[];
  procedures: RollbackProcedure[];
  timeline: string;
  resources: string[];
  notifications: string[];
  recovery: RecoveryPlan;
}

export interface RollbackTrigger {
  condition: string;
  description: string;
  threshold: any;
  monitor: string;
  automation: boolean;
  escalation: string[];
}

export interface RollbackProcedure {
  step: number;
  description: string;
  actions: string[];
  validation: string[];
  owner: string;
  duration: string;
  dependencies: string[];
}

export interface RecoveryPlan {
  objectives: string[];
  procedures: RecoveryProcedure[];
  resources: ResourceRequirement[];
  timeline: string;
  validation: string[];
}

export interface RecoveryProcedure {
  step: number;
  description: string;
  actions: string[];
  validation: string[];
  owner: string;
  duration: string;
  criticality: 'low' | 'medium' | 'high' | 'critical';
}

export interface ResourceEstimate {
  human: HumanResource[];
  technical: TechnicalResource[];
  financial: FinancialResource[];
  external: ExternalResource[];
  totalCost: number;
  currency: string;
  confidence: number;
  contingency: number;
}

export interface HumanResource {
  role: string;
  skills: string[];
  experience: string;
  quantity: number;
  duration: number;
  durationUnit: 'hours' | 'days' | 'weeks' | 'months';
  cost: number;
  availability: string;
  critical: boolean;
}

export interface TechnicalResource {
  type: 'hardware' | 'software' | 'infrastructure' | 'license' | 'subscription';
  description: string;
  specifications: Record<string, any>;
  quantity: number;
  cost: number;
  lifetime: number;
  lifetimeUnit: 'months' | 'years';
  maintenance: number;
  vendor: string;
}

export interface FinancialResource {
  category: string;
  description: string;
  amount: number;
  currency: string;
  type: 'capex' | 'opex';
  frequency: 'one_time' | 'monthly' | 'quarterly' | 'annually';
  approval: ApprovalRequirement;
}

export interface ApprovalRequirement {
  required: boolean;
  level: string;
  approver: string;
  process: string;
  timeline: string;
  documentation: string[];
}

export interface ExternalResource {
  type: 'consultant' | 'vendor' | 'service_provider' | 'auditor';
  description: string;
  provider: string;
  cost: number;
  currency: string;
  duration: number;
  durationUnit: 'days' | 'weeks' | 'months';
  contract: ContractRequirement;
}

export interface ContractRequirement {
  type: 'new' | 'amendment' | 'extension';
  terms: string[];
  sla: ServiceLevelAgreement[];
  penalties: ContractPenalty[];
  termination: TerminationClause[];
}

export interface ServiceLevelAgreement {
  metric: string;
  target: any;
  measurement: string;
  frequency: string;
  penalty: number;
  escalation: string[];
}

export interface ContractPenalty {
  trigger: string;
  type: 'financial' | 'service_credit' | 'termination';
  amount?: number;
  calculation: string;
  cap?: number;
}

export interface TerminationClause {
  reason: string;
  notice: string;
  obligations: string[];
  penalties: number;
  transition: string;
}

export interface ImplementationRisk {
  id: string;
  description: string;
  category: 'technical' | 'operational' | 'financial' | 'regulatory' | 'resource' | 'timeline';
  probability: number;
  impact: RiskLevel;
  riskScore: number;
  mitigation: RiskMitigation;
  contingency: RiskContingency;
  owner: string;
  status: 'identified' | 'assessed' | 'mitigated' | 'accepted' | 'transferred' | 'avoided';
}

export interface RiskMitigation {
  strategy: 'avoid' | 'mitigate' | 'transfer' | 'accept';
  actions: MitigationAction[];
  effectiveness: number;
  cost: number;
  timeline: string;
  monitoring: string[];
}

export interface MitigationAction {
  action: string;
  description: string;
  owner: string;
  dueDate: Date;
  status: 'planned' | 'in_progress' | 'completed' | 'cancelled';
  effectiveness: number;
  cost: number;
}

export interface RiskContingency {
  triggers: string[];
  actions: string[];
  resources: string[];
  timeline: string;
  cost: number;
  activation: ContingencyActivation;
}

export interface ContingencyActivation {
  criteria: string[];
  approval: string;
  notification: string[];
  timeline: string;
  validation: string[];
}

export interface ExpectedBenefit {
  category: 'risk_reduction' | 'cost_savings' | 'efficiency' | 'compliance' | 'reputation' | 'revenue';
  description: string;
  quantified: boolean;
  value?: number;
  unit?: string;
  currency?: string;
  timeframe: string;
  confidence: number;
  measurement: BenefitMeasurement;
  dependencies: string[];
}

export interface BenefitMeasurement {
  metric: string;
  baseline: any;
  target: any;
  frequency: string;
  method: string;
  validation: string[];
  owner: string;
}

export interface Alternative {
  id: string;
  name: string;
  description: string;
  pros: string[];
  cons: string[];
  cost: number;
  timeline: string;
  effectiveness: number;
  risk: RiskLevel;
  recommended: boolean;
  rationale: string;
}

export interface Dependency {
  id: string;
  description: string;
  type: 'internal' | 'external' | 'regulatory' | 'technical' | 'resource';
  criticality: 'low' | 'medium' | 'high' | 'blocking';
  status: 'pending' | 'in_progress' | 'completed' | 'blocked' | 'cancelled';
  owner: string;
  dueDate: Date;
  impact: DependencyImpact;
}

export interface DependencyImpact {
  scope: string[];
  timeline: number;
  timelineUnit: 'days' | 'weeks' | 'months';
  cost: number;
  quality: 'none' | 'minor' | 'moderate' | 'major' | 'severe';
  workaround: string;
}

export interface MonitoringPlan {
  metrics: MonitoringMetric[];
  frequency: string;
  reporting: ReportingPlan;
  escalation: EscalationPlan;
  reviews: ReviewSchedule[];
  automation: MonitoringAutomation;
}

export interface MonitoringMetric {
  name: string;
  description: string;
  formula: string;
  dataSource: string[];
  frequency: string;
  target: any;
  threshold: ThresholdDefinition[];
  owner: string;
  automation: boolean;
}

export interface ThresholdDefinition {
  level: 'green' | 'yellow' | 'red';
  operator: 'greater_than' | 'less_than' | 'equals' | 'between';
  value: any;
  action: string[];
  notification: string[];
}

export interface ReportingPlan {
  audiences: ReportingAudience[];
  formats: ReportFormat[];
  frequency: string;
  distribution: DistributionPlan;
  templates: ReportTemplate[];
}

export interface ReportingAudience {
  name: string;
  role: string;
  interests: string[];
  format: string;
  frequency: string;
  customization: AudienceCustomization;
}

export interface AudienceCustomization {
  metrics: string[];
  visualizations: string[];
  details: 'summary' | 'standard' | 'detailed';
  interactive: boolean;
  realTime: boolean;
}

export interface ReportFormat {
  type: 'dashboard' | 'email' | 'pdf' | 'excel' | 'api' | 'presentation';
  template: string;
  automation: boolean;
  interactivity: boolean;
  realTime: boolean;
}

export interface DistributionPlan {
  channels: DistributionChannel[];
  schedule: DistributionSchedule;
  security: DistributionSecurity;
  archiving: ArchivingPolicy;
}

export interface DistributionChannel {
  type: 'email' | 'portal' | 'api' | 'file_share' | 'database';
  configuration: Record<string, any>;
  recipients: string[];
  formatting: string;
  encryption: boolean;
}

export interface DistributionSchedule {
  frequency: string;
  time: string;
  timezone: string;
  businessDays: boolean;
  holidays: HolidayHandling;
}

export interface HolidayHandling {
  skip: boolean;
  delay: boolean;
  advance: boolean;
  calendar: string;
}

export interface DistributionSecurity {
  classification: string;
  encryption: boolean;
  authentication: boolean;
  authorization: string[];
  retention: RetentionPolicy;
  disposal: DisposalPolicy;
}

export interface RetentionPolicy {
  period: number;
  unit: 'days' | 'months' | 'years';
  trigger: 'creation' | 'access' | 'completion';
  exceptions: RetentionException[];
}

export interface RetentionException {
  condition: string;
  period: number;
  unit: 'days' | 'months' | 'years';
  justification: string;
}

export interface DisposalPolicy {
  method: string;
  verification: boolean;
  documentation: boolean;
  approval: string;
  notification: string[];
}

export interface ArchivingPolicy {
  trigger: string;
  location: string;
  format: string;
  indexing: boolean;
  retrieval: RetrievalPolicy;
}

export interface RetrievalPolicy {
  timeframe: string;
  approval: string;
  cost: number;
  process: string;
  verification: boolean;
}

export interface ReportTemplate {
  id: string;
  name: string;
  description: string;
  type: 'executive' | 'operational' | 'technical' | 'regulatory';
  sections: TemplateSection[];
  formatting: TemplateFormatting;
  branding: TemplateBranding;
}

export interface TemplateSection {
  name: string;
  order: number;
  required: boolean;
  content: SectionContent[];
  formatting: SectionFormatting;
}

export interface SectionContent {
  type: 'text' | 'table' | 'chart' | 'metric' | 'image' | 'list';
  source: string;
  configuration: Record<string, any>;
  styling: Record<string, any>;
}

export interface SectionFormatting {
  layout: string;
  spacing: string;
  alignment: string;
  borders: boolean;
  background: string;
}

export interface TemplateFormatting {
  pageSize: string;
  orientation: 'portrait' | 'landscape';
  margins: Record<string, number>;
  fonts: FontDefinition[];
  colors: ColorScheme;
  branding: boolean;
}

export interface FontDefinition {
  name: string;
  family: string;
  size: number;
  weight: string;
  style: string;
  usage: string[];
}

export interface ColorScheme {
  primary: string;
  secondary: string;
  accent: string;
  success: string;
  warning: string;
  error: string;
  neutral: string[];
}

export interface TemplateBranding {
  logo: boolean;
  logoPosition: string;
  organizationName: boolean;
  footer: string;
  watermark: boolean;
  disclaimer: string;
}

export interface EscalationPlan {
  levels: EscalationLevel[];
  triggers: EscalationTrigger[];
  procedures: EscalationProcedure[];
  communication: EscalationCommunication;
}

export interface EscalationLevel {
  level: number;
  name: string;
  description: string;
  criteria: string[];
  stakeholders: string[];
  timeframe: string;
  actions: string[];
  authority: string[];
}

export interface EscalationTrigger {
  condition: string;
  description: string;
  threshold: any;
  evaluation: string;
  frequency: string;
  automation: boolean;
}

export interface EscalationProcedure {
  step: number;
  description: string;
  actions: string[];
  timeframe: string;
  owner: string;
  approval: string;
  documentation: string[];
}

export interface EscalationCommunication {
  channels: string[];
  templates: string[];
  frequency: string;
  stakeholders: CommunicationStakeholder[];
  escalationMatrix: EscalationMatrix;
}

export interface CommunicationStakeholder {
  role: string;
  contact: string;
  method: string[];
  frequency: string;
  information: string[];
}

export interface EscalationMatrix {
  levels: Record<string, EscalationMatrixEntry>;
  relationships: MatrixRelationship[];
  authorities: AuthorityDefinition[];
}

export interface EscalationMatrixEntry {
  level: number;
  roles: string[];
  timeframe: string;
  authority: string[];
  responsibility: string[];
}

export interface MatrixRelationship {
  from: string;
  to: string;
  condition: string;
  timeframe: string;
  automatic: boolean;
}

export interface AuthorityDefinition {
  role: string;
  scope: string[];
  limits: AuthorityLimit[];
  delegation: DelegationRule[];
}

export interface AuthorityLimit {
  type: 'financial' | 'operational' | 'regulatory' | 'technical';
  limit: any;
  unit: string;
  conditions: string[];
}

export interface DelegationRule {
  to: string;
  scope: string[];
  conditions: string[];
  duration: string;
  approval: string;
}

export interface ReviewSchedule {
  type: 'operational' | 'tactical' | 'strategic';
  frequency: string;
  participants: string[];
  agenda: ReviewAgenda;
  deliverables: string[];
  decisions: DecisionPoint[];
}

export interface ReviewAgenda {
  items: AgendaItem[];
  duration: string;
  preparation: string[];
  materials: string[];
}

export interface AgendaItem {
  topic: string;
  duration: string;
  presenter: string;
  type: 'information' | 'discussion' | 'decision';
  materials: string[];
  outcomes: string[];
}

export interface DecisionPoint {
  decision: string;
  criteria: string[];
  options: DecisionOption[];
  authority: string;
  timeframe: string;
  dependencies: string[];
}

export interface DecisionOption {
  option: string;
  description: string;
  pros: string[];
  cons: string[];
  cost: number;
  risk: RiskLevel;
  timeline: string;
  resources: string[];
}

export interface MonitoringAutomation {
  dataCollection: AutomationCapability;
  processing: AutomationCapability;
  analysis: AutomationCapability;
  reporting: AutomationCapability;
  alerting: AutomationCapability;
  remediation: AutomationCapability;
}

export interface AutomationCapability {
  enabled: boolean;
  level: 'none' | 'partial' | 'full';
  tools: string[];
  triggers: string[];
  workflows: AutomationWorkflow[];
  exceptions: string[];
  overrides: OverrideCapability[];
}

export interface AutomationWorkflow {
  name: string;
  description: string;
  trigger: string;
  steps: WorkflowStep[];
  conditions: WorkflowCondition[];
  outputs: string[];
  error: ErrorHandling;
}

export interface WorkflowStep {
  step: number;
  action: string;
  parameters: Record<string, any>;
  conditions: string[];
  timeout: number;
  retry: RetryPolicy;
  rollback: string[];
}

export interface WorkflowCondition {
  condition: string;
  evaluation: string;
  action: 'continue' | 'skip' | 'stop' | 'escalate';
  notification: string[];
}

export interface ErrorHandling {
  strategy: 'stop' | 'continue' | 'retry' | 'escalate' | 'rollback';
  notification: string[];
  logging: boolean;
  recovery: string[];
  timeout: number;
}

export interface RetryPolicy {
  enabled: boolean;
  maxAttempts: number;
  delay: number;
  backoff: 'linear' | 'exponential' | 'random';
  conditions: string[];
}

export interface OverrideCapability {
  trigger: string;
  authority: string[];
  approval: string;
  duration: string;
  logging: boolean;
  notification: string[];
}

export interface EffectivenessAssessment {
  planned: EffectivenessMetric[];
  actual: EffectivenessMetric[];
  variance: VarianceAnalysis[];
  lessonsLearned: LessonLearned[];
  improvements: ImprovementOpportunity[];
  assessment: OverallAssessment;
}

export interface EffectivenessMetric {
  metric: string;
  target: any;
  actual: any;
  measurement: string;
  period: string;
  confidence: number;
  trend: 'improving' | 'stable' | 'declining';
}

export interface VarianceAnalysis {
  metric: string;
  variance: number;
  significance: 'none' | 'minor' | 'moderate' | 'major' | 'critical';
  causes: string[];
  impact: string[];
  corrective: string[];
}

export interface LessonLearned {
  category: string;
  description: string;
  impact: 'positive' | 'negative' | 'neutral';
  applicability: string[];
  recommendation: string;
  priority: 'low' | 'medium' | 'high';
}

export interface ImprovementOpportunity {
  area: string;
  description: string;
  benefit: string;
  effort: 'low' | 'medium' | 'high';
  priority: 'low' | 'medium' | 'high';
  timeline: string;
  resources: string[];
}

export interface OverallAssessment {
  rating: 'excellent' | 'good' | 'satisfactory' | 'needs_improvement' | 'unsatisfactory';
  summary: string;
  achievements: string[];
  challenges: string[];
  recommendations: string[];
  nextSteps: string[];
}

// Evidence and documentation interfaces
export interface ComplianceEvidence {
  id: string;
  type: EvidenceType;
  title: string;
  description: string;
  source: string;
  format: string;
  size: number;
  hash: string;
  location: string;
  url?: string;
  metadata: EvidenceMetadata;
  quality: EvidenceQualityAssessment;
  authenticity: AuthenticityVerification;
  relevance: RelevanceAssessment;
  retention: EvidenceRetentionInfo;
  access: AccessControl;
  chain: ChainOfCustody[];
  reviews: EvidenceReview[];
  tags: string[];
  createdAt: Date;
  updatedAt: Date;
  createdBy: string;
  collectedBy: string;
}

export interface EvidenceMetadata {
  framework: ComplianceFramework;
  requirement: string;
  assessment: string;
  finding?: string;
  recommendation?: string;
  auditTrail: AuditTrailEntry[];
  relatedEvidence: string[];
  supersedes?: string;
  supersededBy?: string;
  version: number;
  classification: string;
  sensitivity: string;
  jurisdiction: string;
  language: string;
  encoding?: string;
  checksum: string;
  digitalSignature?: DigitalSignature;
}

export interface EvidenceQualityAssessment {
  completeness: QualityScore;
  accuracy: QualityScore;
  reliability: QualityScore;
  relevance: QualityScore;
  timeliness: QualityScore;
  independence: QualityScore;
  overall: QualityScore;
  assessor: string;
  assessedAt: Date;
  notes: string;
}

export interface QualityScore {
  score: number;
  criteria: string[];
  rationale: string;
  confidence: number;
  limitations: string[];
}

export interface AuthenticityVerification {
  verified: boolean;
  method: string[];
  verifier: string;
  verifiedAt: Date;
  certificate?: string;
  signature?: string;
  timestamp?: string;
  challenges: string[];
  confidence: number;
}

export interface RelevanceAssessment {
  score: number;
  criteria: string[];
  mapping: RequirementMapping[];
  coverage: CoverageAnalysis;
  gaps: string[];
  assessor: string;
  assessedAt: Date;
}

export interface RequirementMapping {
  requirement: string;
  coverage: 'full' | 'partial' | 'minimal' | 'none';
  evidence: string[];
  gaps: string[];
  strength: number;
}

export interface CoverageAnalysis {
  scope: string[];
  completeness: number;
  depth: number;
  breadth: number;
  adequacy: 'adequate' | 'inadequate' | 'excessive';
  recommendations: string[];
}

export interface EvidenceRetentionInfo {
  schedule: RetentionSchedule;
  triggers: RetentionTrigger[];
  location: StorageLocation;
  backup: BackupStrategy;
  disposal: DisposalPlan;
}

export interface RetentionSchedule {
  period: number;
  unit: 'days' | 'months' | 'years';
  startDate: Date;
  endDate: Date;
  extensions: RetentionExtension[];
  holds: LegalHold[];
}

export interface RetentionTrigger {
  event: string;
  description: string;
  automatic: boolean;
  action: 'extend' | 'accelerate' | 'hold' | 'dispose';
  notification: string[];
}

export interface RetentionExtension {
  reason: string;
  period: number;
  unit: 'days' | 'months' | 'years';
  approvedBy: string;
  approvedAt: Date;
  documentation: string;
}

export interface LegalHold {
  id: string;
  reason: string;
  authority: string;
  startDate: Date;
  endDate?: Date;
  scope: string[];
  instructions: string;
  contact: string;
}

export interface StorageLocation {
  primary: StorageDetails;
  backup?: StorageDetails;
  archive?: StorageDetails;
  jurisdiction: string;
  compliance: string[];
}

export interface StorageDetails {
  type: 'local' | 'cloud' | 'hybrid' | 'third_party';
  provider: string;
  location: string;
  security: SecurityMeasures;
  availability: AvailabilityRequirements;
  performance: PerformanceRequirements;
}

export interface SecurityMeasures {
  encryption: EncryptionDetails;
  access: AccessSecurity;
  monitoring: SecurityMonitoring;
  compliance: SecurityCompliance[];
}

export interface EncryptionDetails {
  atRest: boolean;
  inTransit: boolean;
  algorithm: string;
  keyLength: number;
  keyManagement: string;
  rotation: string;
}

export interface AccessSecurity {
  authentication: string[];
  authorization: string;
  logging: boolean;
  monitoring: boolean;
  restrictions: AccessRestriction[];
}

export interface AccessRestriction {
  type: 'time' | 'location' | 'device' | 'network' | 'role';
  constraint: string;
  enforcement: string;
  exceptions: string[];
}

export interface SecurityMonitoring {
  intrusion: boolean;
  integrity: boolean;
  access: boolean;
  changes: boolean;
  alerts: string[];
  reporting: string;
}

export interface SecurityCompliance {
  framework: string;
  certification: string;
  audit: string;
  reporting: string;
  contact: string;
}

export interface AvailabilityRequirements {
  uptime: number;
  recovery: RecoveryRequirements;
  backup: BackupRequirements;
  disaster: DisasterRecovery;
}

export interface RecoveryRequirements {
  rto: number; // Recovery Time Objective
  rpo: number; // Recovery Point Objective
  procedures: string[];
  testing: string;
  documentation: string;
}

export interface BackupRequirements {
  frequency: string;
  retention: number;
  testing: string;
  offsite: boolean;
  encryption: boolean;
}

export interface DisasterRecovery {
  plan: string;
  testing: string;
  procedures: string[];
  contacts: string[];
  resources: string[];
}

export interface PerformanceRequirements {
  latency: number;
  throughput: number;
  capacity: number;
  scalability: string;
  monitoring: string[];
}

export interface BackupStrategy {
  frequency: string;
  retention: BackupRetention;
  verification: BackupVerification;
  recovery: BackupRecovery;
  offsite: boolean;
  encryption: boolean;
}

export interface BackupRetention {
  daily: number;
  weekly: number;
  monthly: number;
  yearly: number;
  policy: string;
}

export interface BackupVerification {
  method: string[];
  frequency: string;
  automation: boolean;
  reporting: string;
  escalation: string[];
}

export interface BackupRecovery {
  procedures: string[];
  testing: string;
  rto: number;
  rpo: number;
  validation: string[];
}

export interface DisposalPlan {
  method: string;
  verification: boolean;
  documentation: string;
  approval: string;
  notification: string[];
  certificate: boolean;
}

export interface AccessControl {
  permissions: Permission[];
  roles: string[];
  restrictions: string[];
  audit: boolean;
  encryption: boolean;
  watermarking: boolean;
}

export interface Permission {
  principal: string;
  type: 'user' | 'role' | 'group';
  access: 'read' | 'write' | 'delete' | 'admin';
  conditions: string[];
  expiry?: Date;
  justification: string;
}

export interface ChainOfCustody {
  timestamp: Date;
  action: 'created' | 'accessed' | 'modified' | 'copied' | 'moved' | 'deleted' | 'restored';
  actor: string;
  location: string;
  reason: string;
  hash: string;
  signature?: string;
  witness?: string;
  notes: string;
}

export interface EvidenceReview {
  id: string;
  reviewer: string;
  reviewedAt: Date;
  type: 'quality' | 'relevance' | 'authenticity' | 'completeness' | 'compliance';
  outcome: 'approved' | 'rejected' | 'conditional' | 'requires_modification';
  findings: string[];
  recommendations: string[];
  conditions: string[];
  nextReview?: Date;
  escalation: boolean;
}

export interface AuditTrailEntry {
  timestamp: Date;
  action: string;
  actor: string;
  details: Record<string, any>;
  ipAddress?: string;
  userAgent?: string;
  sessionId?: string;
  hash: string;
  signature?: string;
}

export interface DigitalSignature {
  algorithm: string;
  certificate: string;
  signature: string;
  timestamp: Date;
  valid: boolean;
  verifiedAt?: Date;
  verifiedBy?: string;
}

// Framework-specific types
export type OWASPTop10Category =
  | 'A01_Broken_Access_Control'
  | 'A02_Cryptographic_Failures'
  | 'A03_Injection'
  | 'A04_Insecure_Design'
  | 'A05_Security_Misconfiguration'
  | 'A06_Vulnerable_Components'
  | 'A07_Identification_Authentication_Failures'
  | 'A08_Software_Data_Integrity_Failures'
  | 'A09_Security_Logging_Monitoring_Failures'
  | 'A10_Server_Side_Request_Forgery';

export interface OWASPRiskRating {
  likelihood: 'low' | 'medium' | 'high';
  impact: 'low' | 'medium' | 'high';
  overall: 'low' | 'medium' | 'high' | 'critical';
  score: number;
  factors: OWASPRiskFactor[];
}

export interface OWASPRiskFactor {
  category: 'threat_agent' | 'attack_vector' | 'weakness_prevalence' | 'weakness_detectability' | 'technical_impact' | 'business_impact';
  factor: string;
  rating: number;
  justification: string;
}

export interface OWASPTestingMethodology {
  phase: 'information_gathering' | 'configuration_management' | 'identity_management' | 'authentication' | 'authorization' | 'session_management' | 'input_validation' | 'error_handling' | 'cryptography' | 'business_logic' | 'client_side';
  techniques: string[];
  tools: string[];
  coverage: number;
  automation: boolean;
}

export interface OWASPImpactAnalysis {
  confidentiality: 'none' | 'limited' | 'considerable' | 'extensive';
  integrity: 'none' | 'minimal' | 'moderate' | 'complete';
  availability: 'none' | 'minimal' | 'moderate' | 'complete';
  accountability: 'none' | 'minimal' | 'moderate' | 'complete';
  nonRepudiation: 'none' | 'minimal' | 'moderate' | 'complete';
  description: string;
  businessImpact: string;
  technicalImpact: string;
}

export type ISO27001ControlFamily =
  | 'A.5_Organizational_Controls'
  | 'A.6_People_Controls'
  | 'A.7_Physical_Controls'
  | 'A.8_Technological_Controls';

export type ISO27001ImplementationLevel = 'not_implemented' | 'partially_implemented' | 'largely_implemented' | 'fully_implemented';

export type ISO27001MaturityLevel = 'initial' | 'managed' | 'defined' | 'quantitatively_managed' | 'optimizing';

export interface ISMSImplementation {
  established: boolean;
  scope: string;
  policy: ISMSPolicy;
  governance: ISMSGovernance;
  processes: ISMSProcess[];
  monitoring: ISMSMonitoring;
  improvement: ISMSImprovement;
}

export interface ISMSPolicy {
  approved: boolean;
  version: string;
  approvedBy: string;
  approvedAt: Date;
  nextReview: Date;
  communication: PolicyCommunication;
  understanding: PolicyUnderstanding;
}

export interface PolicyCommunication {
  method: string[];
  audience: string[];
  frequency: string;
  effectiveness: number;
  feedback: string[];
}

export interface PolicyUnderstanding {
  assessed: boolean;
  method: string;
  score: number;
  gaps: string[];
  training: string[];
}

export interface ISMSGovernance {
  leadership: LeadershipCommitment;
  roles: ISMSRole[];
  responsibilities: ISMSResponsibility[];
  authority: ISMSAuthority[];
  resources: ISMSResource[];
}

export interface LeadershipCommitment {
  demonstrated: boolean;
  evidence: string[];
  communication: string[];
  resources: string[];
  integration: string[];
}

export interface ISMSRole {
  title: string;
  description: string;
  responsibilities: string[];
  authority: string[];
  qualifications: string[];
  training: string[];
  assigned: boolean;
  assignee?: string;
}

export interface ISMSResponsibility {
  area: string;
  description: string;
  owner: string;
  accountability: string[];
  reporting: string[];
  escalation: string[];
}

export interface ISMSAuthority {
  role: string;
  scope: string[];
  decisions: string[];
  approvals: string[];
  delegation: DelegationAuthority[];
}

export interface DelegationAuthority {
  to: string;
  scope: string[];
  conditions: string[];
  reporting: string[];
  review: string;
}

export interface ISMSResource {
  type: 'human' | 'financial' | 'technological' | 'infrastructure' | 'information';
  description: string;
  allocated: boolean;
  adequate: boolean;
  justification: string;
  monitoring: string[];
}

export interface ISMSProcess {
  name: string;
  description: string;
  owner: string;
  inputs: ProcessInput[];
  outputs: ProcessOutput[];
  activities: ProcessActivity[];
  controls: ProcessControl[];
  metrics: ProcessMetric[];
  documentation: ProcessDocumentation;
}

export interface ProcessInput {
  name: string;
  source: string;
  quality: string;
  frequency: string;
  dependencies: string[];
}

export interface ProcessOutput {
  name: string;
  destination: string;
  quality: string;
  frequency: string;
  consumers: string[];
}

export interface ProcessActivity {
  name: string;
  description: string;
  owner: string;
  inputs: string[];
  outputs: string[];
  controls: string[];
  resources: string[];
  skills: string[];
}

export interface ProcessControl {
  name: string;
  type: 'preventive' | 'detective' | 'corrective';
  description: string;
  implementation: string;
  testing: string;
  effectiveness: number;
}

export interface ProcessMetric {
  name: string;
  description: string;
  formula: string;
  target: any;
  frequency: string;
  owner: string;
  reporting: string[];
}

export interface ProcessDocumentation {
  procedures: string[];
  workInstructions: string[];
  forms: string[];
  templates: string[];
  version: string;
  approval: string;
  distribution: string[];
}

export interface ISMSMonitoring {
  plan: MonitoringPlan;
  execution: MonitoringExecution;
  analysis: MonitoringAnalysis;
  reporting: MonitoringReporting;
  improvement: MonitoringImprovement;
}

export interface MonitoringExecution {
  schedule: string;
  methods: string[];
  tools: string[];
  responsible: string[];
  coverage: number;
  quality: number;
}

export interface MonitoringAnalysis {
  frequency: string;
  methods: string[];
  trends: TrendAnalysis[];
  insights: AnalysisInsight[];
  recommendations: string[];
}

export interface AnalysisInsight {
  area: string;
  finding: string;
  significance: 'low' | 'medium' | 'high';
  implications: string[];
  actions: string[];
}

export interface MonitoringReporting {
  audience: string[];
  frequency: string;
  format: string;
  distribution: string[];
  feedback: string[];
}

export interface MonitoringImprovement {
  opportunities: string[];
  implemented: string[];
  effectiveness: number;
  lessons: string[];
}

export interface ISMSImprovement {
  policy: ImprovementPolicy;
  processes: ImprovementProcess[];
  culture: ImprovementCulture;
  innovation: ImprovementInnovation;
}

export interface ImprovementPolicy {
  established: boolean;
  scope: string;
  objectives: string[];
  methods: string[];
  resources: string[];
  governance: string[];
}

export interface ImprovementProcess {
  name: string;
  description: string;
  triggers: string[];
  methodology: string;
  stages: ImprovementStage[];
  governance: string[];
  measurement: string[];
}

export interface ImprovementStage {
  name: string;
  description: string;
  activities: string[];
  deliverables: string[];
  criteria: string[];
  approval: string;
}

export interface ImprovementCulture {
  awareness: number;
  participation: number;
  innovation: number;
  learning: number;
  feedback: FeedbackCulture;
}

export interface FeedbackCulture {
  mechanisms: string[];
  frequency: string;
  response: string;
  improvement: string[];
  recognition: string[];
}

export interface ImprovementInnovation {
  encouraged: boolean;
  support: string[];
  resources: string[];
  recognition: string[];
  implementation: string[];
}

export interface RiskTreatmentPlan {
  risks: IdentifiedRisk[];
  treatments: RiskTreatment[];
  monitoring: RiskMonitoring;
  review: RiskReview;
  communication: RiskCommunication;
}

export interface IdentifiedRisk {
  id: string;
  description: string;
  category: string;
  source: string[];
  impact: RiskImpact;
  likelihood: RiskLikelihood;
  level: RiskLevel;
  tolerance: RiskTolerance;
  owner: string;
}

export interface RiskImpact {
  confidentiality: number;
  integrity: number;
  availability: number;
  business: number;
  reputation: number;
  financial: number;
  legal: number;
  overall: number;
}

export interface RiskLikelihood {
  probability: number;
  frequency: string;
  conditions: string[];
  historical: number;
  trend: 'increasing' | 'stable' | 'decreasing';
}

export interface RiskTolerance {
  level: 'very_low' | 'low' | 'medium' | 'high' | 'very_high';
  criteria: string[];
  justification: string;
  approval: string;
  review: string;
}

export interface RiskTreatment {
  riskId: string;
  strategy: 'avoid' | 'mitigate' | 'transfer' | 'accept';
  controls: TreatmentControl[];
  timeline: string;
  resources: string[];
  owner: string;
  monitoring: string[];
  effectiveness: number;
}

export interface TreatmentControl {
  id: string;
  name: string;
  type: 'preventive' | 'detective' | 'corrective';
  description: string;
  implementation: ControlImplementation;
  testing: ControlTesting;
  effectiveness: ControlEffectiveness;
}

export interface ControlImplementation {
  status: 'planned' | 'in_progress' | 'implemented' | 'deferred';
  timeline: string;
  resources: string[];
  dependencies: string[];
  milestones: string[];
  owner: string;
}

export interface ControlTesting {
  frequency: string;
  method: string[];
  criteria: string[];
  results: TestingResult[];
  effectiveness: number;
  recommendations: string[];
}

export interface TestingResult {
  date: Date;
  method: string;
  outcome: 'effective' | 'partially_effective' | 'ineffective';
  findings: string[];
  recommendations: string[];
  tester: string;
}

export interface ControlEffectiveness {
  design: number;
  implementation: number;
  operation: number;
  overall: number;
  assessment: EffectivenessAssessment;
}

export interface RiskMonitoring {
  indicators: RiskIndicator[];
  thresholds: RiskThreshold[];
  reporting: RiskReporting;
  escalation: RiskEscalation;
}

export interface RiskIndicator {
  name: string;
  description: string;
  type: 'leading' | 'lagging' | 'concurrent';
  measurement: string;
  frequency: string;
  source: string[];
  owner: string;
}

export interface RiskThreshold {
  indicator: string;
  level: 'green' | 'yellow' | 'red';
  value: any;
  action: string[];
  escalation: string[];
  frequency: string;
}

export interface RiskReporting {
  audience: string[];
  frequency: string;
  format: string;
  content: string[];
  distribution: string[];
  feedback: string[];
}

export interface RiskEscalation {
  triggers: string[];
  levels: string[];
  procedures: string[];
  timelines: string[];
  authorities: string[];
}

export interface RiskReview {
  frequency: string;
  scope: string[];
  participants: string[];
  agenda: string[];
  outcomes: ReviewOutcome[];
  documentation: string[];
}

export interface ReviewOutcome {
  decision: string;
  rationale: string;
  actions: string[];
  timeline: string;
  owner: string;
  follow_up: string;
}

export interface RiskCommunication {
  stakeholders: RiskStakeholder[];
  methods: string[];
  frequency: string;
  content: string[];
  feedback: string[];
  training: RiskTraining[];
}

export interface RiskStakeholder {
  group: string;
  interests: string[];
  communication: string[];
  frequency: string;
  format: string;
  feedback: string[];
}

export interface RiskTraining {
  audience: string[];
  content: string[];
  frequency: string;
  delivery: string[];
  assessment: string[];
  effectiveness: number;
}

export type SOC2TrustServicesPrinciple = 'security' | 'availability' | 'processing_integrity' | 'confidentiality' | 'privacy';

export type SOC2CriteriaType = 'common_criteria' | 'additional_criteria';

export interface SOC2OperatingEffectiveness {
  tested: boolean;
  period: SOC2TestingPeriod;
  frequency: string;
  methodology: string[];
  sampleSize: number;
  exceptions: number;
  deficiencies: SOC2Deficiency[];
  conclusion: 'effective' | 'ineffective' | 'not_tested';
}

export interface SOC2TestingPeriod {
  startDate: Date;
  endDate: Date;
  coverage: number;
  justification: string;
  gaps: string[];
}

export interface SOC2DesignEffectiveness {
  assessed: boolean;
  date: Date;
  methodology: string[];
  conclusion: 'effective' | 'ineffective' | 'not_assessed';
  deficiencies: SOC2Deficiency[];
  recommendations: string[];
}

export interface SOC2ControlTesting {
  approach: 'inquiry' | 'observation' | 'inspection' | 'reperformance';
  procedures: TestingProcedure[];
  evidence: string[];
  results: TestingResult[];
  exceptions: SOC2Exception[];
  conclusion: string;
}

export interface SOC2Exception {
  id: string;
  description: string;
  cause: string;
  impact: SOC2ExceptionImpact;
  magnitude: 'isolated' | 'systematic' | 'pervasive';
  timeframe: string;
  remediation: ExceptionRemediation;
  status: 'open' | 'remediated' | 'management_response';
}

export interface SOC2ExceptionImpact {
  principle: SOC2TrustServicesPrinciple[];
  severity: 'low' | 'medium' | 'high';
  scope: string[];
  users: number;
  transactions: number;
  financial: number;
  reputation: 'none' | 'minimal' | 'moderate' | 'significant';
}

export interface ExceptionRemediation {
  plan: string;
  timeline: string;
  owner: string;
  resources: string[];
  monitoring: string[];
  testing: string[];
  effectiveness: number;
}

export interface SOC2Deficiency {
  id: string;
  type: 'design' | 'operating';
  description: string;
  criteria: string;
  root_cause: string;
  impact: SOC2DeficiencyImpact;
  remediation: DeficiencyRemediation;
  status: 'open' | 'remediated';
}

export interface SOC2DeficiencyImpact {
  severity: 'significant' | 'material_weakness';
  principle: SOC2TrustServicesPrinciple[];
  controls: string[];
  processes: string[];
  compensating: string[];
}

export interface DeficiencyRemediation {
  description: string;
  timeline: string;
  owner: string;
  milestones: string[];
  testing: string[];
  validation: string[];
  effectiveness: EffectivenessValidation;
}

export interface EffectivenessValidation {
  method: string[];
  period: string;
  criteria: string[];
  results: ValidationResult[];
  conclusion: string;
}

export interface ValidationResult {
  date: Date;
  method: string;
  outcome: 'satisfactory' | 'unsatisfactory';
  findings: string[];
  follow_up: string[];
  validator: string;
}

// Metrics and reporting interfaces
export interface ComplianceMetrics {
  overall: OverallComplianceMetrics;
  byFramework: Record<ComplianceFramework, FrameworkMetrics>;
  trends: ComplianceTrend[];
  benchmarks: ComplianceBenchmark[];
  maturity: ComplianceMaturity;
  performance: CompliancePerformance;
}

export interface OverallComplianceMetrics {
  score: number;
  level: 'non_compliant' | 'partially_compliant' | 'largely_compliant' | 'fully_compliant';
  requirements: RequirementMetrics;
  findings: FindingMetrics;
  remediation: RemediationMetrics;
  costs: CostMetrics;
  efficiency: EfficiencyMetrics;
}

export interface RequirementMetrics {
  total: number;
  compliant: number;
  nonCompliant: number;
  partiallyCompliant: number;
  notApplicable: number;
  pendingReview: number;
  coverage: number;
  effectiveness: number;
}

export interface FindingMetrics {
  total: number;
  open: number;
  resolved: number;
  inRemediation: number;
  overdue: number;
  bySeverity: Record<RiskLevel, number>;
  byType: Record<string, number>;
  recurrence: number;
  avgResolutionTime: number;
}

export interface RemediationMetrics {
  planned: number;
  inProgress: number;
  completed: number;
  overdue: number;
  effectiveness: number;
  avgImplementationTime: number;
  cost: number;
  resourceUtilization: number;
}

export interface CostMetrics {
  total: number;
  currency: string;
  breakdown: CostBreakdown;
  budget: BudgetMetrics;
  roi: ROIMetrics;
  forecasts: CostForecast[];
}

export interface CostBreakdown {
  assessment: number;
  remediation: number;
  monitoring: number;
  training: number;
  tools: number;
  consulting: number;
  penalties: number;
  opportunity: number;
}

export interface BudgetMetrics {
  allocated: number;
  spent: number;
  committed: number;
  remaining: number;
  variance: number;
  utilization: number;
}

export interface ROIMetrics {
  investment: number;
  benefits: number;
  ratio: number;
  payback: number;
  npv: number;
  irr: number;
}

export interface CostForecast {
  period: string;
  amount: number;
  confidence: number;
  assumptions: string[];
  risks: string[];
  scenarios: ForecastScenario[];
}

export interface ForecastScenario {
  name: string;
  probability: number;
  amount: number;
  drivers: string[];
  mitigation: string[];
}

export interface EfficiencyMetrics {
  automation: number;
  manualEffort: number;
  cycleTime: number;
  throughput: number;
  quality: number;
  rework: number;
  productivity: number;
  satisfaction: number;
}

export interface FrameworkMetrics {
  framework: ComplianceFramework;
  version: string;
  score: number;
  level: string;
  requirements: RequirementMetrics;
  maturity: MaturityMetrics;
  gaps: GapMetrics;
  priorities: PriorityMetrics;
  timeline: TimelineMetrics;
  resources: ResourceMetrics;
}

export interface MaturityMetrics {
  overall: number;
  dimensions: Record<string, number>;
  progression: MaturityProgression[];
  targets: MaturityTarget[];
  gaps: MaturityGap[];
}

export interface MaturityProgression {
  dimension: string;
  current: number;
  previous: number;
  change: number;
  trend: 'improving' | 'stable' | 'declining';
  trajectory: ProgressTrajectory;
}

export interface ProgressTrajectory {
  predicted: number;
  confidence: number;
  timeframe: string;
  factors: string[];
  risks: string[];
}

export interface MaturityTarget {
  dimension: string;
  current: number;
  target: number;
  gap: number;
  timeline: string;
  investment: number;
  probability: number;
}

export interface MaturityGap {
  dimension: string;
  gap: number;
  priority: 'low' | 'medium' | 'high' | 'critical';
  effort: 'low' | 'medium' | 'high';
  impact: 'low' | 'medium' | 'high';
  dependencies: string[];
}

export interface GapMetrics {
  total: number;
  critical: number;
  high: number;
  medium: number;
  low: number;
  coverage: number;
  effort: GapEffortMetrics;
  timeline: GapTimelineMetrics;
}

export interface GapEffortMetrics {
  total: number;
  breakdown: Record<string, number>;
  capacity: number;
  utilization: number;
  constraints: string[];
}

export interface GapTimelineMetrics {
  shortest: number;
  longest: number;
  average: number;
  critical: number;
  dependencies: number;
  parallel: number;
}

export interface PriorityMetrics {
  distribution: Record<RemediationPriority, number>;
  capacity: PriorityCapacity;
  allocation: PriorityAllocation;
  effectiveness: PriorityEffectiveness;
}

export interface PriorityCapacity {
  available: number;
  utilized: number;
  reserved: number;
  overflow: number;
  bottlenecks: string[];
}

export interface PriorityAllocation {
  immediate: number;
  critical: number;
  high: number;
  medium: number;
  low: number;
  alignment: number;
}

export interface PriorityEffectiveness {
  riskReduction: number;
  costBenefit: number;
  timeEfficiency: number;
  resourceOptimization: number;
  stakeholderSatisfaction: number;
}

export interface TimelineMetrics {
  duration: number;
  milestones: number;
  completed: number;
  onTrack: number;
  delayed: number;
  critical: number;
  buffer: number;
}

export interface ResourceMetrics {
  allocated: ResourceAllocation;
  utilized: ResourceUtilization;
  efficiency: ResourceEfficiency;
  constraints: ResourceConstraint[];
  optimization: ResourceOptimization;
}

export interface ResourceAllocation {
  human: number;
  financial: number;
  technical: number;
  external: number;
  total: number;
  adequacy: number;
}

export interface ResourceUtilization {
  current: number;
  peak: number;
  average: number;
  efficiency: number;
  waste: number;
  optimization: number;
}

export interface ResourceEfficiency {
  productivity: number;
  quality: number;
  speed: number;
  cost: number;
  satisfaction: number;
  innovation: number;
}

export interface ResourceConstraint {
  type: string;
  description: string;
  impact: 'low' | 'medium' | 'high' | 'critical';
  duration: string;
  mitigation: string[];
  alternatives: string[];
}

export interface ResourceOptimization {
  opportunities: OptimizationOpportunity[];
  implemented: OptimizationImplemented[];
  potential: OptimizationPotential;
  barriers: OptimizationBarrier[];
}

export interface OptimizationOpportunity {
  area: string;
  description: string;
  benefit: number;
  effort: 'low' | 'medium' | 'high';
  timeline: string;
  risk: 'low' | 'medium' | 'high';
  priority: number;
}

export interface OptimizationImplemented {
  area: string;
  description: string;
  benefit: number;
  cost: number;
  timeline: string;
  effectiveness: number;
  lessons: string[];
}

export interface OptimizationPotential {
  total: number;
  achievable: number;
  timeline: string;
  investment: number;
  roi: number;
  confidence: number;
}

export interface OptimizationBarrier {
  type: 'technical' | 'organizational' | 'financial' | 'regulatory' | 'cultural';
  description: string;
  impact: 'low' | 'medium' | 'high';
  mitigation: string[];
  timeline: string;
  cost: number;
}

export interface ComplianceTrend {
  metric: string;
  period: string;
  values: TrendDataPoint[];
  direction: 'improving' | 'stable' | 'declining';
  rate: number;
  forecast: TrendForecast;
  factors: TrendFactor[];
}

export interface TrendDataPoint {
  timestamp: Date;
  value: number;
  context: Record<string, any>;
  annotations: string[];
}

export interface TrendForecast {
  values: ForecastDataPoint[];
  confidence: number;
  methodology: string;
  assumptions: string[];
  scenarios: ForecastScenario[];
}

export interface ForecastDataPoint {
  timestamp: Date;
  value: number;
  confidence: number;
  range: NumberRange;
}

export interface NumberRange {
  min: number;
  max: number;
  mean: number;
  median: number;
  std: number;
}

export interface TrendFactor {
  name: string;
  correlation: number;
  impact: 'positive' | 'negative' | 'neutral';
  significance: number;
  controllable: boolean;
}

export interface ComplianceBenchmark {
  category: string;
  metric: string;
  value: number;
  percentile: number;
  industry: string;
  region: string;
  source: string;
  date: Date;
  sample: BenchmarkSample;
  comparison: BenchmarkComparison;
}

export interface BenchmarkSample {
  size: number;
  demographics: Record<string, any>;
  criteria: string[];
  methodology: string;
  confidence: number;
}

export interface BenchmarkComparison {
  position: 'below' | 'at' | 'above';
  percentile: number;
  gap: number;
  significance: 'not_significant' | 'significant' | 'highly_significant';
  actions: string[];
}

export interface ComplianceMaturity {
  model: string;
  version: string;
  assessment: MaturityAssessment;
  levels: MaturityLevel[];
  progression: MaturityProgression[];
  roadmap: MaturityRoadmap;
}

export interface MaturityAssessment {
  date: Date;
  assessor: string;
  methodology: string;
  scope: string[];
  criteria: string[];
  evidence: string[];
  results: MaturityResult[];
}

export interface MaturityResult {
  dimension: string;
  level: number;
  score: number;
  evidence: string[];
  gaps: string[];
  recommendations: string[];
  timeline: string;
}

export interface MaturityLevel {
  level: number;
  name: string;
  description: string;
  characteristics: string[];
  requirements: string[];
  benefits: string[];
  typical: TypicalOrganization;
}

export interface TypicalOrganization {
  size: string;
  industry: string[];
  maturity: string;
  investment: number;
  timeline: string;
  challenges: string[];
}

export interface MaturityRoadmap {
  currentLevel: number;
  targetLevel: number;
  timeline: string;
  phases: RoadmapPhase[];
  investments: RoadmapInvestment[];
  risks: RoadmapRisk[];
  success: SuccessFactors;
}

export interface RoadmapPhase {
  phase: number;
  name: string;
  description: string;
  duration: string;
  level: number;
  objectives: string[];
  deliverables: string[];
  milestones: string[];
  dependencies: string[];
  resources: string[];
  risks: string[];
}

export interface RoadmapInvestment {
  category: string;
  amount: number;
  timing: string;
  roi: number;
  payback: number;
  benefits: string[];
  alternatives: string[];
}

export interface RoadmapRisk {
  risk: string;
  probability: number;
  impact: 'low' | 'medium' | 'high';
  mitigation: string[];
  contingency: string[];
  monitoring: string[];
}

export interface SuccessFactors {
  critical: string[];
  important: string[];
  helpful: string[];
  measurement: string[];
  monitoring: string[];
  governance: string[];
}

export interface CompliancePerformance {
  efficiency: PerformanceEfficiency;
  effectiveness: PerformanceEffectiveness;
  quality: PerformanceQuality;
  timeliness: PerformanceTimeliness;
  cost: PerformanceCost;
  satisfaction: PerformanceSatisfaction;
}

export interface PerformanceEfficiency {
  automation: number;
  productivity: number;
  throughput: number;
  utilization: number;
  waste: number;
  optimization: number;
}

export interface PerformanceEffectiveness {
  goals: number;
  outcomes: number;
  impact: number;
  value: number;
  sustainability: number;
  scalability: number;
}

export interface PerformanceQuality {
  accuracy: number;
  completeness: number;
  consistency: number;
  reliability: number;
  validity: number;
  excellence: number;
}

export interface PerformanceTimeliness {
  schedule: number;
  responsiveness: number;
  agility: number;
  predictability: number;
  recovery: number;
  improvement: number;
}

export interface PerformanceCost {
  efficiency: number;
  optimization: number;
  control: number;
  predictability: number;
  transparency: number;
  value: number;
}

export interface PerformanceSatisfaction {
  stakeholders: number;
  auditors: number;
  management: number;
  staff: number;
  customers: number;
  regulators: number;
}

// Additional metadata interfaces
export interface AuditMetadata {
  classification: string;
  sensitivity: string;
  retention: number;
  jurisdiction: string;
  standards: string[];
  templates: string[];
  tools: string[];
  automation: number;
  integration: IntegrationMetadata[];
  customFields: Record<string, any>;
}

export interface IntegrationMetadata {
  system: string;
  type: 'import' | 'export' | 'sync' | 'api';
  frequency: string;
  format: string;
  mapping: string[];
  validation: string[];
  error: string[];
}

export interface AssessmentMetadata {
  methodology: string;
  tools: string[];
  automation: number;
  duration: number;
  effort: number;
  complexity: 'low' | 'medium' | 'high';
  confidence: number;
  limitations: string[];
  assumptions: string[];
  dependencies: string[];
}

export interface FindingMetadata {
  discovery: DiscoveryMetadata;
  analysis: AnalysisMetadata;
  validation: ValidationMetadata;
  impact: ImpactMetadata;
  remediation: RemediationMetadata;
  tracking: TrackingMetadata;
}

export interface DiscoveryMetadata {
  method: string[];
  source: string[];
  tools: string[];
  automation: boolean;
  confidence: number;
  verification: string[];
}

export interface AnalysisMetadata {
  methodology: string[];
  depth: 'surface' | 'detailed' | 'comprehensive';
  duration: number;
  effort: number;
  expertise: string[];
  tools: string[];
}

export interface ValidationMetadata {
  methods: string[];
  evidence: string[];
  sources: string[];
  independence: boolean;
  confirmation: string[];
  confidence: number;
}

export interface ImpactMetadata {
  assessment: string[];
  quantification: boolean;
  monetization: boolean;
  stakeholders: string[];
  timeframes: string[];
  scenarios: string[];
}

export interface RemediationMetadata {
  options: number;
  complexity: 'low' | 'medium' | 'high';
  urgency: 'low' | 'medium' | 'high' | 'critical';
  resources: string[];
  dependencies: string[];
  risks: string[];
}

export interface TrackingMetadata {
  workflow: string;
  notifications: string[];
  escalations: string[];
  reporting: string[];
  integration: string[];
  automation: boolean;
}

export interface RecommendationMetadata {
  origin: string;
  rationale: string[];
  alternatives: number;
  complexity: 'low' | 'medium' | 'high';
  urgency: 'low' | 'medium' | 'high' | 'critical';
  confidence: number;
  validation: string[];
  approval: ApprovalMetadata;
  tracking: TrackingMetadata;
}

export interface ApprovalMetadata {
  required: boolean;
  levels: string[];
  criteria: string[];
  process: string;
  timeline: string;
  automation: boolean;
}

// External reference interface
export interface ExternalReference {
  type: 'standard' | 'regulation' | 'guidance' | 'best_practice' | 'research' | 'tool' | 'template';
  id: string;
  title: string;
  organization: string;
  version?: string;
  url?: string;
  description: string;
  relevance: number;
  applicability: string[];
  lastUpdated?: Date;
  accessed?: Date;
}

// Timeline and history interfaces
export interface AuditTimelineEntry {
  timestamp: Date;
  event: string;
  description: string;
  actor: string;
  details: Record<string, any>;
  category: 'planning' | 'execution' | 'reporting' | 'follow_up' | 'administrative';
  milestone: boolean;
  automated: boolean;
}

// Configuration interfaces
export interface AuditConfiguration {
  scope: ScopeConfiguration;
  methodology: MethodologyConfiguration;
  testing: TestingConfiguration;
  reporting: ReportingConfiguration;
  quality: QualityConfiguration;
  automation: AutomationConfiguration;
}

export interface ScopeConfiguration {
  inclusion: string[];
  exclusion: string[];
  boundaries: string[];
  criteria: string[];
  justification: string[];
  approval: string[];
}

export interface MethodologyConfiguration {
  framework: string;
  approach: string[];
  phases: string[];
  activities: string[];
  deliverables: string[];
  quality: string[];
}

export interface TestingConfiguration {
  strategy: string;
  coverage: number;
  sampling: string[];
  procedures: string[];
  evidence: string[];
  documentation: string[];
}

export interface ReportingConfiguration {
  audiences: string[];
  formats: string[];
  frequency: string[];
  distribution: string[];
  retention: string[];
  security: string[];
}

export interface QualityConfiguration {
  standards: string[];
  reviews: string[];
  approvals: string[];
  controls: string[];
  metrics: string[];
  improvement: string[];
}

export interface AutomationConfiguration {
  tools: string[];
  integration: string[];
  workflows: string[];
  notifications: string[];
  reporting: string[];
  analysis: string[];
}

// Report interfaces
export interface AuditReport {
  id: string;
  type: 'interim' | 'final' | 'summary' | 'detailed' | 'management_letter' | 'certification';
  title: string;
  description: string;
  audience: string[];
  classification: string;
  version: string;
  status: 'draft' | 'review' | 'approved' | 'published' | 'archived';
  sections: ReportSection[];
  appendices: ReportAppendix[];
  metadata: ReportMetadata;
  distribution: ReportDistribution;
  feedback: ReportFeedback[];
  createdAt: Date;
  publishedAt?: Date;
  author: string;
  reviewer?: string;
  approver?: string;
}

export interface ReportSection {
  id: string;
  title: string;
  order: number;
  content: SectionContent[];
  subsections: ReportSubsection[];
  mandatory: boolean;
  audience: string[];
  classification: string;
}

export interface ReportSubsection {
  id: string;
  title: string;
  order: number;
  content: SectionContent[];
  mandatory: boolean;
  audience: string[];
}

export interface ReportAppendix {
  id: string;
  title: string;
  type: 'evidence' | 'methodology' | 'data' | 'reference' | 'glossary' | 'acronyms';
  content: string;
  format: string;
  size: number;
  mandatory: boolean;
  audience: string[];
  classification: string;
}

export interface ReportMetadata {
  template: string;
  generator: string;
  automation: boolean;
  language: string;
  format: string;
  pages: number;
  words: number;
  figures: number;
  tables: number;
  references: number;
  appendices: number;
}

export interface ReportDistribution {
  method: string[];
  recipients: ReportRecipient[];
  schedule: string;
  security: string[];
  tracking: boolean;
  confirmation: boolean;
}

export interface ReportRecipient {
  name: string;
  role: string;
  organization: string;
  contact: string;
  format: string;
  classification: string;
  acknowledgment: boolean;
  feedback: boolean;
}

export interface ReportFeedback {
  id: string;
  recipient: string;
  timestamp: Date;
  type: 'acknowledgment' | 'question' | 'clarification' | 'disagreement' | 'suggestion';
  content: string;
  response?: string;
  resolved: boolean;
  category: string;
  priority: 'low' | 'medium' | 'high';
}

// Service interface
export interface IComplianceAuditService {
  // Audit management
  createAudit(audit: Omit<ComplianceAudit, 'id' | 'createdAt' | 'updatedAt'>): Promise<ComplianceAudit>;
  updateAudit(auditId: string, updates: Partial<ComplianceAudit>): Promise<ComplianceAudit>;
  getAudit(auditId: string): Promise<ComplianceAudit | null>;
  listAudits(filters?: AuditFilters): Promise<ComplianceAudit[]>;
  deleteAudit(auditId: string): Promise<void>;

  // Assessment management
  createAssessment(assessment: Omit<ComplianceAssessment, 'id'>): Promise<ComplianceAssessment>;
  updateAssessment(assessmentId: string, updates: Partial<ComplianceAssessment>): Promise<ComplianceAssessment>;
  getAssessment(assessmentId: string): Promise<ComplianceAssessment | null>;
  listAssessments(auditId: string): Promise<ComplianceAssessment[]>;

  // Finding management
  createFinding(finding: Omit<ComplianceFinding, 'id' | 'createdAt' | 'updatedAt'>): Promise<ComplianceFinding>;
  updateFinding(findingId: string, updates: Partial<ComplianceFinding>): Promise<ComplianceFinding>;
  getFinding(findingId: string): Promise<ComplianceFinding | null>;
  listFindings(filters?: FindingFilters): Promise<ComplianceFinding[]>;

  // Recommendation management
  createRecommendation(recommendation: Omit<ComplianceRecommendation, 'id'>): Promise<ComplianceRecommendation>;
  updateRecommendation(recommendationId: string, updates: Partial<ComplianceRecommendation>): Promise<ComplianceRecommendation>;
  getRecommendation(recommendationId: string): Promise<ComplianceRecommendation | null>;
  listRecommendations(filters?: RecommendationFilters): Promise<ComplianceRecommendation[]>;

  // Evidence management
  addEvidence(evidence: Omit<ComplianceEvidence, 'id' | 'createdAt' | 'updatedAt'>): Promise<ComplianceEvidence>;
  getEvidence(evidenceId: string): Promise<ComplianceEvidence | null>;
  listEvidence(filters?: EvidenceFilters): Promise<ComplianceEvidence[]>;
  validateEvidence(evidenceId: string): Promise<EvidenceQualityAssessment>;

  // Reporting
  generateReport(auditId: string, type: AuditReport['type'], options?: ReportOptions): Promise<AuditReport>;
  getReport(reportId: string): Promise<AuditReport | null>;
  listReports(auditId: string): Promise<AuditReport[]>;

  // Metrics and analytics
  getComplianceMetrics(filters?: MetricFilters): Promise<ComplianceMetrics>;
  getMaturityAssessment(framework: ComplianceFramework): Promise<ComplianceMaturity>;
  getBenchmarks(category: string, filters?: BenchmarkFilters): Promise<ComplianceBenchmark[]>;

  // Framework-specific methods
  assessOWASP(scope: string[], options?: OWASPAssessmentOptions): Promise<OWASPAssessment[]>;
  assessISO27001(scope: string[], options?: ISO27001AssessmentOptions): Promise<ISO27001Assessment[]>;
  assessSOC2(principles: SOC2TrustServicesPrinciple[], options?: SOC2AssessmentOptions): Promise<SOC2Assessment[]>;
}

export interface AuditFilters {
  framework?: ComplianceFramework[];
  status?: AuditStatus[];
  auditor?: string[];
  dateRange?: DateRange;
  tags?: string[];
}

export interface FindingFilters {
  auditId?: string[];
  status?: ComplianceFinding['status'][];
  severity?: RiskLevel[];
  type?: ComplianceFinding['type'][];
  assignedTo?: string[];
  dateRange?: DateRange;
}

export interface RecommendationFilters {
  findingId?: string[];
  status?: ComplianceRecommendation['status'][];
  priority?: RemediationPriority[];
  category?: ComplianceRecommendation['category'][];
  assignedTo?: string[];
  dateRange?: DateRange;
}

export interface EvidenceFilters {
  auditId?: string[];
  type?: EvidenceType[];
  source?: string[];
  quality?: QualityScore['score'][];
  dateRange?: DateRange;
}

export interface ReportOptions {
  template?: string;
  sections?: string[];
  audience?: string[];
  format?: string;
  classification?: string;
  customization?: Record<string, any>;
}

export interface MetricFilters {
  framework?: ComplianceFramework[];
  dateRange?: DateRange;
  scope?: string[];
  granularity?: 'daily' | 'weekly' | 'monthly' | 'quarterly' | 'yearly';
}

export interface BenchmarkFilters {
  industry?: string[];
  region?: string[];
  size?: string[];
  dateRange?: DateRange;
}

export interface DateRange {
  start: Date;
  end: Date;
}

export interface OWASPAssessmentOptions {
  methodology?: OWASPTestingMethodology;
  depth?: 'basic' | 'standard' | 'comprehensive';
  automation?: boolean;
  tools?: string[];
}

export interface ISO27001AssessmentOptions {
  scope?: string[];
  depth?: 'basic' | 'standard' | 'comprehensive';
  maturityModel?: string;
  baseline?: string;
}

export interface SOC2AssessmentOptions {
  type?: 'type1' | 'type2';
  period?: DateRange;
  testing?: 'design' | 'operating' | 'both';
  sampling?: string;
}