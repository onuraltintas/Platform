import { createFeatureSelector, createSelector } from '@ngrx/store';
import { AuthState } from './auth.state';

// Feature Selector
export const selectAuthState = createFeatureSelector<AuthState>('auth');

// Basic Selectors
export const selectCurrentUser = createSelector(
  selectAuthState,
  (state) => state.user
);

export const selectIsAuthenticated = createSelector(
  selectAuthState,
  (state) => state.isAuthenticated
);

export const selectIsLoading = createSelector(
  selectAuthState,
  (state) => state.isLoading
);

export const selectAuthError = createSelector(
  selectAuthState,
  (state) => state.error
);

export const selectTokens = createSelector(
  selectAuthState,
  (state) => state.tokens
);

export const selectAccessToken = createSelector(
  selectTokens,
  (tokens) => tokens.accessToken
);

export const selectRefreshToken = createSelector(
  selectTokens,
  (tokens) => tokens.refreshToken
);

export const selectPermissions = createSelector(
  selectAuthState,
  (state) => state.permissions
);

export const selectLastActivity = createSelector(
  selectAuthState,
  (state) => state.lastActivity
);

export const selectLoginAttempts = createSelector(
  selectAuthState,
  (state) => state.loginAttempts
);

export const selectIsLockedOut = createSelector(
  selectAuthState,
  (state) => state.isLockedOut
);

export const selectLockoutEndsAt = createSelector(
  selectAuthState,
  (state) => state.lockoutEndsAt
);

// Computed Selectors
export const selectUserDisplayName = createSelector(
  selectCurrentUser,
  (user) => user ? `${user.firstName} ${user.lastName}` : ''
);

export const selectUserInitials = createSelector(
  selectCurrentUser,
  (user) => user ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase() : ''
);

export const selectUserRoles = createSelector(
  selectCurrentUser,
  (user) => user?.roles || []
);

export const selectUserRoleNames = createSelector(
  selectUserRoles,
  (roles) => roles.map(role => role.name)
);

export const selectIsEmailVerified = createSelector(
  selectCurrentUser,
  (user) => user?.emailConfirmed || false
);

export const selectIsTwoFactorEnabled = createSelector(
  selectCurrentUser,
  (user) => user?.twoFactorEnabled || false
);

export const selectIsSessionValid = createSelector(
  selectLastActivity,
  (lastActivity) => {
    if (!lastActivity) return false;
    const sessionTimeout = 30 * 60 * 1000; // 30 minutes
    return Date.now() - lastActivity.getTime() < sessionTimeout;
  }
);

export const selectRemainingLockoutTime = createSelector(
  selectLockoutEndsAt,
  (lockoutEndsAt) => {
    if (!lockoutEndsAt) return 0;
    const remaining = lockoutEndsAt.getTime() - Date.now();
    return Math.max(0, Math.floor(remaining / 1000)); // seconds
  }
);

export const selectCanAttemptLogin = createSelector(
  selectIsLockedOut,
  selectRemainingLockoutTime,
  (isLockedOut, remainingTime) => !isLockedOut || remainingTime <= 0
);

// Permission Selectors
export const selectHasPermission = (permission: string) =>
  createSelector(
    selectPermissions,
    (permissions) => permissions.includes(permission)
  );

export const selectHasAnyPermission = (permissions: string[]) =>
  createSelector(
    selectPermissions,
    (userPermissions) => permissions.some(permission => userPermissions.includes(permission))
  );

export const selectHasAllPermissions = (permissions: string[]) =>
  createSelector(
    selectPermissions,
    (userPermissions) => permissions.every(permission => userPermissions.includes(permission))
  );

export const selectHasRole = (roleName: string) =>
  createSelector(
    selectUserRoleNames,
    (roleNames) => roleNames.includes(roleName)
  );

export const selectHasAnyRole = (roleNames: string[]) =>
  createSelector(
    selectUserRoleNames,
    (userRoleNames) => roleNames.some(role => userRoleNames.includes(role))
  );

// Auth State Summary
export const selectAuthSummary = createSelector(
  selectCurrentUser,
  selectIsAuthenticated,
  selectIsLoading,
  selectAuthError,
  selectIsEmailVerified,
  selectIsTwoFactorEnabled,
  selectCanAttemptLogin,
  (user, isAuthenticated, isLoading, error, isEmailVerified, isTwoFactorEnabled, canAttemptLogin) => ({
    user,
    isAuthenticated,
    isLoading,
    error,
    isEmailVerified,
    isTwoFactorEnabled,
    canAttemptLogin
  })
);