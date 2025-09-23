import { Directive, Input, ElementRef, inject, OnDestroy, Renderer2 } from '@angular/core';

type TooltipPosition = 'top' | 'bottom' | 'left' | 'right';

@Directive({
  selector: '[appTooltip]',
  standalone: true
})
export class TooltipDirective implements OnDestroy {
  @Input() appTooltip: string = '';
  @Input() tooltipPosition: TooltipPosition = 'top';
  @Input() tooltipDelay: number = 500; // ms
  @Input() tooltipDisabled: boolean = false;

  private readonly elementRef = inject(ElementRef);
  private readonly renderer = inject(Renderer2);

  private tooltipElement?: HTMLElement;
  private showTimeout?: number;
  private hideTimeout?: number;

  constructor() {
    this.addEventListeners();
  }

  ngOnDestroy(): void {
    this.clearTimeouts();
    this.removeTooltip();
    this.removeEventListeners();
  }

  private addEventListeners(): void {
    const element = this.elementRef.nativeElement;

    this.renderer.listen(element, 'mouseenter', () => {
      if (!this.tooltipDisabled && this.appTooltip) {
        this.showTooltip();
      }
    });

    this.renderer.listen(element, 'mouseleave', () => {
      this.hideTooltip();
    });

    this.renderer.listen(element, 'focus', () => {
      if (!this.tooltipDisabled && this.appTooltip) {
        this.showTooltip();
      }
    });

    this.renderer.listen(element, 'blur', () => {
      this.hideTooltip();
    });
  }

  private removeEventListeners(): void {
    // Event listener'lar otomatik olarak temizlenecek (Renderer2 kullandığımız için)
  }

  private showTooltip(): void {
    this.clearTimeouts();

    this.showTimeout = window.setTimeout(() => {
      this.createTooltip();
    }, this.tooltipDelay);
  }

  private hideTooltip(): void {
    this.clearTimeouts();

    if (this.tooltipElement) {
      this.hideTimeout = window.setTimeout(() => {
        this.removeTooltip();
      }, 100); // Kısa gecikme ile gizle
    }
  }

  private createTooltip(): void {
    if (this.tooltipElement) {
      this.removeTooltip();
    }

    const tooltip = this.renderer.createElement('div');
    this.renderer.addClass(tooltip, 'app-tooltip');
    this.renderer.addClass(tooltip, `app-tooltip-${this.tooltipPosition}`);
    this.renderer.setProperty(tooltip, 'textContent', this.appTooltip);

    // Tooltip stillerini ayarla
    this.setTooltipStyles(tooltip);

    // DOM'a ekle
    this.renderer.appendChild(document.body, tooltip);
    this.tooltipElement = tooltip;

    // Pozisyonu hesapla ve ayarla
    setTimeout(() => {
      this.positionTooltip();
    }, 0);
  }

  private setTooltipStyles(tooltip: HTMLElement): void {
    const styles = {
      'position': 'absolute',
      'z-index': '10000',
      'background-color': '#333',
      'color': '#fff',
      'padding': '8px 12px',
      'border-radius': '4px',
      'font-size': '12px',
      'font-weight': '400',
      'line-height': '1.4',
      'max-width': '250px',
      'word-wrap': 'break-word',
      'box-shadow': '0 2px 8px rgba(0, 0, 0, 0.15)',
      'opacity': '0',
      'transform': 'scale(0.8)',
      'transition': 'opacity 0.15s, transform 0.15s',
      'pointer-events': 'none'
    };

    Object.entries(styles).forEach(([property, value]) => {
      this.renderer.setStyle(tooltip, property, value);
    });

    // Show animation
    setTimeout(() => {
      this.renderer.setStyle(tooltip, 'opacity', '1');
      this.renderer.setStyle(tooltip, 'transform', 'scale(1)');
    }, 10);
  }

  private positionTooltip(): void {
    if (!this.tooltipElement) return;

    const element = this.elementRef.nativeElement;
    const tooltip = this.tooltipElement;
    const elementRect = element.getBoundingClientRect();
    const tooltipRect = tooltip.getBoundingClientRect();

    let top = 0;
    let left = 0;

    switch (this.tooltipPosition) {
      case 'top':
        top = elementRect.top - tooltipRect.height - 8;
        left = elementRect.left + (elementRect.width - tooltipRect.width) / 2;
        break;
      case 'bottom':
        top = elementRect.bottom + 8;
        left = elementRect.left + (elementRect.width - tooltipRect.width) / 2;
        break;
      case 'left':
        top = elementRect.top + (elementRect.height - tooltipRect.height) / 2;
        left = elementRect.left - tooltipRect.width - 8;
        break;
      case 'right':
        top = elementRect.top + (elementRect.height - tooltipRect.height) / 2;
        left = elementRect.right + 8;
        break;
    }

    // Viewport sınırlarını kontrol et
    const viewport = {
      width: window.innerWidth,
      height: window.innerHeight
    };

    // Sol sınır kontrolü
    if (left < 8) {
      left = 8;
    }

    // Sağ sınır kontrolü
    if (left + tooltipRect.width > viewport.width - 8) {
      left = viewport.width - tooltipRect.width - 8;
    }

    // Üst sınır kontrolü
    if (top < 8) {
      top = 8;
    }

    // Alt sınır kontrolü
    if (top + tooltipRect.height > viewport.height - 8) {
      top = viewport.height - tooltipRect.height - 8;
    }

    // Scroll offset'ini ekle
    top += window.pageYOffset;
    left += window.pageXOffset;

    this.renderer.setStyle(tooltip, 'top', `${top}px`);
    this.renderer.setStyle(tooltip, 'left', `${left}px`);
  }

  private removeTooltip(): void {
    if (this.tooltipElement) {
      // Fade out animation
      this.renderer.setStyle(this.tooltipElement, 'opacity', '0');
      this.renderer.setStyle(this.tooltipElement, 'transform', 'scale(0.8)');

      setTimeout(() => {
        if (this.tooltipElement) {
          this.renderer.removeChild(document.body, this.tooltipElement);
          this.tooltipElement = undefined;
        }
      }, 150);
    }
  }

  private clearTimeouts(): void {
    if (this.showTimeout) {
      clearTimeout(this.showTimeout);
      this.showTimeout = undefined;
    }

    if (this.hideTimeout) {
      clearTimeout(this.hideTimeout);
      this.hideTimeout = undefined;
    }
  }
}