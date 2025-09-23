import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class GroupContextService {
  private readonly storageKey = 'selected_group_id';
  private readonly _groupId = signal<string | null>(this.load());

  get groupId() {
    return this._groupId();
  }

  setGroup(groupId: string | null): void {
    this._groupId.set(groupId);
    if (groupId) {
      localStorage.setItem(this.storageKey, groupId);
    } else {
      localStorage.removeItem(this.storageKey);
    }
  }

  private load(): string | null {
    const v = localStorage.getItem(this.storageKey);
    return v && v.length > 0 ? v : null;
  }
}

