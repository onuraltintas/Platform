import { Directive, Input, ElementRef, inject, AfterViewInit, OnDestroy } from '@angular/core';

@Directive({
  selector: '[appAutoFocus]',
  standalone: true
})
export class AutoFocusDirective implements AfterViewInit, OnDestroy {
  @Input() autoFocusDelay: number = 0; // ms cinsinden gecikme
  @Input() autoFocusCondition: boolean = true; // Odaklanma koşulu

  private readonly elementRef = inject(ElementRef);
  private timeoutId?: number;

  ngAfterViewInit(): void {
    if (this.autoFocusCondition) {
      this.timeoutId = window.setTimeout(() => {
        this.focusElement();
      }, this.autoFocusDelay);
    }
  }

  ngOnDestroy(): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
    }
  }

  private focusElement(): void {
    const element = this.elementRef.nativeElement;

    if (element && typeof element.focus === 'function') {
      element.focus();

      // Input elementleri için cursor'u sona taşı
      if (element.tagName === 'INPUT' || element.tagName === 'TEXTAREA') {
        if (element.setSelectionRange && element.value) {
          const length = element.value.length;
          element.setSelectionRange(length, length);
        }
      }
    }
  }
}