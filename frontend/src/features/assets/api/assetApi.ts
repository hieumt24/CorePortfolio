import { apiClient } from '../../../shared/api/baseClient';
import type { CreateAssetRequest } from '../types';

export const createAsset = (data: CreateAssetRequest): Promise<{ id: string }> => {
  const { portfolioId, ...bodyData } = data;
  return apiClient<{ id: string }>(`/portfolios/${portfolioId}/assets`, {
    method: 'POST',
    body: JSON.stringify(bodyData),
  });
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
