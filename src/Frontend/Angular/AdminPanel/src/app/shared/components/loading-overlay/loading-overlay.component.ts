import { Component, Input, signal, computed, effect, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';
import { trigger, state, style, transition, animate, keyframes } from '@angular/animations';

import { LoadingOverlayConfig, LOADING_OVERLAY_PRESETS } from './loading-overlay.models';

@Component({
  selector: 'app-loading-overlay',
  standalone: true,
  imports: [
    CommonModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatIconModule
  ],
  templateUrl: './loading-overlay.component.html',
  styleUrl: './loading-overlay.component.scss',
  animations: [
    trigger('fadeInOut', [
      state('visible', style({ opacity: 1 })),
      state('hidden', style({ opacity: 0 })),
      transition('hidden => visible', [
        animate('{{ fadeInDuration }}ms ease-out')
      ]),
      transition('visible => hidden', [
        animate('{{ fadeOutDuration }}ms ease-in')
      ])
    ]),
    trigger('pulse', [
      transition('* => *', [
        animate('1s ease-in-out', keyframes([
          style({ transform: 'scale(1)', offset: 0 }),
          style({ transform: 'scale(1.1)', offset: 0.5 }),
          style({ transform: 'scale(1)', offset: 1 })
        ]))
      ])
    ]),
    trigger('wave', [
      transition('* => *', [
        animate('1.2s ease-in-out infinite', keyframes([
          style({ height: '8px', offset: 0 }),
          style({ height: '32px', offset: 0.2 }),
          style({ height: '8px', offset: 0.4 }),
          style({ height: '8px', offset: 1 })
        ]))
      ])
    ]),
    trigger('bounce', [
      transition('* => *', [
        animate('1.4s ease-in-out infinite', keyframes([
          style({ transform: 'scale(0)', offset: 0 }),
          style({ transform: 'scale(1)', offset: 0.4 }),
          style({ transform: 'scale(0)', offset: 0.8 }),
          style({ transform: 'scale(0)', offset: 1 })
        ]))
      ])
    ])
  ]
})
export class LoadingOverlayComponent implements OnInit, OnDestroy {
  @Input() config = signal<LoadingOverlayConfig>(LOADING_OVERLAY_PRESETS.DEFAULT);
  @Input() visible = signal<boolean>(true);

  public readonly isVisible = computed(() => this.visible());

  // Computed styles and classes
  public readonly overlayStyles = computed(() => {
    const config = this.config();
    return {
      'z-index': config.zIndex || 1000,
      'position': config.fullscreen ? 'fixed' : 'absolute'
    };
  });

  public readonly contentPositionClass = computed(() => {
    const position = this.config().position;
    return {
      'top': position === 'top',
      'bottom': position === 'bottom',
      'center': position === 'center' || !position
    };
  });

  public readonly spinnerSizeClass = computed(() => {
    const size = this.config().spinnerSize;
    return {
      'small': size === 'small',
      'medium': size === 'medium' || !size,
      'large': size === 'large',
      'xlarge': size === 'xlarge',
      'minimal': !this.config().backdrop && !this.config().message
    };
  });

  public readonly spinnerDiameter = computed(() => {
    const size = this.config().spinnerSize;
    switch (size) {
      case 'small': return 24;
      case 'large': return 64;
      case 'xlarge': return 80;
      default: return 40;
    }
  });

  private autoHideTimeout?: number;

  ngOnInit(): void {
    this.setupAutoHide();
    this.setupScrollPrevention();
  }

  ngOnDestroy(): void {
    this.clearAutoHide();
    this.restoreScroll();
  }

  private setupAutoHide(): void {
    effect(() => {
      const config = this.config();
      if (config.autoHide && config.autoHideDelay && this.isVisible()) {
        this.clearAutoHide();
        this.autoHideTimeout = window.setTimeout(() => {
          this.visible.set(false);
        }, config.autoHideDelay);
      }
    });
  }

  private clearAutoHide(): void {
    if (this.autoHideTimeout) {
      clearTimeout(this.autoHideTimeout);
      this.autoHideTimeout = undefined;
    }
  }

  private setupScrollPrevention(): void {
    effect(() => {
      const config = this.config();
      if (config.preventScroll && this.isVisible()) {
        document.body.style.overflow = 'hidden';
      } else {
        document.body.style.overflow = '';
      }
    });
  }

  private restoreScroll(): void {
    document.body.style.overflow = '';
  }

  public onOverlayClick(event: Event): void {
    const config = this.config();
    if (config.clickToClose && event.target === event.currentTarget) {
      this.visible.set(false);
    }
  }

  public updateConfig(newConfig: Partial<LoadingOverlayConfig>): void {
    this.config.update(current => ({ ...current, ...newConfig }));
  }

  public updateProgress(progress: number): void {
    this.config.update(current => ({ ...current, progress }));
  }

  public updateMessage(message: string, submessage?: string): void {
    this.config.update(current => ({
      ...current,
      message,
      submessage: submessage !== undefined ? submessage : current.submessage
    }));
  }
}