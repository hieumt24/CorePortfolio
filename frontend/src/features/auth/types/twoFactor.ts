export type LoginStatus =
  | 'Authenticated'
  | 'TwoFactorRequired'
  | 'TwoFactorSetupRequired';

export interface LoginResponse {
  status: LoginStatus;
  token: string | null;
  expiresAt: string | null;
  userId: string;
  username: string;
  displayName: string | null;
  email: string | null;
  role: string;
  challengeToken: string | null;
  challengeExpiresAt: string | null;
  recoveryCodes: string[] | null;
}

export interface TwoFactorSetupResponse {
  challengeToken: string;
  provisioningUri: string;
  manualKey: string;
  expiresAt: string;
}

export type LoginFlowStage = 'credentials' | 'setup' | 'verify' | 'recovery';

export const resolveLoginStage = (response: LoginResponse): LoginFlowStage => {
  if (response.status === 'TwoFactorSetupRequired') return 'setup';
  if (response.status === 'TwoFactorRequired') return 'verify';
  return response.recoveryCodes?.length ? 'recovery' : 'credentials';
};
