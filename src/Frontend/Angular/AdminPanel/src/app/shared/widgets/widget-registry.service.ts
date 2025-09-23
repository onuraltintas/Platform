import { Injectable, signal } from '@angular/core';

export type WidgetData = {
  value?: number | string;
  subtitle?: string;
  trend?: { direction: 'up' | 'down' | 'flat'; value?: number };
  extra?: Record<string, unknown>;
};

export type WidgetDefinition = {
  id: string;
  module: 'identity' | 'user' | 'speed' | 'system';
  title: string;
  permission?: string; // widget görünürlüğü için
  fetch: () => Promise<WidgetData>;
  route?: string; // tıklandığında gidilecek
};

@Injectable({ providedIn: 'root' })
export class WidgetRegistryService {
  private readonly widgetsSignal = signal<WidgetDefinition[]>([]);

  register(...defs: WidgetDefinition[]): void {
    const current = this.widgetsSignal();
    const map = new Map(current.map(w => [w.id, w] as const));
    for (const d of defs) {
      map.set(d.id, d);
    }
    this.widgetsSignal.set(Array.from(map.values()));
  }

  clear(): void {
    this.widgetsSignal.set([]);
  }

  list(): WidgetDefinition[] {
    return this.widgetsSignal();
  }
}

