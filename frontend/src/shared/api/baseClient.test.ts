import { afterEach, describe, expect, it, vi } from 'vitest';
import {
  ApiError,
  apiClient,
  getApiErrorMessage,
  setAccessToken,
} from './baseClient';

const failedResponse = (
  status: number,
  statusText: string,
  body?: unknown,
) => ({
  ok: false,
  status,
  statusText,
  json: body === undefined
    ? vi.fn().mockRejectedValue(new SyntaxError('Empty response'))
    : vi.fn().mockResolvedValue(body),
}) as unknown as Response;

describe('apiClient errors', () => {
  afterEach(() => {
    setAccessToken(null);
    vi.unstubAllGlobals();
  });

  it('localizes an empty unauthorized response instead of exposing the HTTP status text', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(failedResponse(401, 'Unauthorized')),
    );

    await expect(apiClient('/auth/login', { method: 'POST' })).rejects.toMatchObject({
      name: 'ApiError',
      status: 401,
      message: 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn.',
    });
  });

  it('ignores a generic ProblemDetails title and uses the localized fallback', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(failedResponse(
        401,
        'Unauthorized',
        { title: 'Unauthorized', status: 401 },
      )),
    );

    await expect(apiClient('/auth/2fa/verify', { method: 'POST' })).rejects
      .toHaveProperty('message', 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn.');
  });

  it('supports a message tailored to the active UI step', () => {
    const message = getApiErrorMessage(
      new ApiError(401, 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn.'),
      'Không thể xác minh.',
      { 401: 'Mã xác minh không đúng hoặc phiên xác minh đã hết hạn.' },
    );

    expect(message).toBe('Mã xác minh không đúng hoặc phiên xác minh đã hết hạn.');
  });
});
