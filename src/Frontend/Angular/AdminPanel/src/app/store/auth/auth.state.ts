import { User } from '../../core/auth/models/auth.models';

export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  tokens: {
    accessToken: string | null;
    refreshToken: string | null;
  };
  permissions: string[];
  lastActivity: Date | null;
  loginAttempts: number;
  isLockedOut: boolean;
  lockoutEndsAt: Date | null;
}

export const initialAuthState: AuthState = {
  user: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
  tokens: {
    accessToken: null,
    refreshToken: null
  },
  permissions: [],
  lastActivity: null,
  loginAttempts: 0,
  isLockedOut: false,
  lockoutEndsAt: null
};