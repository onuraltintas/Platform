import { Directive, ElementRef, EventEmitter, Output, inject, OnDestroy } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';

@Directive({
  selector: '[appClickOutside]',
  standalone: true
})
export class ClickOutsideDirective implements OnDestroy {
  @Output() clickOutside = new EventEmitter<Event>();

  private readonly elementRef = inject(ElementRef);
  private readonly destroy$ = new Subject<void>();

  constructor() {
    this.initializeClickListener();
  }

  private initializeClickListener(): void {
    fromEvent<MouseEvent>(document, 'click')
      .pipe(
        filter(event => {
          const target = event.target as HTMLElement;
          const element = this.elementRef.nativeElement;

          // Element veya alt elementlerine tıklanıp tıklanmadığını kontrol et
          return !element.contains(target);
        }),
        takeUntil(this.destroy$)
      )
      .subscribe(event => {
        this.clickOutside.emit(event);
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}