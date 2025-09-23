import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface AuditLogEntry {
  id: string;
  timestamp: number;
  level: 'info' | 'warning' | 'error' | 'critical';
  category: 'security' | 'performance' | 'access' | 'data' | 'system';
  action: string;
  details: any;
  userId?: string;
  sessionId?: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface AuditLogFilter {
  level?: 'info' | 'warning' | 'error' | 'critical';
  category?: 'security' | 'performance' | 'access' | 'data' | 'system';
  dateFrom?: number;
  dateTo?: number;
  userId?: string;
  action?: string;
}

/**
 * Audit Logging Service for Security Events
 * Lightweight implementation for lazy loading
 */
@Injectable({
  providedIn: 'root'
})
export class AuditLoggingService {
  private auditLogs$ = new BehaviorSubject<AuditLogEntry[]>([]);
  private readonly maxLogs = 1000;

  constructor() {
    console.log('📝 Audit Logging Service initialized');
  }

  /**
   * Log audit event
   */
  log(
    level: 'info' | 'warning' | 'error' | 'critical',
    category: 'security' | 'performance' | 'access' | 'data' | 'system',
    action: string,
    details: any = {},
    context?: { userId?: string; sessionId?: string; ipAddress?: string; userAgent?: string }
  ): void {
    const entry: AuditLogEntry = {
      id: crypto.randomUUID(),
      timestamp: Date.now(),
      level,
      category,
      action,
      details,
      ...context
    };

    const currentLogs = this.auditLogs$.value;
    const newLogs = [entry, ...currentLogs].slice(0, this.maxLogs);
    this.auditLogs$.next(newLogs);

    // Console output for development
    if (level === 'critical' || level === 'error') {
      console.error(`🚨 AUDIT [${level.toUpperCase()}] ${category}:`, action, details);
    } else if (level === 'warning') {
      console.warn(`⚠️ AUDIT [${level.toUpperCase()}] ${category}:`, action, details);
    } else {
      console.log(`📝 AUDIT [${level.toUpperCase()}] ${category}:`, action, details);
    }
  }

  /**
   * Get audit trail
   */
  getAuditTrail(filter?: AuditLogFilter): Observable<AuditLogEntry[]> {
    if (!filter) {
      return this.auditLogs$.asObservable();
    }

    return new Observable(subscriber => {
      this.auditLogs$.subscribe(logs => {
        const filteredLogs = logs.filter(log => {
          if (filter.level && log.level !== filter.level) return false;
          if (filter.category && log.category !== filter.category) return false;
          if (filter.dateFrom && log.timestamp < filter.dateFrom) return false;
          if (filter.dateTo && log.timestamp > filter.dateTo) return false;
          if (filter.userId && log.userId !== filter.userId) return false;
          if (filter.action && !log.action.includes(filter.action)) return false;
          return true;
        });
        subscriber.next(filteredLogs);
      });
    });
  }

  /**
   * Clear audit logs
   */
  clearLogs(): void {
    this.auditLogs$.next([]);
    this.log('info', 'system', 'Audit logs cleared');
  }

  /**
   * Export audit logs
   */
  exportLogs(format: 'json' | 'csv' = 'json'): string {
    const logs = this.auditLogs$.value;

    if (format === 'csv') {
      const headers = ['timestamp', 'level', 'category', 'action', 'userId', 'details'];
      const csvRows = [
        headers.join(','),
        ...logs.map(log => [
          new Date(log.timestamp).toISOString(),
          log.level,
          log.category,
          log.action,
          log.userId || '',
          JSON.stringify(log.details).replace(/"/g, '""')
        ].join(','))
      ];
      return csvRows.join('\n');
    }

    return JSON.stringify(logs, null, 2);
  }
}