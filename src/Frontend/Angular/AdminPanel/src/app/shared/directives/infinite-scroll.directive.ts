import { Directive, EventEmitter, Input, Output, ElementRef, inject, OnDestroy, AfterViewInit } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { throttleTime, takeUntil } from 'rxjs/operators';

@Directive({
  selector: '[appInfiniteScroll]',
  standalone: true
})
export class InfiniteScrollDirective implements AfterViewInit, OnDestroy {
  @Input() infiniteScrollDistance: number = 2; // Scroll distance threshold (in pixels)
  @Input() infiniteScrollThrottle: number = 150; // Throttle time in milliseconds
  @Input() infiniteScrollDisabled: boolean = false;
  @Input() infiniteScrollContainer: string | HTMLElement | null = null; // Custom scroll container

  @Output() infiniteScrolled = new EventEmitter<void>();

  private readonly elementRef = inject(ElementRef);
  private readonly destroy$ = new Subject<void>();

  private scrollContainer?: HTMLElement;

  ngAfterViewInit(): void {
    this.setupScrollContainer();
    this.setupScrollListener();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupScrollContainer(): void {
    if (typeof this.infiniteScrollContainer === 'string') {
      this.scrollContainer = document.querySelector(this.infiniteScrollContainer) as HTMLElement;
    } else if (this.infiniteScrollContainer instanceof HTMLElement) {
      this.scrollContainer = this.infiniteScrollContainer;
    } else {
      this.scrollContainer = window as Window & typeof globalThis; // Default to window
    }
  }

  private setupScrollListener(): void {
    if (!this.scrollContainer) return;

    fromEvent(this.scrollContainer, 'scroll')
      .pipe(
        throttleTime(this.infiniteScrollThrottle),
        takeUntil(this.destroy$)
      )
      .subscribe(() => {
        if (!this.infiniteScrollDisabled && this.shouldTriggerScroll()) {
          this.infiniteScrolled.emit();
        }
      });
  }

  private shouldTriggerScroll(): boolean {
    if (this.scrollContainer === window) {
      return this.checkWindowScroll();
    } else {
      return this.checkElementScroll();
    }
  }

  private checkWindowScroll(): boolean {
    const documentHeight = Math.max(
      document.body.scrollHeight,
      document.body.offsetHeight,
      document.documentElement.clientHeight,
      document.documentElement.scrollHeight,
      document.documentElement.offsetHeight
    );

    const windowHeight = window.innerHeight;
    const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

    const distanceFromBottom = documentHeight - (scrollTop + windowHeight);

    return distanceFromBottom <= this.infiniteScrollDistance;
  }

  private checkElementScroll(): boolean {
    if (!this.scrollContainer) return false;

    const element = this.scrollContainer as HTMLElement;
    const scrollTop = element.scrollTop;
    const scrollHeight = element.scrollHeight;
    const clientHeight = element.clientHeight;

    const distanceFromBottom = scrollHeight - (scrollTop + clientHeight);

    return distanceFromBottom <= this.infiniteScrollDistance;
  }
}