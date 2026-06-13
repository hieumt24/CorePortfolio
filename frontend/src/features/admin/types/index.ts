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
}

export interface CreateMarketAssetRequest {
  categoryId: string;
  symbol: string;
  name: string;
  currentPrice: number;
}
