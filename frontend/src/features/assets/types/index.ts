export const AssetType = {
  Crypto: 0,
  Stock: 1,
  MutualFund: 2,
  Cash: 3
} as const;
export type AssetType = typeof AssetType[keyof typeof AssetType];

export interface AssetSummaryDto {
  assetId: string;
  marketAssetId: string;
  symbol: string;
  name: string;
  categoryName: string;
  currency: string;
  currentPrice: number;
  totalQuantity: number;
  totalCost: number;
  currentValue: number;
  totalBought: number;
  averageCost: number;
  realizedPnl: number;
  unrealizedPnl: number;
  fees: number;
  priceUpdatedAt: string;
}

export interface CreateAssetRequest {
  portfolioId: string;
  marketAssetId: string;
}

export interface AvailableMarketAsset {
  id: string;
  portfolioAssetId?: string | null;
  categoryId: string;
  categoryName: string;
  currency: string;
  symbol: string;
  name: string;
  currentPrice: number;
  priceSource: string;
  priceStatus: string;
}
