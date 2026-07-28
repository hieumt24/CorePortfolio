import { describe, expect, it } from 'vitest';
import { resolveLoginStage } from '../types/twoFactor';
import type { LoginResponse } from '../types/twoFactor';

const response = (overrides: Partial<LoginResponse>): LoginResponse => ({
  status: 'Authenticated',
  token: 'token',
  expiresAt: null,
  userId: 'user-id',
  username: 'admin',
  displayName: null,
  email: null,
  role: 'Admin',
  challengeToken: null,
  challengeExpiresAt: null,
  recoveryCodes: null,
  ...overrides,
});

describe('resolveLoginStage', () => {
  it('routes mandatory enrollment to setup', () => {
    expect(resolveLoginStage(response({
      status: 'TwoFactorSetupRequired',
      token: null,
      challengeToken: 'challenge',
    }))).toBe('setup');
  });

  it('routes enrolled accounts to verification', () => {
    expect(resolveLoginStage(response({
      status: 'TwoFactorRequired',
      token: null,
      challengeToken: 'challenge',
    }))).toBe('verify');
  });

  it('keeps generated recovery codes visible before entering the app', () => {
    expect(resolveLoginStage(response({
      recoveryCodes: ['AAAA-BBBB-CCCC-DDDD'],
    }))).toBe('recovery');
  });
});
