import { Injectable, signal, computed, effect, Signal, WritableSignal } from '@angular/core';
import { Observable, Subject } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

export interface BaseState {
  loading: boolean;
  error: string | null;
  lastUpdated: Date | null;
}

export interface LoadingState {
  [key: string]: boolean;
}

export interface ErrorState {
  [key: string]: string | null;
}

export interface PaginationState {
  page: number;
  pageSize: number;
  total: number;
  totalPages: number;
}

export interface FilterState {
  [key: string]: any;
}

export interface SortState {
  field: string;
  direction: 'asc' | 'desc';
}

@Injectable()
export abstract class BaseStore<T extends BaseState> {
  // Ana state
  protected readonly _state: WritableSignal<T>;

  // Loading states (operation-specific)
  protected readonly _loadingStates = signal<LoadingState>({});

  // Error states (operation-specific)
  protected readonly _errorStates = signal<ErrorState>({});

  // Computed signals
  public readonly state = computed(() => this._state());
  public readonly isLoading = computed(() => this._state().loading);
  public readonly error = computed(() => this._state().error);
  public readonly lastUpdated = computed(() => this._state().lastUpdated);
  public readonly hasError = computed(() => !!this._state().error);

  // Loading states
  public readonly loadingStates = computed(() => this._loadingStates());
  public readonly hasAnyLoading = computed(() =>
    Object.values(this._loadingStates()).some(loading => loading)
  );

  // Error states
  public readonly errorStates = computed(() => this._errorStates());
  public readonly hasAnyError = computed(() =>
    Object.values(this._errorStates()).some(error => !!error)
  );

  // Events
  protected readonly _events = new Subject<{ type: string; payload?: any }>();
  public readonly events$ = this._events.asObservable();

  constructor(initialState: T) {
    this._state = signal(initialState);

    // Auto-save to localStorage effect (override edilebilir)
    if (this.enableAutoSave) {
      effect(() => {
        const state = this._state();
        this.saveToStorage(state);
      });
    }

    // Load from localStorage on init
    if (this.enableAutoSave) {
      const savedState = this.loadFromStorage();
      if (savedState) {
        this.patchState(savedState);
      }
    }
  }

  /**
   * State'i tamamen değiştirir
   */
  protected setState(newState: T): void {
    this._state.set({
      ...newState,
      lastUpdated: new Date()
    });
    this.emitEvent('STATE_CHANGED', newState);
  }

  /**
   * State'in sadece belirli kısımlarını günceller
   */
  protected patchState(updates: Partial<T>): void {
    const currentState = this._state();
    const newState = {
      ...currentState,
      ...updates,
      lastUpdated: new Date()
    } as T;

    this._state.set(newState);
    this.emitEvent('STATE_PATCHED', updates);
  }

  /**
   * Loading durumunu ayarlar
   */
  protected setLoading(loading: boolean, operation?: string): void {
    if (operation) {
      this._loadingStates.update(states => ({
        ...states,
        [operation]: loading
      }));
    } else {
      this.patchState({ loading } as Partial<T>);
    }
  }

  /**
   * Error durumunu ayarlar
   */
  protected setError(error: string | null, operation?: string): void {
    if (operation) {
      this._errorStates.update(states => ({
        ...states,
        [operation]: error
      }));
    } else {
      this.patchState({ error } as Partial<T>);
    }
  }

  /**
   * Belirli bir operasyonun loading durumunu kontrol eder
   */
  public isOperationLoading(operation: string): Signal<boolean> {
    return computed(() => this._loadingStates()[operation] || false);
  }

  /**
   * Belirli bir operasyonun error durumunu kontrol eder
   */
  public getOperationError(operation: string): Signal<string | null> {
    return computed(() => this._errorStates()[operation] || null);
  }

  /**
   * State'i sıfırlar
   */
  public reset(): void {
    this.setState(this.getInitialState());
    this._loadingStates.set({});
    this._errorStates.set({});
    this.emitEvent('STATE_RESET');
  }

