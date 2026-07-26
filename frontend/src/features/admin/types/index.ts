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
  priceSource: 'Manual' | 'KBS' | 'CoinGecko' | string;
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

export interface KbsInstrument {
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

export interface SyncVn100Result {
  providerCount: number;
  created: number;
  updated: number;
  unchanged: number;
  withReferencePrice: number;
}

export interface AdminOverview {
  totalUsers: number;
  activeUsers: number;
  adminUsers: number;
  totalPortfolios: number;
  totalAssets: number;
  totalTransactions: number;
  totalCashflows: number;
  totalMarketAssets: number;
  marketAssetsNeedingAttention: number;
  generatedAt: string;
}

export interface AdminUser {
  id: string;
  username: string;
  role: 'Admin' | 'User';
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  portfolioCount: number;
  transactionCount: number;
}

export interface AdminUserFilters {
  search?: string;
  role?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}
