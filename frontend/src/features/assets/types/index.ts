export const AssetType = {
  Crypto: 0,
  Stock: 1,
  MutualFund: 2,
  Cash: 3
} as const;
export type AssetType = typeof AssetType[keyof typeof AssetType];

export interface AssetSummaryDto {
  assetId: string;
  symbol: string;
  name: string;
  type: number; // Enum: 0 = Crypto, 1 = Stock, 2 = MutualFund
  currency: string;
  currentPrice: number;
  totalQuantity: number;
  totalCost: number;
  currentValue: number;
}

export interface CreateAssetRequest {
  portfolioId: string;
  symbol: string;
  name: string;
  type: AssetType;
  currency: string;
}
