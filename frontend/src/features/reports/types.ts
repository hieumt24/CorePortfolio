export interface CategoryAllocationDto {
  categoryName: string;
  currency: string;
  totalInvested: number;
  currentValue: number;
}

export interface PortfolioCurrencyAllocationDto {
  currency: string;
  totalInvested: number;
  currentValue: number;
}

export interface PortfolioAllocationDto {
  portfolioId: string;
  portfolioName: string;
  currencies: PortfolioCurrencyAllocationDto[];
}

export interface GlobalReportDto {
  allocationsByCategory: CategoryAllocationDto[];
  allocationsByPortfolio: PortfolioAllocationDto[];
}

export interface SnapshotDto {
  date: string;
  totalInvested: number;
  totalValue: number;
  holdingsValue: number;
  cashValue: number;
  netAssetValue: number;
  netExternalFlow: number;
  realizedPnl: number;
  unrealizedPnl: number;
  income: number;
  fees: number;
  currency: string;
  usdToVndRate: number;
  valuationTimestamp: string;
  qualityStatus: 'Complete' | 'StalePrices' | 'Partial' | 'Legacy' | string;
  staleAssetCount: number;
  unclassifiedCashFlowCount: number;
}
