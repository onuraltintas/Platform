// Shared module barrel exports

// Components
export * from './components/data-table/data-table.component';
export * from './components/data-table/advanced-data-table.component';
export * from './components/filter-panel/filter-panel.component';
export * from './components/action-button-group/action-button-group.component';
export * from './components/confirmation-modal/confirmation-modal.component';
export * from './components/statistics-card/statistics-card.component';
export * from './components/form-dialog';
export * from './components/loading-overlay';

// Pipes
export * from './pipes';

// Directives
export * from './directives';

// Validators
export * from './validators';

// Convenience imports for commonly used items
import { SHARED_PIPES } from './pipes';
import { SHARED_DIRECTIVES } from './directives';

export const SHARED_IMPORTS = [
  ...SHARED_PIPES,
  ...SHARED_DIRECTIVES
] as const;