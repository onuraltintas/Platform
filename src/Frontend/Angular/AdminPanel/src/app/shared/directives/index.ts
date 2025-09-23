// Directives barrel exports
export * from './permission.directive';
export * from './has-permission.directive';
export * from './has-any-permission.directive';
export * from './has-role.directive';
export * from './permission-disabled.directive';
export * from './debounce.directive';
export * from './click-outside.directive';
export * from './auto-focus.directive';
export * from './copy-to-clipboard.directive';
export * from './tooltip.directive';
export * from './infinite-scroll.directive';
export * from './lazy-load.directive';

// Directive array for easy importing
import { PermissionDirective } from './permission.directive';
import { HasPermissionDirective } from './has-permission.directive';
import { HasAnyPermissionDirective } from './has-any-permission.directive';
import { HasRoleDirective } from './has-role.directive';
import { PermissionDisabledDirective } from './permission-disabled.directive';
import { DebounceDirective } from './debounce.directive';
import { ClickOutsideDirective } from './click-outside.directive';
import { AutoFocusDirective } from './auto-focus.directive';
import { CopyToClipboardDirective } from './copy-to-clipboard.directive';
import { TooltipDirective } from './tooltip.directive';
import { InfiniteScrollDirective } from './infinite-scroll.directive';
import { LazyLoadDirective } from './lazy-load.directive';

export const SHARED_DIRECTIVES = [
  PermissionDirective,
  HasPermissionDirective,
  HasAnyPermissionDirective,
  HasRoleDirective,
  PermissionDisabledDirective,
  DebounceDirective,
  ClickOutsideDirective,
  AutoFocusDirective,
  CopyToClipboardDirective,
  TooltipDirective,
  InfiniteScrollDirective,
  LazyLoadDirective
] as const;

// Permission-specific directives array
export const PERMISSION_DIRECTIVES = [
  HasPermissionDirective,
  HasAnyPermissionDirective,
  HasRoleDirective,
  PermissionDisabledDirective
] as const;