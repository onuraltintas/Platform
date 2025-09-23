import { Directive, EventEmitter, Input, Output, OnDestroy, ElementRef, inject, AfterViewInit } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';

@Directive({
  selector: '[appDebounce]',
  standalone: true
})
export class DebounceDirective implements AfterViewInit, OnDestroy {
  @Input() debounceTime: number = 300; // ms
  @Input() debounceEvent: string = 'input'; // varsayılan event

  @Output() debounced = new EventEmitter<Event>();

  private readonly elementRef = inject(ElementRef);
  private destroy$ = new Subject<void>();

  ngAfterViewInit(): void {
    const element = this.elementRef.nativeElement;

    fromEvent(element, this.debounceEvent)
      .pipe(
        debounceTime(this.debounceTime),
        takeUntil(this.destroy$)
      )
      .subscribe((event: Event) => {
        this.debounced.emit(event);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}