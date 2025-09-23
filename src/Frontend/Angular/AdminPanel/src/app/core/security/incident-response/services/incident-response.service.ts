import { Injectable, signal, computed, effect } from '@angular/core';
import { Subject, interval } from 'rxjs';
import { map, filter, debounceTime } from 'rxjs/operators';
import {
  IIncidentResponseService,
  SecurityIncident,
  IncidentStatus,
  IncidentSeverity,
  IncidentType,
  ResponsePlaybook,
  PlaybookExecution,
  PlaybookExecutionStatus,
  AutomationRule,
  AutomationTestResult,
  ResponseAction,
  ResponseActionRecord,
  ForensicInvestigation,
  InvestigationScope,
  ForensicFinding,
  Evidence,
  InvestigationReport,
  WarRoom,
  IncidentCommunication,
  RecoveryExecution,
  RecoveryStatus,
  ValidationResult,
  ThreatHunt,
  HuntQuery,
  HuntFinding,
  HuntReport,
  IncidentMetrics,
  IncidentReport,
  PostMortemReport,
  IncidentFilter,
  TimeRange,
  EscalationLevel,
  PlaybookTrigger,
  IncidentResponseConfig,
  PlaybookStep,
  AutomationAction,
  IncidentImpact
} from '../interfaces/incident-response.interface';

interface IncidentResponseState {
  incidents: Map<string, SecurityIncident>;
  playbooks: Map<string, ResponsePlaybook>;
  automationRules: Map<string, AutomationRule>;
  investigations: Map<string, ForensicInvestigation>;
  warRooms: Map<string, WarRoom>;
  executions: Map<string, PlaybookExecution>;
  hunts: Map<string, ThreatHunt>;
  metrics: IncidentMetrics | null;
  config: IncidentResponseConfig;
  lastUpdated: Date;
}

interface ActionQueue {
  incidentId: string;
  action: ResponseAction;
  parameters?: any;
  priority: number;
  scheduledAt?: Date;
  retryCount: number;
}

interface AlertCorrelation {
  alerts: any[];
  pattern?: string;
  confidence: number;
  suggestedType?: IncidentType;
  suggestedSeverity?: IncidentSeverity;
}

@Injectable({
  providedIn: 'root'
})
export class IncidentResponseService implements IIncidentResponseService {
  private readonly state = signal<IncidentResponseState>({
    incidents: new Map(),
    playbooks: new Map(),
    automationRules: new Map(),
    investigations: new Map(),
    warRooms: new Map(),
    executions: new Map(),
    hunts: new Map(),
    metrics: null,
    config: this.getDefaultConfig(),
    lastUpdated: new Date()
  });

  // Computed signals
  readonly activeIncidents = computed(() =>
    Array.from(this.state().incidents.values()).filter(i =>
      ![IncidentStatus.CLOSED, IncidentStatus.FALSE_POSITIVE].includes(i.status)
    )
  );

  readonly criticalIncidents = computed(() =>
    this.activeIncidents().filter(i => i.severity === IncidentSeverity.CRITICAL)
  );

  readonly incidentsByStatus = computed(() => {
    const incidents = Array.from(this.state().incidents.values());
    return Object.values(IncidentStatus).reduce((acc, status) => {
      acc[status] = incidents.filter(i => i.status === status);
      return acc;
    }, {} as Record<IncidentStatus, SecurityIncident[]>);
  });

  readonly averageMTTR = computed(() => {
    const closedIncidents = Array.from(this.state().incidents.values())
      .filter(i => i.status === IncidentStatus.CLOSED && i.closedAt);

    if (closedIncidents.length === 0) return 0;

    const totalTime = closedIncidents.reduce((sum, i) => {
      const responseTime = i.closedAt!.getTime() - i.createdAt.getTime();
      return sum + responseTime;
    }, 0);

    return totalTime / closedIncidents.length;
  });

  // Event streams
  private readonly incidentCreated$ = new Subject<SecurityIncident>();
  private readonly incidentUpdated$ = new Subject<SecurityIncident>();
  private readonly actionExecuted$ = new Subject<ResponseActionRecord>();
  private readonly playbookExecuted$ = new Subject<PlaybookExecution>();
  private readonly alertReceived$ = new Subject<any>();

  // Processing queues
  private readonly actionQueue: ActionQueue[] = [];
  private readonly correlationQueue: AlertCorrelation[] = [];
  private readonly escalationTimers = new Map<string, NodeJS.Timeout>();

  constructor() {
    this.initializeEffects();
    this.startAutomationEngine();
    this.startMetricsCollection();
  }

  private initializeEffects(): void {
    // Auto-escalation effect
    effect(() => {
      const config = this.state().config;
      if (config.autoEscalation) {
        this.activeIncidents().forEach(incident => {
          this.checkEscalation(incident);
        });
      }
    });

    // Alert correlation
    this.alertReceived$.pipe(
      debounceTime(1000),
      map(alert => this.correlateAlert(alert)),
      filter(correlation => correlation.confidence > 0.7)
    ).subscribe(correlation => {
      this.handleCorrelation(correlation);
    });

    // Automation trigger monitoring
    this.incidentCreated$.subscribe(incident => {
      this.triggerAutomation(incident);
    });
  }

