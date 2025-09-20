import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  message: string;
  title?: string;
  duration?: number;
  actions?: NotificationAction[];
  timestamp: Date;
}

export interface NotificationAction {
  label: string;
  action: () => void;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private readonly defaultDuration = 5000; // 5 seconds
  private notificationsSubject = new BehaviorSubject<Notification[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  success(message: string, title?: string, duration?: number): void {
    this.show('success', message, title, duration);
  }

  error(message: string, title?: string, duration?: number): void {
    this.show('error', message, title || 'Hata', duration || 8000);
  }

  warning(message: string, title?: string, duration?: number): void {
    this.show('warning', message, title || 'Uyarı', duration);
  }

  info(message: string, title?: string, duration?: number): void {
    this.show('info', message, title || 'Bilgi', duration);
  }

  show(
    type: Notification['type'],
    message: string,
    title?: string,
    duration?: number,
    actions?: NotificationAction[]
  ): void {
    const notification: Notification = {
      id: this.generateId(),
      type,
      message,
      title,
      duration: duration || this.defaultDuration,
      actions,
      timestamp: new Date()
    };

    const currentNotifications = this.notificationsSubject.value;
    this.notificationsSubject.next([...currentNotifications, notification]);

    // Auto-remove notification after duration
    if (notification.duration && notification.duration > 0) {
      setTimeout(() => {
        this.remove(notification.id);
      }, notification.duration);
    }
  }

  remove(id: string): void {
    const currentNotifications = this.notificationsSubject.value;
    const filtered = currentNotifications.filter(n => n.id !== id);
    this.notificationsSubject.next(filtered);
  }

  clear(): void {
    this.notificationsSubject.next([]);
  }

  getNotifications(): Notification[] {
    return this.notificationsSubject.value;
  }

  dismiss(id: string): void {
    this.remove(id);
  }

  dismissAll(): void {
    this.clear();
  }

  private generateId(): string {
    return `notification-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }
}