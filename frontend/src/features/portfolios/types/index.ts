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
}
