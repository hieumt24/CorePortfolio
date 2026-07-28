export interface UserProfile {
  id: string;
  username: string;
  displayName: string;
  email: string | null;
  role: string;
  createdAt: string;
  lastLoginAt: string | null;
}

export interface UpdateProfileInput {
  username: string;
  displayName: string;
  email: string | null;
}

export interface ChangePasswordInput {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export interface TwoFactorStatus {
  isAvailable: boolean;
  isEnabled: boolean;
  isRequired: boolean;
  isPrivilegedRole: boolean;
  enabledAt: string | null;
  recoveryCodesRemaining: number;
}

export interface TwoFactorSetup {
  challengeToken: string;
  provisioningUri: string;
  manualKey: string;
  expiresAt: string;
}
