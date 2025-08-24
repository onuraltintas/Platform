export interface LoginRequest {
  emailOrUsername: string;
  password: string;
  rememberMe?: boolean;
  deviceId?: string;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  confirmPassword: string;
  phoneNumber?: string;
  categories?: string[];
}

export interface AuthResponse {
  success: boolean;
  data?: {
    accessToken: string;
    refreshToken?: string;
    expiresAt: Date;
    tokenType: string;
    user: User;
  };
  error?: any;
  message?: string;
}

export interface User {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phoneNumber?: string;
  avatar?: string;
  isActive: boolean;
  isEmailConfirmed: boolean;
  lastLoginAt?: Date;
  roles: string[];
  categories: string[];
  permissions: string[];
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
  confirmPassword: string;
}

export interface PasswordResetResponse {
  success: boolean;
  message: string;
}

export interface ConfirmEmailRequest {
  userId: string;
  token: string;
}