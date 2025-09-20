export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  profileImageUrl?: string;
  profilePicture?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumberConfirmed: boolean;
  twoFactorEnabled: boolean;
  lockoutEnd?: Date;
  createdAt: Date;
  updatedAt: Date;
  lastLoginAt?: Date;
  roles: Role[];
  permissions: string[];
  groups?: Group[];
}

export interface Role {
  id: string;
  name: string;
  description?: string;
  isSystemRole: boolean;
  permissions: Permission[];
  createdAt: Date;
  updatedAt: Date;
}

export interface Permission {
  id: string;
  name: string;
  resource: string;
  action: string;
  description?: string;
  isSystemPermission: boolean;
  parentId?: string;
  children?: Permission[];
}

export interface Group {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
  memberCount: number;
  createdAt: Date;
  updatedAt: Date;
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
  expiresAt: string;
  tokenType: string;
  user: User;
  permissions: string[];
  roles: string[];
  activeGroup: any;
  availableGroups: any[];
  deviceId?: string;
  isNewDevice: boolean;
  requiresTwoFactor: boolean;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  acceptTerms: boolean;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface GoogleLoginRequest {
  idToken: string;
  deviceId?: string;
  deviceName?: string;
  userAgent?: string;
  ipAddress?: string;
}

export interface TokenPayload {
  sub: string;
  email: string;
  unique_name: string;
  jti: string;
  iat: number;
  exp: number;
  iss: string;
  aud: string;
  permissions?: string[];
  roles?: string[];
}