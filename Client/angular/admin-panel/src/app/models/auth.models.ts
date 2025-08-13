export interface LoginRequest {
  emailOrUsername: string;
  password: string;
  rememberMe?: boolean;
  deviceId?: string;
}

export interface AuthResponse {
  success: boolean;
  data: {
    accessToken: string;
    refreshToken: string;
    expiresAt: Date;
    tokenType: string;
    user: UserDto;
  };
  error: any;
  message: string;
}

export interface UserDto {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phoneNumber: string | null;
  isEmailConfirmed: boolean;
  isActive: boolean;
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
  message: string;
  success: boolean;
}

export interface EmailConfirmationResponse {
  message: string;
  success: boolean;
}

export interface RegisterRequest {
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  confirmPassword: string;
  phoneNumber: string | null;
  categories?: string[];
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface ConfirmEmailRequest {
  token: string;
  userId: string;
}