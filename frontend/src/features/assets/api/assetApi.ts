import { apiClient } from '../../../shared/api/baseClient';
import type { AvailableMarketAsset, CreateAssetRequest } from '../types';

export const createAsset = (data: CreateAssetRequest): Promise<{ id: string }> => {
  const { portfolioId, ...bodyData } = data;
  return apiClient<{ id: string }>(`/portfolios/${portfolioId}/assets`, {
    method: 'POST',
    body: JSON.stringify(bodyData),
  });
};

export const searchAvailableMarketAssets = (
  portfolioId: string,
  filters?: { search?: string; categoryId?: string; limit?: number }
): Promise<AvailableMarketAsset[]> => {
  const params = new URLSearchParams();
  if (filters?.search) params.set('search', filters.search);
  if (filters?.categoryId) params.set('categoryId', filters.categoryId);
  params.set('limit', String(filters?.limit ?? 20));
  return apiClient<AvailableMarketAsset[]>(
    `/portfolios/${portfolioId}/available-market-assets?${params.toString()}`,
    { method: 'GET' }
  );
};

export const updateAssetPrice = (marketAssetId: string, newPrice: number): Promise<void> => {
  return apiClient<void>(`/market-assets/${marketAssetId}/price`, {
    method: 'PUT',
    body: JSON.stringify({ newPrice }),
  });
};

export const deleteAsset = (portfolioId: string, assetId: string): Promise<void> => {
  return apiClient<void>(`/portfolios/${portfolioId}/assets/${assetId}`, {
    method: 'DELETE',
  });
};
