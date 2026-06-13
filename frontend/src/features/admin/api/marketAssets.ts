import { apiClient } from '../../../shared/api/baseClient';
import type { MarketAsset, CreateMarketAssetRequest } from '../types';

export const marketAssetsApi = {
  getMarketAssets: (categoryId?: string) => {
    const url = categoryId ? `/admin/market-assets?categoryId=${categoryId}` : '/admin/market-assets';
    return apiClient<MarketAsset[]>(url, { method: 'GET' });
  },
    
  createMarketAsset: (data: CreateMarketAssetRequest) =>
    apiClient<{ id: string }>('/admin/market-assets', { method: 'POST', body: JSON.stringify(data) }),

  updateMarketAsset: (id: string, data: CreateMarketAssetRequest) =>
    apiClient<void>(`/admin/market-assets/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  deleteMarketAsset: (id: string) =>
    apiClient<void>(`/admin/market-assets/${id}`, { method: 'DELETE' }),
};
