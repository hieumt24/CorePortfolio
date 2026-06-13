export const API_URL = 'http://localhost:5211/api';

export const apiClient = async <T>(endpoint: string, options?: RequestInit): Promise<T> => {
  const response = await fetch(`${API_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    throw new Error(`API Error: ${response.statusText}`);
  }

  if (response.status === 204) {
    return {} as T;
  }

  return response.json();
};
