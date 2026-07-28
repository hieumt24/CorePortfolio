import { apiClient } from '../../../shared/api/baseClient';
import type {
  ChangePasswordInput,
  UpdateProfileInput,
  UserProfile,
  TwoFactorSetup,
  TwoFactorStatus,
} from '../types';

export const profileApi = {
  get: () => apiClient<UserProfile>('/profile'),
  update: (profile: UpdateProfileInput) =>
    apiClient<UserProfile>('/profile', {
      method: 'PUT',
      body: JSON.stringify(profile),
    }),
  changePassword: (passwords: ChangePasswordInput) =>
    apiClient<void>('/profile/password', {
      method: 'PUT',
      body: JSON.stringify(passwords),
    }),
  getTwoFactorStatus: () =>
    apiClient<TwoFactorStatus>('/profile/2fa'),
  beginTwoFactorSetup: (currentPassword: string) =>
    apiClient<TwoFactorSetup>('/profile/2fa/setup', {
      method: 'POST',
      body: JSON.stringify({ currentPassword }),
    }),
  regenerateRecoveryCodes: (currentPassword: string, code: string) =>
    apiClient<{ recoveryCodes: string[] }>('/profile/2fa/recovery-codes', {
      method: 'POST',
      body: JSON.stringify({ currentPassword, code }),
    }),
  disableTwoFactor: (currentPassword: string, code: string) =>
    apiClient<void>('/profile/2fa', {
      method: 'DELETE',
      body: JSON.stringify({ currentPassword, code }),
    }),
};
