export interface AssetCategory {
  id: string;
  name: string;
  defaultCurrency: string;
}

export interface CreateCategoryRequest {
  name: string;
  defaultCurrency: string;
}

export interface MarketAsset {
  id: string;
  categoryId: string;
  categoryName: string;
  symbol: string;
  name: string;
  currentPrice: number;
  lastUpdated: string;
  priceSource: 'Manual' | 'DNSE' | 'CoinGecko' | string;
  externalId: string | null;
  priceStatus: 'Manual' | 'Fresh' | 'Stale' | 'Error' | string;
  lastPriceError: string | null;
}

export interface CreateMarketAssetRequest {
  categoryId: string;
  symbol: string;
  name: string;
  currentPrice: number;
  priceSource?: string;
  externalId?: string | null;
}

export interface DnseInstrument {
  symbol: string;
  marketId: string;
  securityGroupId: string;
  shortName: string;
  name: string;
  indexName: string[];
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PriceRefreshResult {
  marketAssetId: string;
  symbol: string;
  status: string;
  price: number | null;
  error: string | null;
}
