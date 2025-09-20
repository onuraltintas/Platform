import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private loadingCounterSubject = new BehaviorSubject<number>(0);
  private loadingMessageSubject = new BehaviorSubject<string>('');

  public isLoading$ = this.loadingSubject.asObservable();
  public loadingMessage$ = this.loadingMessageSubject.asObservable();
  public loadingCounter$ = this.loadingCounterSubject.asObservable();

  show(message?: string): void {
    const currentCount = this.loadingCounterSubject.value;
    this.loadingCounterSubject.next(currentCount + 1);

    if (message) {
      this.loadingMessageSubject.next(message);
    }

    this.loadingSubject.next(true);
  }

  hide(): void {
    const currentCount = this.loadingCounterSubject.value;
    const newCount = Math.max(0, currentCount - 1);
    this.loadingCounterSubject.next(newCount);

    if (newCount === 0) {
      this.loadingSubject.next(false);
      this.loadingMessageSubject.next('');
    }
  }

  reset(): void {
    this.loadingCounterSubject.next(0);
    this.loadingSubject.next(false);
    this.loadingMessageSubject.next('');
  }

  get isLoading(): boolean {
    return this.loadingSubject.value;
  }

  get loadingCounter(): number {
    return this.loadingCounterSubject.value;
  }
}