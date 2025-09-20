import { createReducer, on } from '@ngrx/store';
import { AuthActions } from './auth.actions';
import { initialAuthState } from './auth.state';

export const authReducer = createReducer(
  initialAuthState,

  // Login
  on(AuthActions.login, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.loginSuccess, (state, { response }) => ({
    ...state,
    user: response.user,
    isAuthenticated: true,
    isLoading: false,
    error: null,
    tokens: {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken
    },
    permissions: response.user.permissions,
    lastActivity: new Date(),
    loginAttempts: 0,
    isLockedOut: false,
    lockoutEndsAt: null
  })),

  on(AuthActions.loginFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error,
    loginAttempts: state.loginAttempts + 1,
    isLockedOut: state.loginAttempts >= 4, // Lock after 5 attempts
    lockoutEndsAt: state.loginAttempts >= 4 ? new Date(Date.now() + 15 * 60 * 1000) : null // 15 minutes
  })),

  // Google Login
  on(AuthActions.googleLogin, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.googleLoginSuccess, (state, { response }) => ({
    ...state,
    user: response.user,
    isAuthenticated: true,
    isLoading: false,
    error: null,
    tokens: {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken
    },
    permissions: response.user.permissions,
    lastActivity: new Date(),
    loginAttempts: 0,
    isLockedOut: false,
    lockoutEndsAt: null
  })),

  on(AuthActions.googleLoginFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error
  })),

  // Register
  on(AuthActions.register, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.registerSuccess, (state) => ({
    ...state,
    isLoading: false,
    error: null
  })),

  on(AuthActions.registerFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error
  })),

  // Logout
  on(AuthActions.logout, (state) => ({
    ...state,
    isLoading: true
  })),

  on(AuthActions.logoutSuccess, () => ({
    ...initialAuthState
  })),

  on(AuthActions.logoutFailure, () => ({
    ...initialAuthState
  })),

  // Token Refresh
  on(AuthActions.refreshToken, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.refreshTokenSuccess, (state, { response }) => ({
    ...state,
    isLoading: false,
    tokens: {
      accessToken: response.accessToken,
      refreshToken: response.refreshToken
    },
    lastActivity: new Date()
  })),

  on(AuthActions.refreshTokenFailure, () => ({
    ...initialAuthState
  })),

  // Get Current User
  on(AuthActions.getCurrentUser, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.getCurrentUserSuccess, (state, { user }) => ({
    ...state,
    user,
    isAuthenticated: true,
    isLoading: false,
    permissions: user.permissions,
    lastActivity: new Date()
  })),

  on(AuthActions.getCurrentUserFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error
  })),

  // Password Operations
  on(AuthActions.changePassword, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.changePasswordSuccess, (state) => ({
    ...state,
    isLoading: false,
    error: null
  })),

  on(AuthActions.changePasswordFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error
  })),

  on(AuthActions.requestPasswordReset, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.requestPasswordResetSuccess, (state) => ({
    ...state,
    isLoading: false,
    error: null
  })),

  on(AuthActions.requestPasswordResetFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error
  })),

  on(AuthActions.resetPassword, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.resetPasswordSuccess, (state) => ({
    ...state,
    isLoading: false,
    error: null
  })),

  on(AuthActions.resetPasswordFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error
  })),

  // Email Verification
  on(AuthActions.verifyEmail, (state) => ({
    ...state,
    isLoading: true,
    error: null
  })),

  on(AuthActions.verifyEmailSuccess, (state) => ({
    ...state,
    isLoading: false,
    error: null,
    user: state.user ? { ...state.user, emailConfirmed: true } : null
  })),

  on(AuthActions.verifyEmailFailure, (state, { error }) => ({
    ...state,
    isLoading: false,
    error
  })),

  // Session Management
  on(AuthActions.updateLastActivity, (state) => ({
    ...state,
    lastActivity: new Date()
  })),

  on(AuthActions.sessionExpired, () => ({
    ...initialAuthState
  })),

  // Account Lockout
  on(AuthActions.accountLocked, (state, { lockoutEndsAt }) => ({
    ...state,
    isLockedOut: true,
    lockoutEndsAt,
    isLoading: false
  })),

  on(AuthActions.resetLoginAttempts, (state) => ({
    ...state,
    loginAttempts: 0,
    isLockedOut: false,
    lockoutEndsAt: null
  })),

  // Permission Check
  on(AuthActions.checkPermissionSuccess, (state) => ({
    ...state,
    error: null
  })),

  // Clear Actions
  on(AuthActions.clearError, (state) => ({
    ...state,
    error: null
  })),

  on(AuthActions.clearAuthState, () => ({
    ...initialAuthState
  }))
);