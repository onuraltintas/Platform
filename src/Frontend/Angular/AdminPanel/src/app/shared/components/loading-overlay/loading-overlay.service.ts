import { Injectable, signal, computed, ApplicationRef, createComponent, inject, EnvironmentInjector, ComponentRef } from '@angular/core';
import { Observable, finalize } from 'rxjs';

import { LoadingOverlayComponent } from './loading-overlay.component';
import {
  LoadingOverlayConfig,
  LoadingOverlayRef,
  LoadingOverlayService as ILoadingOverlayService,
  LoadingState,
  LOADING_OVERLAY_PRESETS
} from './loading-overlay.models';

interface ActiveLoading extends LoadingState {
  componentRef: ComponentRef<LoadingOverlayComponent>;
  hostElement: HTMLElement;
}

@Injectable({
  providedIn: 'root'
})
export class LoadingOverlayService implements ILoadingOverlayService {
  private readonly appRef = inject(ApplicationRef);
  private readonly environmentInjector = inject(EnvironmentInjector);

  private readonly activeLoadings = signal<Map<string, ActiveLoading>>(new Map());
  private idCounter = 0;

  // Computed properties
  public readonly activeCount = computed(() => this.activeLoadings().size);
  public readonly hasActiveLoadings = computed(() => this.activeCount() > 0);

  /**
   * Genel loading overlay gösterir
   */
  public show(config: Partial<LoadingOverlayConfig> = {}): LoadingOverlayRef {
    const id = this.generateId();
    const mergedConfig = this.mergeWithDefaults(config);

    const componentRef = this.createLoadingComponent(mergedConfig);
    const hostElement = this.appendToDOM(componentRef);

    const activeLoading: ActiveLoading = {
      id,
      active: true,
      config: mergedConfig,
      startTime: new Date(),
      componentRef,
      hostElement
    };

    this.activeLoadings.update(loadings => {
      const newLoadings = new Map(loadings);
      newLoadings.set(id, activeLoading);
      return newLoadings;
    });

    return this.createLoadingRef(id);
  }

  /**
   * Belirli bir loading'i gizler
   */
  public hide(id?: string): void {
    if (id) {
      this.hideSpecific(id);
    } else {
      this.hideLatest();
    }
  }

  /**
   * Tüm loading'leri gizler
   */
  public hideAll(): void {
    const loadings = this.activeLoadings();
    loadings.forEach((_, id) => this.hideSpecific(id));
  }

  /**
   * Varsayılan loading gösterir
   */
  public showDefault(message?: string): LoadingOverlayRef {
    return this.show({
      ...LOADING_OVERLAY_PRESETS.DEFAULT,
      message: message || LOADING_OVERLAY_PRESETS.DEFAULT.message
    });
  }

  /**
   * Minimal loading gösterir
   */
  public showMinimal(): LoadingOverlayRef {
    return this.show(LOADING_OVERLAY_PRESETS.MINIMAL);
  }

  /**
   * Tam ekran loading gösterir
   */
  public showFullscreen(message?: string): LoadingOverlayRef {
    return this.show({
      ...LOADING_OVERLAY_PRESETS.FULLSCREEN,
      message: message || LOADING_OVERLAY_PRESETS.FULLSCREEN.message
    });
  }

  /**
   * Progress loading gösterir
   */
  public showProgress(message?: string, progress?: number): LoadingOverlayRef {
    return this.show({
      ...LOADING_OVERLAY_PRESETS.PROGRESS,
      message: message || LOADING_OVERLAY_PRESETS.PROGRESS.message,
      progress
    });
  }

  /**
   * Skeleton loading gösterir
   */
  public showSkeleton(): LoadingOverlayRef {
    return this.show(LOADING_OVERLAY_PRESETS.SKELETON);
  }

  /**
   * Kaydetme loading'i gösterir
   */
  public showSaving(message?: string): LoadingOverlayRef {
    return this.show({
      ...LOADING_OVERLAY_PRESETS.SAVING,
      message: message || LOADING_OVERLAY_PRESETS.SAVING.message
    });
  }

  /**
   * Veri yükleme loading'i gösterir
   */
  public showLoadingData(message?: string): LoadingOverlayRef {
    return this.show({
      ...LOADING_OVERLAY_PRESETS.LOADING_DATA,
      message: message || LOADING_OVERLAY_PRESETS.LOADING_DATA.message
    });
  }

  /**
   * İşlem loading'i gösterir
   */
  public showProcessing(message?: string): LoadingOverlayRef {
    return this.show({
      ...LOADING_OVERLAY_PRESETS.PROCESSING,
      message: message || LOADING_OVERLAY_PRESETS.PROCESSING.message
    });
  }

  /**
   * Belirli bir loading aktif mi kontrol eder
   */
  public isActive(id?: string): boolean {
    if (id) {
      return this.activeLoadings().has(id);
    }
    return this.hasActiveLoadings();
  }

  /**
   * Aktif loading sayısını döner
   */
  public getActiveCount(): number {
    return this.activeCount();
  }

