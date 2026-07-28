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

const defaultHttpErrorMessages: Record<number, string> = {
  400: 'Dữ liệu gửi lên không hợp lệ.',
  401: 'Phiên đăng nhập không hợp lệ hoặc đã hết hạn.',
  403: 'Bạn không có quyền thực hiện thao tác này.',
  404: 'Không tìm thấy dữ liệu yêu cầu.',
  409: 'Dữ liệu đã thay đổi hoặc đang xung đột. Vui lòng tải lại và thử lại.',
  422: 'Dữ liệu không thể xử lý.',
  429: 'Bạn thao tác quá nhanh. Vui lòng chờ một lúc rồi thử lại.',
  500: 'Hệ thống đang gặp sự cố. Vui lòng thử lại sau.',
  503: 'Dịch vụ tạm thời chưa sẵn sàng. Vui lòng thử lại sau.',
};

const genericProblemTitles = new Set([
  'bad request',
  'unauthorized',
  'forbidden',
  'not found',
  'conflict',
  'unprocessable entity',
  'too many requests',
  'internal server error',
  'service unavailable',
]);

export class ApiError extends Error {
  readonly status: number;

  constructor(
    status: number,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

export const getApiErrorMessage = (
  reason: unknown,
  fallback: string,
  messagesByStatus: Partial<Record<number, string>> = {},
) => {
  if (reason instanceof ApiError) {
    return messagesByStatus[reason.status] ?? reason.message;
  }
  return reason instanceof Error && reason.message.trim()
    ? reason.message
    : fallback;
};

const readProblemMessage = (
  errorBody: unknown,
  status: number,
  statusText: string,
) => {
  const fallback = defaultHttpErrorMessages[status]
    ?? `Yêu cầu không thành công (HTTP ${status}).`;
  if (!errorBody || typeof errorBody !== 'object') return fallback;

  const problem = errorBody as Record<string, unknown>;
  for (const field of ['detail', 'message']) {
    const value = problem[field];
    if (typeof value === 'string' && value.trim()) return value.trim();
  }

  const title = typeof problem.title === 'string' ? problem.title.trim() : '';
  const normalizedTitle = title.toLowerCase();
  if (
    title
    && normalizedTitle !== statusText.trim().toLowerCase()
    && !genericProblemTitles.has(normalizedTitle)
  ) {
    return title;
  }
  return fallback;
};

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
    let errorBody: unknown;

    try {
      errorBody = await response.json();
    } catch {
      // The localized status fallback is used for empty or non-JSON error responses.
    }

    throw new ApiError(
      response.status,
      readProblemMessage(errorBody, response.status, response.statusText),
    );
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
