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

export const apiClient = async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
  const token = localStorage.getItem('token');
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
    ...options?.headers,
  };

  if (token) {
    (headers as Record<string, string>)['Authorization'] = `Bearer ${token}`;
  }

  const response = await fetch(`${API_URL}${endpoint}`, {
    ...options,
    headers,
  });

  if (response.status === 401 && token) {
    localStorage.removeItem('token');
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
