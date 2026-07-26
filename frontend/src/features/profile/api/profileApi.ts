import { apiClient } from '../../../shared/api/baseClient';
import type {
  ChangePasswordInput,
  UpdateProfileInput,
  UserProfile,
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
};
