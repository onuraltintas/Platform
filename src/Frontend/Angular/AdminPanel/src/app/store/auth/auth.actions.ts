import { createActionGroup, emptyProps, props } from '@ngrx/store';
import {
  User,
  LoginRequest,
  LoginResponse,
  RegisterRequest,
  RefreshTokenResponse,
  ChangePasswordRequest,
  ResetPasswordRequest,
  GoogleLoginRequest
} from '../../core/auth/models/auth.models';

export const AuthActions = createActionGroup({
  source: 'Auth',
  events: {
    // Login
    'Login': props<{ request: LoginRequest }>(),
    'Login Success': props<{ response: LoginResponse }>(),
    'Login Failure': props<{ error: string }>(),

    // Google Login
    'Google Login': props<{ request: GoogleLoginRequest }>(),
    'Google Login Success': props<{ response: LoginResponse }>(),
    'Google Login Failure': props<{ error: string }>(),

    // Register
    'Register': props<{ request: RegisterRequest }>(),
    'Register Success': props<{ user: User }>(),
    'Register Failure': props<{ error: string }>(),

    // Logout
    'Logout': emptyProps(),
    'Logout Success': emptyProps(),
    'Logout Failure': props<{ error: string }>(),

    // Token Refresh
    'Refresh Token': emptyProps(),
    'Refresh Token Success': props<{ response: RefreshTokenResponse }>(),
    'Refresh Token Failure': props<{ error: string }>(),

    // Get Current User
    'Get Current User': emptyProps(),
    'Get Current User Success': props<{ user: User }>(),
    'Get Current User Failure': props<{ error: string }>(),

    // Password Operations
    'Change Password': props<{ request: ChangePasswordRequest }>(),
    'Change Password Success': emptyProps(),
    'Change Password Failure': props<{ error: string }>(),

    'Request Password Reset': props<{ email: string }>(),
    'Request Password Reset Success': emptyProps(),
    'Request Password Reset Failure': props<{ error: string }>(),

    'Reset Password': props<{ request: ResetPasswordRequest }>(),
    'Reset Password Success': emptyProps(),
    'Reset Password Failure': props<{ error: string }>(),

    // Email Verification
    'Verify Email': props<{ token: string }>(),
    'Verify Email Success': emptyProps(),
    'Verify Email Failure': props<{ error: string }>(),

    'Resend Verification Email': emptyProps(),
    'Resend Verification Email Success': emptyProps(),
    'Resend Verification Email Failure': props<{ error: string }>(),

    // Permission Check
    'Check Permission': props<{ permission: string }>(),
    'Check Permission Success': props<{ permission: string; hasPermission: boolean }>(),
    'Check Permission Failure': props<{ error: string }>(),

    // Session Management
    'Initialize Auth State': emptyProps(),
    'Update Last Activity': emptyProps(),
    'Check Session Validity': emptyProps(),
    'Session Expired': emptyProps(),

    // Account Lockout
    'Account Locked': props<{ lockoutEndsAt: Date }>(),
    'Reset Login Attempts': emptyProps(),

    // Clear Errors
    'Clear Error': emptyProps(),
    'Clear Auth State': emptyProps()
  }
});