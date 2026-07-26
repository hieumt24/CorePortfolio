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
  cashBalances: Array<{ cashAccountId: string; currency: string; balance: number }>;
  realizedPnl: number;
  unrealizedPnl: number;
  fees: number;
  baseCurrency: string;
  asOf: string;
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
  averageCost: number;
  realizedPnl: number;
  unrealizedPnl: number;
  fees: number;
  priceUpdatedAt: string;
}

export interface MarketIndexQuote {
  symbol: 'VNINDEX' | 'VN30';
  name: string;
  value: number;
  change: number;
  changePercent: number;
  asOf: string;
  source: string;
  status: 'Fresh' | 'Stale' | 'Error';
  error?: string | null;
}
