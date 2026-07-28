const PRODUCTION_API_URL =
  'https://coreportfolio-api-cdbchffjhtg2hgda.southeastasia-01.azurewebsites.net/api';

const configuredApiUrl = import.meta.env.VITE_API_URL?.trim();
const productionConfiguredApiUrl = configuredApiUrl?.startsWith('http')
  ? configuredApiUrl
  : undefined;

export const API_URL = (
  (import.meta.env.PROD ? productionConfiguredApiUrl : configuredApiUrl)
  || (import.meta.env.PROD ? PRODUCTION_API_URL : '/api')
).replace(/\/+$/, '');

let accessToken: string | null = null;
let refreshPromise: Promise<string | null> | null = null;

export const getAccessToken = () => accessToken;

export const setAccessToken = (token: string | null) => {
  accessToken = token;
};

export const refreshAccessToken = async (): Promise<string | null> => {
  if (refreshPromise) return refreshPromise;
  refreshPromise = (async () => {
    try {
      const response = await fetch(`${API_URL}/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          'X-Requested-With': 'CorePortfolio',
        },
      });
      if (!response.ok) {
        setAccessToken(null);
        return null;
      }
      const result = await response.json() as { token: string };
      setAccessToken(result.token);
      window.dispatchEvent(new CustomEvent('auth:token-refreshed', { detail: result.token }));
      return result.token;
    } catch {
      setAccessToken(null);
      return null;
    } finally {
      refreshPromise = null;
    }
  })();
  return refreshPromise;
};

const sendRequest = (endpoint: string, options?: RequestInit) => {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    'X-Requested-With': 'CorePortfolio',
    ...options?.headers,
  };
  if (accessToken) {
    (headers as Record<string, string>).Authorization = `Bearer ${accessToken}`;
  }
  return fetch(`${API_URL}${endpoint}`, {
    ...options,
    credentials: 'include',
    headers,
  });
};

export const apiClient = async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
  let response = await sendRequest(endpoint, options);
  const isSessionEndpoint = endpoint.startsWith('/auth/login')
    || endpoint.startsWith('/auth/register')
    || endpoint.startsWith('/auth/2fa')
    || endpoint.startsWith('/auth/refresh')
    || endpoint.startsWith('/auth/logout');
  if (response.status === 401 && accessToken && !isSessionEndpoint) {
    const refreshedToken = await refreshAccessToken();
    if (refreshedToken) response = await sendRequest(endpoint, options);
  }

  if (response.status === 401 && !isSessionEndpoint) {
    setAccessToken(null);
    window.dispatchEvent(new CustomEvent('auth:unauthorized'));
  }

  if (!response.ok) {
    let message = `API Error: ${response.statusText}`;

    try {
      const errorBody = await response.json();
      message = errorBody.detail || errorBody.message || errorBody.title || message;
    } catch {
      // Keep the HTTP status text when the server returns an empty or non-JSON error body.
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return {} as T;
  }

  const contentType = response.headers.get('content-type')?.toLowerCase() ?? '';
  if (!contentType.includes('application/json')) {
    const responsePreview = (await response.text()).trim().slice(0, 80).toLowerCase();
    const looksLikeHtml = responsePreview.startsWith('<!doctype')
      || responsePreview.startsWith('<html');
    throw new Error(
      looksLikeHtml
        ? 'API đang trả về trang HTML. Hãy kiểm tra VITE_API_URL hoặc cấu hình proxy /api.'
        : `API trả về định dạng không được hỗ trợ (${contentType || 'unknown'}).`,
    );
  }

  return response.json();
};
