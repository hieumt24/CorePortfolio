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
}