  /**
   * Belirli bir operasyonu temizler
   */
  public clearOperation(operation: string): void {
    this._loadingStates.update(states => {
      const newStates = { ...states };
      delete newStates[operation];
      return newStates;
    });

    this._errorStates.update(states => {
      const newStates = { ...states };
      delete newStates[operation];
      return newStates;
    });
  }

  /**
   * Async operasyon wrapper'ı
   */
  protected async executeAsync<R>(
    operation: () => Promise<R>,
    operationName?: string
  ): Promise<R | null> {
    const opName = operationName || 'async-operation';

    try {
      this.setLoading(true, opName);
      this.setError(null, opName);

      const result = await operation();

      this.emitEvent('OPERATION_SUCCESS', { operation: opName, result });
      return result;
    } catch (error: any) {
      const errorMessage = error?.message || 'Bir hata oluştu';
      this.setError(errorMessage, opName);
      this.emitEvent('OPERATION_ERROR', { operation: opName, error });
      return null;
    } finally {
      this.setLoading(false, opName);
    }
  }

  /**
   * Observable operasyon wrapper'ı
   */
  protected executeObservable<R>(
    operation: () => Observable<R>,
    operationName?: string
  ): Observable<R> {
    const opName = operationName || 'observable-operation';

    return new Observable<R>(subscriber => {
      this.setLoading(true, opName);
      this.setError(null, opName);

      const subscription = operation()
        .pipe(takeUntilDestroyed())
        .subscribe({
          next: (result) => {
            this.emitEvent('OPERATION_SUCCESS', { operation: opName, result });
            subscriber.next(result);
          },
          error: (error) => {
            const errorMessage = error?.message || 'Bir hata oluştu';
            this.setError(errorMessage, opName);
            this.emitEvent('OPERATION_ERROR', { operation: opName, error });
            subscriber.error(error);
          },
          complete: () => {
            this.setLoading(false, opName);
            subscriber.complete();
          }
        });

      return () => {
        subscription.unsubscribe();
        this.setLoading(false, opName);
      };
    });
  }

  /**
   * Event emit eder
   */
  protected emitEvent(type: string, payload?: any): void {
    this._events.next({ type, payload });
  }

  /**
   * Storage'a kaydetme (override edilebilir)
   */
  protected saveToStorage(state: T): void {
    if (this.storageKey) {
      try {
        const stateToSave = this.getStateForStorage(state);
        localStorage.setItem(this.storageKey, JSON.stringify(stateToSave));
      } catch (error) {
        console.warn('Storage save failed:', error);
      }
    }
  }

  /**
   * Storage'dan yükleme (override edilebilir)
   */
  protected loadFromStorage(): Partial<T> | null {
    if (this.storageKey) {
      try {
        const saved = localStorage.getItem(this.storageKey);
        if (saved) {
          return JSON.parse(saved);
        }
      } catch (error) {
        console.warn('Storage load failed:', error);
      }
    }
    return null;
  }

  /**
   * Storage için state'i hazırlar (override edilebilir)
   */
  protected getStateForStorage(state: T): Partial<T> {
    // Varsayılan olarak loading, error gibi durumları kaydetme
    const { loading, error, ...stateToSave } = state as any;
    return stateToSave;
  }

  /**
   * Debug bilgileri
   */
  public getDebugInfo(): {
    state: T;
    loadingStates: LoadingState;
    errorStates: ErrorState;
    hasAutoSave: boolean;
    storageKey: string | null;
  } {
    return {
      state: this._state(),
      loadingStates: this._loadingStates(),
      errorStates: this._errorStates(),
      hasAutoSave: this.enableAutoSave,
      storageKey: this.storageKey
    };
  }

  // Abstract methods
  protected abstract getInitialState(): T;

  // Configurable properties
  protected get enableAutoSave(): boolean { return false; }
  protected get storageKey(): string | null { return null; }
}

