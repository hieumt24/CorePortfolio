import { apiClient } from '../../../shared/api/baseClient';
import type { MarketAsset, CreateMarketAssetRequest, PaginatedResult } from '../types';

export const marketAssetsApi = {
  getMarketAssets: (categoryId?: string, page = 1, pageSize = 10) => {
    let url = `/admin/market-assets?page=${page}&pageSize=${pageSize}`;
    if (categoryId) url += `&categoryId=${categoryId}`;
    return apiClient<PaginatedResult<MarketAsset>>(url, { method: 'GET' });
  },
    
  createMarketAsset: (data: CreateMarketAssetRequest) =>
    apiClient<{ id: string }>('/admin/market-assets', { method: 'POST', body: JSON.stringify(data) }),

  updateMarketAsset: (id: string, data: CreateMarketAssetRequest) =>
    apiClient<void>(`/admin/market-assets/${id}`, { method: 'PUT', body: JSON.stringify(data) }),

  deleteMarketAsset: (id: string) =>
    apiClient<void>(`/admin/market-assets/${id}`, { method: 'DELETE' }),

  fetchCoinGeckoPrice: (coinId: string) =>
    apiClient<{ price: number }>(`/admin/market-assets/coingecko-price/${coinId}`, { method: 'GET' }),

  fetchDnsePrice: (symbol: string) =>
    apiClient<{ price: number }>(`/admin/market-assets/dnse-price/${symbol}`, { method: 'GET' }),

  searchDnseInstruments: (query: string) =>
    apiClient<import('../types').DnseInstrument[]>(`/admin/market-assets/dnse-instruments?query=${encodeURIComponent(query)}`, { method: 'GET' }),
};
