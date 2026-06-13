export interface PortfolioDto {
  id: string;
  name: string;
  description: string;
  createdAt: string;
}

export interface PortfolioSummaryDto {
  portfolioId: string;
  name: string;
  totalInvested: number;
  currentTotalValue: number;
  assets: AssetSummaryDto[];
}

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