/**
 * Pagination özellikli store
 */
@Injectable()
export abstract class BasePaginatedStore<T extends BaseState, ItemType = any> extends BaseStore<T> {
  // Pagination state
  protected readonly _pagination = signal<PaginationState>({
    page: 1,
    pageSize: 10,
    total: 0,
    totalPages: 0
  });

  // Filter state
  protected readonly _filters = signal<FilterState>({});

  // Sort state
  protected readonly _sort = signal<SortState>({
    field: 'createdAt',
    direction: 'desc'
  });

  // Items
  protected readonly _items = signal<ItemType[]>([]);

  // Computed
  public readonly pagination = computed(() => this._pagination());
  public readonly filters = computed(() => this._filters());
  public readonly sort = computed(() => this._sort());
  public readonly items = computed(() => this._items());
  public readonly hasItems = computed(() => this._items().length > 0);
  public readonly isEmpty = computed(() => this._items().length === 0 && !this.isLoading());

  /**
   * Sayfa değiştirir
   */
  public setPage(page: number): void {
    this._pagination.update(p => ({ ...p, page }));
    this.emitEvent('PAGE_CHANGED', page);
  }

  /**
   * Sayfa boyutunu değiştirir
   */
  public setPageSize(pageSize: number): void {
    this._pagination.update(p => ({ ...p, pageSize, page: 1 }));
    this.emitEvent('PAGE_SIZE_CHANGED', pageSize);
  }

  /**
   * Filtreleri ayarlar
   */
  public setFilters(filters: FilterState): void {
    this._filters.set(filters);
    this._pagination.update(p => ({ ...p, page: 1 })); // İlk sayfaya dön
    this.emitEvent('FILTERS_CHANGED', filters);
  }

  /**
   * Tek bir filtreyi günceller
   */
  public updateFilter(key: string, value: any): void {
    this._filters.update(filters => ({ ...filters, [key]: value }));
    this._pagination.update(p => ({ ...p, page: 1 }));
    this.emitEvent('FILTER_UPDATED', { key, value });
  }

  /**
   * Sıralamayı ayarlar
   */
  public setSort(field: string, direction: 'asc' | 'desc'): void {
    this._sort.set({ field, direction });
    this.emitEvent('SORT_CHANGED', { field, direction });
  }

  /**
   * Items'ı ayarlar
   */
  protected setItems(items: ItemType[]): void {
    this._items.set(items);
    this.emitEvent('ITEMS_LOADED', items);
  }

  /**
   * Pagination bilgisini günceller
   */
  protected updatePagination(total: number): void {
    const currentPagination = this._pagination();
    const totalPages = Math.ceil(total / currentPagination.pageSize);

    this._pagination.update(p => ({
      ...p,
      total,
      totalPages
    }));
  }

  /**
   * Tek bir item ekler
   */
  protected addItem(item: ItemType): void {
    this._items.update(items => [...items, item]);
    this.emitEvent('ITEM_ADDED', item);
  }

  /**
   * Item'ı günceller
   */
  protected updateItem(id: string | number, updates: Partial<ItemType>): void {
    this._items.update(items =>
      items.map(item =>
        (item as any).id === id ? { ...item, ...updates } : item
      )
    );
    this.emitEvent('ITEM_UPDATED', { id, updates });
  }

  /**
   * Item'ı siler
   */
  protected removeItem(id: string | number): void {
    this._items.update(items =>
      items.filter(item => (item as any).id !== id)
    );
    this.emitEvent('ITEM_REMOVED', id);
  }

  /**
   * Store'u sıfırlar
   */
  public override reset(): void {
    super.reset();
    this._items.set([]);
    this._pagination.set({
      page: 1,
      pageSize: 10,
      total: 0,
      totalPages: 0
    });
    this._filters.set({});
    this._sort.set({
      field: 'createdAt',
      direction: 'desc'
    });
  }
}