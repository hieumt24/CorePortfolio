import { apiClient } from '../../../shared/api/baseClient';
import type { LoginResponse, TwoFactorSetupResponse } from '../types/twoFactor';

export const authApi = {
  login: (username: string, password: string) =>
    apiClient<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ username, password }),
    }),
  beginTwoFactorSetup: (challengeToken: string) =>
    apiClient<TwoFactorSetupResponse>('/auth/2fa/setup', {
      method: 'POST',
      body: JSON.stringify({ challengeToken }),
    }),
  verifyTwoFactor: (
    challengeToken: string,
    input: { code?: string; recoveryCode?: string },
  ) =>
    apiClient<LoginResponse>('/auth/2fa/verify', {
      method: 'POST',
      body: JSON.stringify({ challengeToken, ...input }),
    }),
};
