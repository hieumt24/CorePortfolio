import { apiClient } from '../../../shared/api/baseClient';
import type { MarketAsset, CreateMarketAssetRequest, PaginatedResult, PriceRefreshResult } from '../types';

export const marketAssetsApi = {
  getMarketAssets: (
    categoryId?: string,
    page = 1,
    pageSize = 10,
    filters?: {
      search?: string;
      priceSource?: string;
      priceStatus?: string;
      sortBy?: string;
      sortDirection?: 'asc' | 'desc';
    },
  ) => {
    let url = `/admin/market-assets?page=${page}&pageSize=${pageSize}`;
    if (categoryId) url += `&categoryId=${categoryId}`;
    if (filters?.search) url += `&search=${encodeURIComponent(filters.search)}`;
    if (filters?.priceSource) url += `&priceSource=${encodeURIComponent(filters.priceSource)}`;
    if (filters?.priceStatus) url += `&priceStatus=${encodeURIComponent(filters.priceStatus)}`;
    if (filters?.sortBy) url += `&sortBy=${encodeURIComponent(filters.sortBy)}`;
    if (filters?.sortDirection) url += `&sortDirection=${filters.sortDirection}`;
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

  refreshPrices: () =>
    apiClient<PriceRefreshResult[]>('/admin/market-assets/refresh', { method: 'POST' }),

  refreshPrice: (id: string) =>
    apiClient<PriceRefreshResult[]>(`/admin/market-assets/${id}/refresh`, { method: 'POST' }),
};
