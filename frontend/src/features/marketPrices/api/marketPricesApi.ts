import { apiClient } from '../../../shared/api/baseClient';

export interface MarketPriceStatus {
  id: string;
  symbol: string;
  priceSource: string;
  currentPrice: number;
  lastUpdated: string;
  priceStatus: 'Fresh' | 'Stale' | 'Error' | 'Manual' | string;
  lastPriceError?: string;
}

export const marketPricesApi = {
  getStatus: () => apiClient<MarketPriceStatus[]>('/market-prices/status'),
};
