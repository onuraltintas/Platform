export interface UserProfileDto {
  id: string;
  userId: string;
  avatar?: string;
  bio?: string;
  dateOfBirth?: string;
  gender?: string;
  address?: string;
  city?: string;
  country?: string;
  postalCode?: string;
  phoneNumber?: string;
  website?: string;
  socialMediaLinks?: string;
  preferences?: string;
}

export interface CreateUserProfileRequest {
  userId: string;
  avatar?: string;
  bio?: string;
  dateOfBirth?: string;
  gender?: string;
  address?: string;
  city?: string;
  country?: string;
  postalCode?: string;
  phoneNumber?: string;
  website?: string;
  socialMediaLinks?: string;
  preferences?: string;
}

export interface UpdateUserProfileRequest extends Partial<CreateUserProfileRequest> {}

export interface UserSettingsDto {
  id: string;
  userId: string;
  theme: string;
  language: string;
  timeZone: string;
  emailNotifications: boolean;
  pushNotifications: boolean;
  smsNotifications: boolean;
  preferences?: string;
}

export interface UpdateUserSettingsRequest {
  theme?: string;
  language?: string;
  timeZone?: string;
  emailNotifications?: boolean;
  pushNotifications?: boolean;
  smsNotifications?: boolean;
  preferences?: string;
}
