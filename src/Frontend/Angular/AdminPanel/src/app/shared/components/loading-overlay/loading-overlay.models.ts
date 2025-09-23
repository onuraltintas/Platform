import { TemplateRef } from '@angular/core';
import { Observable } from 'rxjs';

export interface LoadingOverlayConfig {
  // Display Configuration
  message?: string;
  submessage?: string;
  showSpinner?: boolean;
  showProgress?: boolean;
  progress?: number; // 0-100

  // Appearance
  spinnerType?: SpinnerType;
  spinnerSize?: SpinnerSize;
  spinnerColor?: 'primary' | 'accent' | 'warn';
  backdrop?: boolean;
  backdropClass?: string;

  // Positioning
  position?: 'center' | 'top' | 'bottom';
  fullscreen?: boolean;
  zIndex?: number;

  // Animation
  fadeInDuration?: number;
  fadeOutDuration?: number;
  pulseAnimation?: boolean;

  // Custom Content
  customTemplate?: TemplateRef<unknown>;
  icon?: string;
  iconSize?: string;

  // Behavior
  clickToClose?: boolean;
  autoHide?: boolean;
  autoHideDelay?: number;
  preventScroll?: boolean;

  // Accessibility
  ariaLabel?: string;
  ariaDescribedBy?: string;
}

export type SpinnerType =
  | 'circular'
  | 'linear'
  | 'dots'
  | 'pulse'
  | 'wave'
  | 'bounce'
  | 'skeleton'
  | 'custom';

export type SpinnerSize = 'small' | 'medium' | 'large' | 'xlarge';

export interface LoadingState {
  active: boolean;
  config: LoadingOverlayConfig;
  startTime: Date;
  id: string;
}

export interface LoadingOverlayRef {
  id: string;
  hide(): void;
  updateConfig(config: Partial<LoadingOverlayConfig>): void;
  updateProgress(progress: number): void;
  updateMessage(message: string, submessage?: string): void;
}

// Preset configurations
export const LOADING_OVERLAY_PRESETS = {
  DEFAULT: {
    message: 'Yükleniyor...',
    showSpinner: true,
    spinnerType: 'circular' as SpinnerType,
    spinnerSize: 'medium' as SpinnerSize,
    spinnerColor: 'primary' as const,
    backdrop: true,
    position: 'center' as const,
    fullscreen: false,
    fadeInDuration: 200,
    fadeOutDuration: 200
  },
  MINIMAL: {
    showSpinner: true,
    spinnerType: 'circular' as SpinnerType,
    spinnerSize: 'small' as SpinnerSize,
    backdrop: false,
    position: 'center' as const,
    fullscreen: false
  },
  FULLSCREEN: {
    message: 'Yükleniyor...',
    showSpinner: true,
    spinnerType: 'circular' as SpinnerType,
    spinnerSize: 'large' as SpinnerSize,
    backdrop: true,
    position: 'center' as const,
    fullscreen: true,
    preventScroll: true,
    zIndex: 9999
  },
  PROGRESS: {
    message: 'İşleniyor...',
    showSpinner: false,
    showProgress: true,
    backdrop: true,
    position: 'center' as const,
    fullscreen: false
  },
  SKELETON: {
    spinnerType: 'skeleton' as SpinnerType,
    backdrop: false,
    fullscreen: false,
    showSpinner: true
  },
  SAVING: {
    message: 'Kaydediliyor...',
    submessage: 'Lütfen bekleyiniz',
    showSpinner: true,
    spinnerType: 'pulse' as SpinnerType,
    spinnerSize: 'medium' as SpinnerSize,
    backdrop: true,
    autoHide: true,
    autoHideDelay: 3000
  },
  LOADING_DATA: {
    message: 'Veriler yükleniyor...',
    showSpinner: true,
    spinnerType: 'dots' as SpinnerType,
    spinnerSize: 'medium' as SpinnerSize,
    backdrop: true,
    position: 'center' as const
  },
  PROCESSING: {
    message: 'İşlem yapılıyor...',
    submessage: 'Bu işlem birkaç saniye sürebilir',
    showSpinner: true,
    showProgress: true,
    spinnerType: 'wave' as SpinnerType,
    backdrop: true,
    fullscreen: true,
    preventScroll: true
  }
} as const;

// Loading overlay service interface
export interface LoadingOverlayService {
  // Basic methods
  show(config?: Partial<LoadingOverlayConfig>): LoadingOverlayRef;
  hide(id?: string): void;
  hideAll(): void;

  // Preset methods
  showDefault(message?: string): LoadingOverlayRef;
  showMinimal(): LoadingOverlayRef;
  showFullscreen(message?: string): LoadingOverlayRef;
  showProgress(message?: string, progress?: number): LoadingOverlayRef;
  showSkeleton(): LoadingOverlayRef;
  showSaving(message?: string): LoadingOverlayRef;
  showLoadingData(message?: string): LoadingOverlayRef;
  showProcessing(message?: string): LoadingOverlayRef;

  // State methods
  isActive(id?: string): boolean;
  getActiveCount(): number;
  getActiveLoadings(): LoadingState[];

  // Promise wrapper
  wrapPromise<T>(promise: Promise<T>, config?: Partial<LoadingOverlayConfig>): Promise<T>;
  wrapObservable<T>(observable: Observable<T>, config?: Partial<LoadingOverlayConfig>): Observable<T>;
}