  private startAutomationEngine(): void {
    interval(5000).subscribe(() => {
      this.processActionQueue();
      this.checkAutomationTriggers();
      this.updateMetrics();
    });
  }

  private startMetricsCollection(): void {
    interval(60000).subscribe(() => {
      this.calculateMetrics();
    });
  }

  // Incident Management
  async createIncident(incidentData: Omit<SecurityIncident, 'id' | 'createdAt' | 'updatedAt'>): Promise<SecurityIncident> {
    const incident: SecurityIncident = {
      ...incidentData,
      id: this.generateId('INC'),
      createdAt: new Date(),
      updatedAt: new Date(),
      timeline: [{
        timestamp: new Date(),
        event: 'Incident created',
        actor: 'System',
        action: 'create'
      }],
      responseActions: []
    };

    this.updateState(state => {
      state.incidents.set(incident.id, incident);
      return state;
    });

    this.incidentCreated$.next(incident);

    // Auto-assign based on rules
    if (this.state().config.defaultAssignment.length > 0) {
      await this.autoAssign(incident);
    }

    // Check for playbook triggers
    await this.checkPlaybookTriggers(incident);

    // Set up SLA monitoring
    this.setupSLAMonitoring(incident);

    return incident;
  }

  async updateIncident(id: string, updates: Partial<SecurityIncident>): Promise<SecurityIncident> {
    const incident = this.state().incidents.get(id);
    if (!incident) throw new Error(`Incident ${id} not found`);

    const updatedIncident: SecurityIncident = {
      ...incident,
      ...updates,
      updatedAt: new Date(),
      timeline: [
        ...incident.timeline,
        {
          timestamp: new Date(),
          event: `Incident updated: ${Object.keys(updates).join(', ')}`,
          actor: 'User',
          action: 'update',
          details: JSON.stringify(updates)
        }
      ]
    };

    this.updateState(state => {
      state.incidents.set(id, updatedIncident);
      return state;
    });

    this.incidentUpdated$.next(updatedIncident);
    return updatedIncident;
  }

  async getIncident(id: string): Promise<SecurityIncident> {
    const incident = this.state().incidents.get(id);
    if (!incident) throw new Error(`Incident ${id} not found`);
    return incident;
  }

  async listIncidents(filter?: IncidentFilter): Promise<SecurityIncident[]> {
    let incidents = Array.from(this.state().incidents.values());

    if (filter) {
      if (filter.status) {
        incidents = incidents.filter(i => filter.status!.includes(i.status));
      }
      if (filter.severity) {
        incidents = incidents.filter(i => filter.severity!.includes(i.severity));
      }
      if (filter.type) {
        incidents = incidents.filter(i => filter.type!.includes(i.type));
      }
      if (filter.assignedTo) {
        incidents = incidents.filter(i => i.assignedTo === filter.assignedTo);
      }
      if (filter.dateRange) {
        incidents = incidents.filter(i =>
          i.createdAt >= filter.dateRange!.start &&
          i.createdAt <= filter.dateRange!.end
        );
      }
      if (filter.tags) {
        incidents = incidents.filter(i =>
          filter.tags!.some(tag => i.tags.includes(tag))
        );
      }
    }

    return incidents.sort((a, b) => b.createdAt.getTime() - a.createdAt.getTime());
  }

  async assignIncident(id: string, assignee: string): Promise<void> {
    await this.updateIncident(id, {
      assignedTo: assignee,
      timeline: [
        ...this.state().incidents.get(id)!.timeline,
        {
          timestamp: new Date(),
          event: `Assigned to ${assignee}`,
          actor: 'System',
          action: 'assign'
        }
      ]
    });

    // Notify assignee
    await this.sendNotification({
      id: this.generateId('NOTIF'),
      incidentId: id,
      type: 'internal',
      channel: 'email',
      recipients: [assignee],
      subject: `Incident ${id} assigned to you`,
      content: `You have been assigned to incident ${id}`,
      sentAt: new Date(),
      sentBy: 'System',
      status: 'sent'
    });
  }

  async escalateIncident(id: string, level: EscalationLevel): Promise<void> {
    const incident = await this.getIncident(id);

    await this.updateIncident(id, {
      escalationLevel: level,
      timeline: [
        ...incident.timeline,
        {
          timestamp: new Date(),
          event: `Escalated to ${level}`,
          actor: 'System',
          action: 'escalate'
        }
      ]
    });

    // Trigger escalation notifications
    await this.triggerEscalationNotifications(incident, level);
  }