  /**
   * Aktif loading'leri döner
   */
  public getActiveLoadings(): LoadingState[] {
    return Array.from(this.activeLoadings().values()).map(loading => ({
      id: loading.id,
      active: loading.active,
      config: loading.config,
      startTime: loading.startTime
    }));
  }

  /**
   * Promise'i loading ile wrap eder
   */
  public async wrapPromise<T>(
    promise: Promise<T>,
    config: Partial<LoadingOverlayConfig> = {}
  ): Promise<T> {
    const loadingRef = this.show(config);

    try {
      const result = await promise;
      return result;
    } finally {
      loadingRef.hide();
    }
  }

  /**
   * Observable'ı loading ile wrap eder
   */
  public wrapObservable<T>(
    observable: Observable<T>,
    config: Partial<LoadingOverlayConfig> = {}
  ): Observable<T> {
    const loadingRef = this.show(config);

    return observable.pipe(
      finalize(() => loadingRef.hide())
    );
  }

  // Utility methods
  private generateId(): string {
    return `loading-${++this.idCounter}-${Date.now()}`;
  }

  private mergeWithDefaults(config: Partial<LoadingOverlayConfig>): LoadingOverlayConfig {
    return {
      ...LOADING_OVERLAY_PRESETS.DEFAULT,
      ...config
    };
  }

  private createLoadingComponent(config: LoadingOverlayConfig): ComponentRef<LoadingOverlayComponent> {
    const componentRef = createComponent(LoadingOverlayComponent, {
      environmentInjector: this.environmentInjector
    });

    componentRef.setInput('config', config);
    componentRef.setInput('visible', true);

    this.appRef.attachView(componentRef.hostView);

    return componentRef;
  }

  private appendToDOM(componentRef: ComponentRef<LoadingOverlayComponent>): HTMLElement {
    const hostElement = componentRef.location.nativeElement;
    document.body.appendChild(hostElement);
    return hostElement;
  }

  private hideSpecific(id: string): void {
    const loadings = this.activeLoadings();
    const loading = loadings.get(id);

    if (loading) {
      // Fade out animation
      const component = loading.componentRef.instance;
      component.visible.set(false);

      // Remove after animation
      setTimeout(() => {
        this.removeLoading(id);
      }, loading.config.fadeOutDuration || 200);
    }
  }

  private hideLatest(): void {
    const loadings = this.activeLoadings();
    if (loadings.size > 0) {
      const latestId = Array.from(loadings.keys()).pop();
      if (latestId) {
        this.hideSpecific(latestId);
      }
    }
  }

  private removeLoading(id: string): void {
    const loadings = this.activeLoadings();
    const loading = loadings.get(id);

    if (loading) {
      // Remove from DOM
      if (loading.hostElement && loading.hostElement.parentNode) {
        loading.hostElement.parentNode.removeChild(loading.hostElement);
      }

      // Destroy component
      if (loading.componentRef) {
        this.appRef.detachView(loading.componentRef.hostView);
        loading.componentRef.destroy();
      }

      // Remove from active loadings
      this.activeLoadings.update(loadings => {
        const newLoadings = new Map(loadings);
        newLoadings.delete(id);
        return newLoadings;
      });
    }
  }

  private createLoadingRef(id: string): LoadingOverlayRef {
    return {
      id,
      hide: () => this.hideSpecific(id),
      updateConfig: (config: Partial<LoadingOverlayConfig>) => {
        const loading = this.activeLoadings().get(id);
        if (loading) {
          const newConfig = { ...loading.config, ...config };
          loading.componentRef.instance.updateConfig(newConfig);

          this.activeLoadings.update(loadings => {
            const newLoadings = new Map(loadings);
            const updatedLoading = { ...loading, config: newConfig };
            newLoadings.set(id, updatedLoading);
            return newLoadings;
          });
        }
      },
      updateProgress: (progress: number) => {
        const loading = this.activeLoadings().get(id);
        if (loading) {
          loading.componentRef.instance.updateProgress(progress);
        }
      },
      updateMessage: (message: string, submessage?: string) => {
        const loading = this.activeLoadings().get(id);
        if (loading) {
          loading.componentRef.instance.updateMessage(message, submessage);
        }
      }
    };
  }

  /**
   * Hızlı erişim methodları
   */
  public readonly quick = {
    /**
     * Hızlı veri yükleme
     */
    data: (message?: string) => this.showLoadingData(message),

    /**
     * Hızlı kaydetme
     */
    save: (message?: string) => this.showSaving(message),

    /**
     * Hızlı işlem
     */
    process: (message?: string) => this.showProcessing(message),

    /**
     * Hızlı tam ekran
     */
    fullscreen: (message?: string) => this.showFullscreen(message),

    /**
     * Hızlı minimal
     */
    minimal: () => this.showMinimal(),

    /**
     * Hızlı skeleton
     */
    skeleton: () => this.showSkeleton()
  };

  /**
   * Debug bilgileri
   */
  public getDebugInfo(): {
    activeCount: number;
    activeLoadings: LoadingState[];
    totalLoadingsCreated: number;
  } {
    return {
      activeCount: this.getActiveCount(),
      activeLoadings: this.getActiveLoadings(),
      totalLoadingsCreated: this.idCounter
    };
  }
}