  async closeIncident(id: string, resolution: string): Promise<void> {
    await this.updateIncident(id, {
      status: IncidentStatus.CLOSED,
      closedAt: new Date(),
      rootCause: resolution,
      timeline: [
        ...this.state().incidents.get(id)!.timeline,
        {
          timestamp: new Date(),
          event: 'Incident closed',
          actor: 'User',
          action: 'close',
          details: resolution
        }
      ]
    });

    // Clear escalation timers
    const timer = this.escalationTimers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.escalationTimers.delete(id);
    }
  }

  // Playbook Management
  async createPlaybook(playbookData: Omit<ResponsePlaybook, 'id' | 'createdAt' | 'updatedAt'>): Promise<ResponsePlaybook> {
    const playbook: ResponsePlaybook = {
      ...playbookData,
      id: this.generateId('PLB'),
      createdAt: new Date(),
      updatedAt: new Date(),
      executionCount: 0,
      successRate: 100
    };

    this.updateState(state => {
      state.playbooks.set(playbook.id, playbook);
      return state;
    });

    return playbook;
  }

  async updatePlaybook(id: string, updates: Partial<ResponsePlaybook>): Promise<ResponsePlaybook> {
    const playbook = this.state().playbooks.get(id);
    if (!playbook) throw new Error(`Playbook ${id} not found`);

    const updatedPlaybook: ResponsePlaybook = {
      ...playbook,
      ...updates,
      updatedAt: new Date()
    };

    this.updateState(state => {
      state.playbooks.set(id, updatedPlaybook);
      return state;
    });

    return updatedPlaybook;
  }

  async executePlaybook(playbookId: string, incidentId: string, variables?: Record<string, any>): Promise<PlaybookExecution> {
    const playbook = this.state().playbooks.get(playbookId);
    if (!playbook) throw new Error(`Playbook ${playbookId} not found`);

    const execution: PlaybookExecution = {
      id: this.generateId('EXEC'),
      playbookId,
      incidentId,
      status: 'running',
      currentStep: playbook.steps[0]?.id,
      completedSteps: [],
      variables: variables || {},
      startedAt: new Date(),
      logs: []
    };

    this.updateState(state => {
      state.executions.set(execution.id, execution);
      return state;
    });

    // Execute playbook steps
    this.executePlaybookSteps(execution, playbook);

    this.playbookExecuted$.next(execution);
    return execution;
  }

  private async executePlaybookSteps(execution: PlaybookExecution, playbook: ResponsePlaybook): Promise<void> {
    for (const step of playbook.steps) {
      if (execution.status !== 'running') break;

      // Check conditions
      if (step.conditions && !this.evaluateConditions(step.conditions, execution.variables)) {
        this.logExecutionStep(execution.id, step.id, 'skipped', 'Conditions not met');
        continue;
      }

      // Check for manual approval
      if (step.approvalRequired) {
        await this.requestApproval(execution.id, step);
      }

      try {
        // Execute action
        const result = await this.executeStepAction(step, execution.incidentId, execution.variables);

        this.logExecutionStep(execution.id, step.id, 'completed', result);
        execution.completedSteps.push(step.id);

        // Handle success actions
        if (step.onSuccess) {
          for (const successAction of step.onSuccess) {
            await this.executeAction(execution.incidentId, successAction as ResponseAction);
          }
        }
      } catch (error: any) {
        this.logExecutionStep(execution.id, step.id, 'failed', error.message);

        // Handle failure actions
        if (step.onFailure) {
          for (const failureAction of step.onFailure) {
            await this.executeAction(execution.incidentId, failureAction as ResponseAction);
          }
        }

        // Retry if configured
        if (step.retryCount && step.retryCount > 0) {
          await this.retryStep(execution, step);
        }
      }
    }

    // Update execution status
    this.updateState(state => {
      const exec = state.executions.get(execution.id);
      if (exec) {
        exec.status = 'completed';
        exec.completedAt = new Date();
      }
      return state;
    });
  }

  async getPlaybookStatus(executionId: string): Promise<PlaybookExecutionStatus> {
    const execution = this.state().executions.get(executionId);
    if (!execution) throw new Error(`Execution ${executionId} not found`);

    const playbook = this.state().playbooks.get(execution.playbookId);
    const progress = playbook ?
      (execution.completedSteps.length / playbook.steps.length) * 100 : 0;

    return {
      executionId,
      status: execution.status,
      progress,
      currentStep: execution.currentStep,
      errors: execution.logs.filter(l => l.status === 'failed').map(l => l.error || 'Unknown error'),
      warnings: execution.logs.filter(l => l.details?.includes('warning')).map(l => l.details!)
    };
  }

  async stopPlaybook(executionId: string): Promise<void> {
    this.updateState(state => {
      const execution = state.executions.get(executionId);
      if (execution) {
        execution.status = 'cancelled';
        execution.completedAt = new Date();
      }
      return state;
    });
  }

  // Automation
  async createAutomationRule(ruleData: Omit<AutomationRule, 'id' | 'createdAt' | 'updatedAt'>): Promise<AutomationRule> {
    const rule: AutomationRule = {
      ...ruleData,
      id: this.generateId('AUTO'),
      createdAt: new Date(),
      updatedAt: new Date(),
      triggerCount: 0
    };

    this.updateState(state => {
      state.automationRules.set(rule.id, rule);
      return state;
    });

    return rule;
  }

  async updateAutomationRule(id: string, updates: Partial<AutomationRule>): Promise<AutomationRule> {
    const rule = this.state().automationRules.get(id);
    if (!rule) throw new Error(`Automation rule ${id} not found`);

    const updatedRule: AutomationRule = {
      ...rule,
      ...updates,
      updatedAt: new Date()
    };

    this.updateState(state => {
      state.automationRules.set(id, updatedRule);
      return state;
    });

    return updatedRule;
  }

  async enableAutomation(id: string): Promise<void> {
    await this.updateAutomationRule(id, { enabled: true });
  }

  async disableAutomation(id: string): Promise<void> {
    await this.updateAutomationRule(id, { enabled: false });
  }

  async testAutomation(id: string, testData?: any): Promise<AutomationTestResult> {
    const rule = this.state().automationRules.get(id);
    if (!rule) throw new Error(`Automation rule ${id} not found`);

    const startTime = Date.now();
    const executedActions: string[] = [];
    const results: Record<string, any> = {};
    const errors: string[] = [];
    const warnings: string[] = [];

    for (const action of rule.actions) {
      try {
        const result = await this.simulateAction(action, testData);
        executedActions.push(action.type);
        results[action.type] = result;
      } catch (error: any) {
        errors.push(`Action ${action.type} failed: ${error.message}`);
      }
    }

    return {
      success: errors.length === 0,
      executedActions,
      results,
      errors,
      warnings,
      duration: Date.now() - startTime
    };
  }

  // Response Actions
  async executeAction(incidentId: string, action: ResponseAction, parameters?: any): Promise<ResponseActionRecord> {
    const record: ResponseActionRecord = {
      id: this.generateId('ACT'),
      action,
      status: 'in_progress',
      automatedExecution: true,
      executedAt: new Date()
    };

    try {
      const result = await this.performAction(action, parameters, incidentId);
      record.status = 'completed';
      record.result = result;
    } catch (error: any) {
      record.status = 'failed';
      record.error = error.message;
    }

    // Update incident with action record
    const incident = await this.getIncident(incidentId);
    await this.updateIncident(incidentId, {
      responseActions: [...incident.responseActions, record]
    });

    this.actionExecuted$.next(record);
    return record;
  }

  async batchExecuteActions(incidentId: string, actions: ResponseAction[]): Promise<ResponseActionRecord[]> {
    const records: ResponseActionRecord[] = [];

    for (const action of actions) {
      const record = await this.executeAction(incidentId, action);
      records.push(record);
    }

    return records;
  }

  async rollbackAction(actionId: string): Promise<void> {
    // Find the action record
    const incidents = Array.from(this.state().incidents.values());
    const incident = incidents.find(i =>
      i.responseActions.some(a => a.id === actionId)
    );

    if (!incident) throw new Error(`Action ${actionId} not found`);

    const action = incident.responseActions.find(a => a.id === actionId);
    if (!action || !action.rollbackAvailable) {
      throw new Error(`Action ${actionId} cannot be rolled back`);
    }

    // Perform rollback
    await this.performRollback(action, incident.id);
  }

  // Investigation
  async startInvestigation(incidentId: string, scope: InvestigationScope): Promise<ForensicInvestigation> {
    const investigation: ForensicInvestigation = {
      id: this.generateId('INV'),
      incidentId,
      investigator: 'Current User',
      status: 'in_progress',
      scope,
      findings: [],
      evidence: [],
      timeline: [],
      recommendations: [],
      createdAt: new Date()
    };

    this.updateState(state => {
      state.investigations.set(investigation.id, investigation);
      return state;
    });

    return investigation;
  }

  async updateInvestigation(id: string, findings: ForensicFinding[]): Promise<void> {
    this.updateState(state => {
      const investigation = state.investigations.get(id);
      if (investigation) {
        investigation.findings.push(...findings);
      }
      return state;
    });
  }

  async collectEvidence(investigationId: string, evidence: Evidence): Promise<void> {
    this.updateState(state => {
      const investigation = state.investigations.get(investigationId);
      if (investigation) {
        investigation.evidence.push(evidence);
      }
      return state;
    });
  }

  async generateInvestigationReport(investigationId: string): Promise<InvestigationReport> {
    const investigation = this.state().investigations.get(investigationId);
    if (!investigation) throw new Error(`Investigation ${investigationId} not found`);

    const report: InvestigationReport = {
      id: this.generateId('REPORT'),
      executive_summary: this.generateExecutiveSummary(investigation),
      technical_details: this.generateTechnicalDetails(investigation),
      timeline_analysis: this.generateTimelineAnalysis(investigation),
      root_cause_analysis: this.generateRootCauseAnalysis(investigation),
      impact_assessment: this.generateImpactAssessment(investigation),
      recommendations: investigation.recommendations,
      lessons_learned: this.extractLessonsLearned(investigation),
      generatedAt: new Date()
    };

    this.updateState(state => {
      const inv = state.investigations.get(investigationId);
      if (inv) {
        inv.report = report;
        inv.status = 'completed';
        inv.completedAt = new Date();
      }
      return state;
    });

    return report;
  }

  // Communication
  async createWarRoom(incidentId: string): Promise<WarRoom> {
    const warRoom: WarRoom = {
      id: this.generateId('WAR'),
      incidentId,
      name: `War Room - ${incidentId}`,
      participants: [],
      status: 'active',
      decisions: [],
      actionItems: [],
      createdAt: new Date()
    };

    this.updateState(state => {
      state.warRooms.set(warRoom.id, warRoom);
      return state;
    });

    return warRoom;
  }

  async joinWarRoom(warRoomId: string, participant: string): Promise<void> {
    this.updateState(state => {
      const warRoom = state.warRooms.get(warRoomId);
      if (warRoom) {
        warRoom.participants.push({
          userId: participant,
          name: participant,
          role: 'Participant',
          joinedAt: new Date(),
          status: 'active'
        });
      }
      return state;
    });
  }

  async sendNotification(notification: IncidentCommunication): Promise<void> {
    // Implementation for sending notifications
    console.log('Sending notification:', notification);
  }

  async broadcastUpdate(incidentId: string, update: string): Promise<void> {
    const incident = await this.getIncident(incidentId);
    const warRoom = Array.from(this.state().warRooms.values())
      .find(w => w.incidentId === incidentId);

    if (warRoom) {
      const notification: IncidentCommunication = {
        id: this.generateId('NOTIF'),
        incidentId,
        type: 'internal',
        channel: 'broadcast',
        recipients: warRoom.participants.map(p => p.userId),
        subject: `Update on incident ${incidentId}`,
        content: update,
        sentAt: new Date(),
        sentBy: 'System',
        status: 'sent'
      };

      await this.sendNotification(notification);
    }
  }

  // Recovery
  async initiateRecovery(incidentId: string, planId: string): Promise<RecoveryExecution> {
    const execution: RecoveryExecution = {
      id: this.generateId('REC'),
      planId,
      incidentId,
      status: 'in_progress',
      currentStep: 0,
      completedSteps: [],
      startedAt: new Date()
    };

    // Implementation for recovery initiation
    return execution;
  }

  async updateRecoveryStatus(executionId: string, status: RecoveryStatus): Promise<void> {
    // Implementation for updating recovery status
  }

  async validateRecovery(executionId: string): Promise<ValidationResult> {
    // Implementation for recovery validation
    return {
      success: true,
      validatedSystems: [],
      failedValidations: []
    };
  }

  // Threat Hunting
  async createHunt(huntData: Omit<ThreatHunt, 'id' | 'startDate'>): Promise<ThreatHunt> {
    const hunt: ThreatHunt = {
      ...huntData,
      id: this.generateId('HUNT'),
      startDate: new Date(),
      findings: [],
      queries: []
    };

    this.updateState(state => {
      state.hunts.set(hunt.id, hunt);
      return state;
    });

    return hunt;
  }

  async executeHuntQuery(huntId: string, query: HuntQuery): Promise<any[]> {
    // Implementation for executing hunt queries
    return [];
  }

  async reportHuntFinding(huntId: string, finding: HuntFinding): Promise<void> {
    this.updateState(state => {
      const hunt = state.hunts.get(huntId);
      if (hunt) {
        hunt.findings.push(finding);
      }
      return state;
    });
  }

  async completeHunt(huntId: string, report: HuntReport): Promise<void> {
    this.updateState(state => {
      const hunt = state.hunts.get(huntId);
      if (hunt) {
        hunt.status = 'completed';
        hunt.endDate = new Date();
        hunt.report = report;
      }
      return state;
    });
  }

  // Analytics and Reporting
  async getIncidentMetrics(timeRange?: TimeRange): Promise<IncidentMetrics> {
    const incidents = Array.from(this.state().incidents.values());
    const filteredIncidents = timeRange ?
      incidents.filter(i => i.createdAt >= timeRange.start && i.createdAt <= timeRange.end) :
      incidents;

    const metrics: IncidentMetrics = {
      totalIncidents: filteredIncidents.length,
      byStatus: this.groupByProperty(filteredIncidents, 'status'),
      bySeverity: this.groupByProperty(filteredIncidents, 'severity'),
      byType: this.groupByProperty(filteredIncidents, 'type'),
      mttr: await this.getMTTR(),
      mttd: await this.getMTTD(),
      mttrBySeverity: await this.getMTTRBySeverity(),
      escalationRate: this.calculateEscalationRate(filteredIncidents),
      falsePositiveRate: this.calculateFalsePositiveRate(filteredIncidents),
      automationRate: this.calculateAutomationRate(filteredIncidents),
      trends: this.calculateTrends(filteredIncidents)
    };

    this.updateState(state => {
      state.metrics = metrics;
      return state;
    });

    return metrics;
  }

  async getMTTR(severity?: IncidentSeverity): Promise<number> {
    const incidents = Array.from(this.state().incidents.values())
      .filter(i => i.status === IncidentStatus.CLOSED && i.closedAt);

    const filtered = severity ?
      incidents.filter(i => i.severity === severity) :
      incidents;

    if (filtered.length === 0) return 0;

    const totalTime = filtered.reduce((sum, i) => {
      const responseTime = i.closedAt!.getTime() - i.createdAt.getTime();
      return sum + responseTime;
    }, 0);

    return totalTime / filtered.length;
  }

  async getMTTD(type?: IncidentType): Promise<number> {
    const incidents = Array.from(this.state().incidents.values());
    const filtered = type ?
      incidents.filter(i => i.type === type) :
      incidents;

    // Simulated MTTD calculation
    return filtered.length > 0 ? 3600000 : 0; // 1 hour average
  }

  async generateIncidentReport(incidentId: string): Promise<IncidentReport> {
    const incident = await this.getIncident(incidentId);

    return {
      incidentId,
      summary: `Incident ${incident.title} - ${incident.status}`,
      timeline: this.formatTimeline(incident.timeline),
      impact: this.formatImpact(incident.estimatedImpact),
      responseActions: this.formatResponseActions(incident.responseActions),
      rootCause: incident.rootCause,
      recommendations: [],
      generatedAt: new Date()
    };
  }

  async generatePostMortem(incidentId: string): Promise<PostMortemReport> {
    const incident = await this.getIncident(incidentId);

    return {
      incidentId,
      executiveSummary: `Post-mortem analysis for ${incident.title}`,
      timeline: this.formatTimeline(incident.timeline),
      rootCause: incident.rootCause || 'Under investigation',
      impact: this.formatImpact(incident.actualImpact || incident.estimatedImpact),
      whatWentWell: this.extractWhatWentWell(incident),
      whatWentWrong: this.extractWhatWentWrong(incident),
      actionItems: this.extractActionItems(incident),
      lessonsLearned: [incident.lessonsLearned || 'None documented'],
      preventiveMeasures: this.extractPreventiveMeasures(incident),
      generatedAt: new Date()
    };
  }

  async exportIncidentData(format: 'json' | 'csv' | 'pdf'): Promise<Blob> {
    const incidents = Array.from(this.state().incidents.values());

    switch (format) {
      case 'json':
        return new Blob([JSON.stringify(incidents, null, 2)], { type: 'application/json' });
      case 'csv':
        return new Blob([this.convertToCSV(incidents)], { type: 'text/csv' });
      case 'pdf':
        // PDF generation would require additional library
        return new Blob(['PDF generation not implemented'], { type: 'application/pdf' });
    }
  }

  // Helper methods
  private updateState(updater: (state: IncidentResponseState) => IncidentResponseState): void {
    this.state.update(updater);
  }

  private generateId(prefix: string): string {
    return `${prefix}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private getDefaultConfig(): IncidentResponseConfig {
    return {
      autoEscalation: true,
      escalationThresholds: [
        { severity: IncidentSeverity.CRITICAL, timeThreshold: 15, escalateTo: EscalationLevel.INCIDENT_COMMANDER },
        { severity: IncidentSeverity.HIGH, timeThreshold: 30, escalateTo: EscalationLevel.L3_SUPPORT },
        { severity: IncidentSeverity.MEDIUM, timeThreshold: 60, escalateTo: EscalationLevel.L2_SUPPORT }
      ],
      defaultAssignment: [],
      slaConfig: {
        responseTime: [
          { severity: IncidentSeverity.CRITICAL, threshold: 15, unit: 'minutes' },
          { severity: IncidentSeverity.HIGH, threshold: 30, unit: 'minutes' },
          { severity: IncidentSeverity.MEDIUM, threshold: 2, unit: 'hours' },
          { severity: IncidentSeverity.LOW, threshold: 8, unit: 'hours' }
        ],
        resolutionTime: [
          { severity: IncidentSeverity.CRITICAL, threshold: 4, unit: 'hours' },
          { severity: IncidentSeverity.HIGH, threshold: 8, unit: 'hours' },
          { severity: IncidentSeverity.MEDIUM, threshold: 24, unit: 'hours' },
          { severity: IncidentSeverity.LOW, threshold: 72, unit: 'hours' }
        ]
      },
      notificationChannels: [],
      integrations: [],
      retentionPolicy: {
        incidentRetention: 365,
        evidenceRetention: 730,
        logRetention: 90
      }
    };
  }

  private async autoAssign(incident: SecurityIncident): Promise<void> {
    const rules = this.state().config.defaultAssignment;
    for (const rule of rules.sort((a, b) => b.priority - a.priority)) {
      if (this.evaluateAssignmentRule(rule, incident)) {
        await this.assignIncident(incident.id, rule.assignTo);
        break;
      }
    }
  }

  private evaluateAssignmentRule(rule: any, incident: SecurityIncident): boolean {
    // Evaluate assignment rule conditions
    return true;
  }

  private async checkPlaybookTriggers(incident: SecurityIncident): Promise<void> {
    const playbooks = Array.from(this.state().playbooks.values())
      .filter(p => p.enabled && p.trigger === PlaybookTrigger.AUTOMATIC);

    for (const playbook of playbooks) {
      if (playbook.type === incident.type && playbook.severity.includes(incident.severity)) {
        await this.executePlaybook(playbook.id, incident.id);
      }
    }
  }

  private setupSLAMonitoring(incident: SecurityIncident): void {
    const sla = this.state().config.slaConfig;
    const threshold = sla.responseTime.find(t => t.severity === incident.severity);

    if (threshold) {
      const timer = setTimeout(() => {
        this.handleSLABreach(incident.id, 'response');
      }, this.convertToMilliseconds(threshold.threshold, threshold.unit));

      this.escalationTimers.set(incident.id, timer);
    }
  }

  private convertToMilliseconds(value: number, unit: string): number {
    switch (unit) {
      case 'minutes': return value * 60 * 1000;
      case 'hours': return value * 60 * 60 * 1000;
      case 'days': return value * 24 * 60 * 60 * 1000;
      default: return value;
    }
  }

  private async handleSLABreach(incidentId: string, type: string): Promise<void> {
    const incident = await this.getIncident(incidentId);

    // Escalate incident
    const nextLevel = this.getNextEscalationLevel(incident.escalationLevel);
    if (nextLevel) {
      await this.escalateIncident(incidentId, nextLevel);
    }

    // Send SLA breach notification
    await this.sendNotification({
      id: this.generateId('NOTIF'),
      incidentId,
      type: 'internal',
      channel: 'email',
      recipients: ['soc-team@company.com'],
      subject: `SLA Breach: ${type} time exceeded for incident ${incidentId}`,
      content: `The ${type} SLA has been breached for incident ${incident.title}`,
      sentAt: new Date(),
      sentBy: 'System',
      status: 'sent'
    });
  }

  private getNextEscalationLevel(current?: EscalationLevel): EscalationLevel | null {
    const levels = Object.values(EscalationLevel);
    const currentIndex = current ? levels.indexOf(current) : -1;
    return currentIndex < levels.length - 1 ? levels[currentIndex + 1] : null;
  }

  private checkEscalation(incident: SecurityIncident): void {
    const config = this.state().config;
    const threshold = config.escalationThresholds.find(t => t.severity === incident.severity);

    if (threshold && !incident.escalationLevel) {
      const age = Date.now() - incident.createdAt.getTime();
      const thresholdMs = this.convertToMilliseconds(threshold.timeThreshold, 'minutes');

      if (age > thresholdMs) {
        this.escalateIncident(incident.id, threshold.escalateTo);
      }
    }
  }

  private correlateAlert(alert: any): AlertCorrelation {
    // Implement alert correlation logic
    return {
      alerts: [alert],
      confidence: 0.8,
      suggestedType: IncidentType.MALWARE,
      suggestedSeverity: IncidentSeverity.HIGH
    };
  }

  private async handleCorrelation(correlation: AlertCorrelation): Promise<void> {
    // Create or update incident based on correlation
    const incidentData = {
      title: `Correlated alerts: ${correlation.pattern}`,
      description: `Multiple related alerts detected`,
      type: correlation.suggestedType || IncidentType.OTHER,
      severity: correlation.suggestedSeverity || IncidentSeverity.MEDIUM,
      source: {
        type: 'siem' as const,
        confidence: correlation.confidence
      },
      affectedAssets: [],
      indicators: [],
      tags: ['auto-correlated']
    };

    await this.createIncident(incidentData);
  }

  private triggerAutomation(incident: SecurityIncident): void {
    const rules = Array.from(this.state().automationRules.values())
      .filter(r => r.enabled);

    for (const rule of rules) {
      if (this.evaluateAutomationRule(rule, incident)) {
        this.queueAutomationActions(rule, incident);
      }
    }
  }

  private evaluateAutomationRule(rule: AutomationRule, incident: SecurityIncident): boolean {
    // Evaluate rule conditions
    return true;
  }

  private queueAutomationActions(rule: AutomationRule, incident: SecurityIncident): void {
    for (const action of rule.actions) {
      this.actionQueue.push({
        incidentId: incident.id,
        action: action.type,
        parameters: action.parameters,
        priority: rule.priority,
        retryCount: 0
      });
    }
  }

  private processActionQueue(): void {
    const actions = this.actionQueue.sort((a, b) => b.priority - a.priority);

    for (const action of actions.slice(0, 5)) {
      this.executeAction(action.incidentId, action.action, action.parameters)
        .then(() => {
          const index = this.actionQueue.indexOf(action);
          if (index > -1) {
            this.actionQueue.splice(index, 1);
          }
        })
        .catch(() => {
          action.retryCount++;
          if (action.retryCount > 3) {
            const index = this.actionQueue.indexOf(action);
            if (index > -1) {
              this.actionQueue.splice(index, 1);
            }
          }
        });
    }
  }

  private checkAutomationTriggers(): void {
    const rules = Array.from(this.state().automationRules.values())
      .filter(r => r.enabled);

    for (const rule of rules) {
      for (const trigger of rule.triggers) {
        if (trigger.type === 'threshold') {
          this.checkThresholdTrigger(rule, trigger);
        }
      }
    }
  }

  private checkThresholdTrigger(rule: AutomationRule, trigger: any): void {
    // Check threshold triggers
  }

  private calculateMetrics(): void {
    this.getIncidentMetrics();
  }

  private evaluateConditions(conditions: any[], variables: Record<string, any>): boolean {
    // Evaluate step conditions
    return true;
  }

  private async requestApproval(executionId: string, step: PlaybookStep): Promise<void> {
    // Request manual approval
  }

  private async executeStepAction(step: PlaybookStep, incidentId: string, variables: Record<string, any>): Promise<string> {
    const result = await this.executeAction(incidentId, step.action, step.parameters);
    return result.result || 'Completed';
  }

  private logExecutionStep(executionId: string, stepId: string, status: string, details?: string): void {
    this.updateState(state => {
      const execution = state.executions.get(executionId);
      if (execution) {
        execution.logs.push({
          timestamp: new Date(),
          stepId,
          action: 'execute',
          status,
          details
        });
      }
      return state;
    });
  }

  private async retryStep(execution: PlaybookExecution, step: PlaybookStep): Promise<void> {
    // Retry step execution
  }

  private async performAction(action: ResponseAction, parameters: any, incidentId: string): Promise<string> {
    // Simulate action execution
    switch (action) {
      case ResponseAction.ISOLATE_SYSTEM:
        return 'System isolated successfully';
      case ResponseAction.BLOCK_IP:
        return `IP ${parameters?.ip} blocked`;
      case ResponseAction.DISABLE_ACCOUNT:
        return `Account ${parameters?.account} disabled`;
      default:
        return 'Action completed';
    }
  }

  private async performRollback(action: ResponseActionRecord, incidentId: string): Promise<void> {
    // Perform action rollback
  }

  private async triggerEscalationNotifications(incident: SecurityIncident, level: EscalationLevel): Promise<void> {
    // Send escalation notifications
  }

  private async simulateAction(action: AutomationAction, testData: any): Promise<any> {
    // Simulate action for testing
    return { success: true };
  }

  private async getMTTRBySeverity(): Promise<Record<IncidentSeverity, number>> {
    const result: any = {};
    for (const severity of Object.values(IncidentSeverity)) {
      result[severity] = await this.getMTTR(severity);
    }
    return result;
  }

  private generateExecutiveSummary(investigation: ForensicInvestigation): string {
    return `Investigation ${investigation.id} summary`;
  }

  private generateTechnicalDetails(investigation: ForensicInvestigation): string {
    return 'Technical analysis details';
  }

  private generateTimelineAnalysis(investigation: ForensicInvestigation): string {
    return 'Timeline analysis';
  }

  private generateRootCauseAnalysis(investigation: ForensicInvestigation): string {
    return 'Root cause analysis';
  }

  private generateImpactAssessment(investigation: ForensicInvestigation): string {
    return 'Impact assessment';
  }

  private extractLessonsLearned(investigation: ForensicInvestigation): string[] {
    return ['Lesson 1', 'Lesson 2'];
  }

  private groupByProperty<T>(items: T[], property: keyof T): Record<string, number> {
    return items.reduce((acc, item) => {
      const key = String(item[property]);
      acc[key] = (acc[key] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);
  }

  private calculateEscalationRate(incidents: SecurityIncident[]): number {
    const escalated = incidents.filter(i => i.escalationLevel).length;
    return incidents.length > 0 ? (escalated / incidents.length) * 100 : 0;
  }

  private calculateFalsePositiveRate(incidents: SecurityIncident[]): number {
    const falsePositives = incidents.filter(i => i.status === IncidentStatus.FALSE_POSITIVE).length;
    return incidents.length > 0 ? (falsePositives / incidents.length) * 100 : 0;
  }

  private calculateAutomationRate(incidents: SecurityIncident[]): number {
    const automated = incidents.filter(i =>
      i.responseActions.some(a => a.automatedExecution)
    ).length;
    return incidents.length > 0 ? (automated / incidents.length) * 100 : 0;
  }

  private calculateTrends(incidents: SecurityIncident[]): any[] {
    // Calculate incident trends
    return [];
  }

  private formatTimeline(timeline: any[]): string {
    return timeline.map(t => `${t.timestamp}: ${t.event}`).join('\n');
  }

  private formatImpact(impact?: IncidentImpact): string {
    if (!impact) return 'No impact assessment available';
    return `CIA Impact - C:${impact.confidentiality}, I:${impact.integrity}, A:${impact.availability}`;
  }

  private formatResponseActions(actions: ResponseActionRecord[]): string {
    return actions.map(a => `${a.action}: ${a.status}`).join('\n');
  }

  private extractWhatWentWell(incident: SecurityIncident): string[] {
    return ['Quick detection', 'Effective response'];
  }

  private extractWhatWentWrong(incident: SecurityIncident): string[] {
    return ['Delayed escalation', 'Incomplete documentation'];
  }

  private extractActionItems(incident: SecurityIncident): string[] {
    return ['Improve monitoring', 'Update playbooks'];
  }

  private extractPreventiveMeasures(incident: SecurityIncident): string[] {
    return ['Implement additional controls', 'Enhance training'];
  }

  private convertToCSV(incidents: SecurityIncident[]): string {
    // Convert incidents to CSV format
    const headers = ['ID', 'Title', 'Type', 'Severity', 'Status', 'Created', 'Updated'];
    const rows = incidents.map(i =>
      [i.id, i.title, i.type, i.severity, i.status, i.createdAt, i.updatedAt].join(',')
    );
    return [headers.join(','), ...rows].join('\n');
  }
}