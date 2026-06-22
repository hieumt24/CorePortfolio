export interface CashflowMonthlyAnalyticsDto {
  month: string;
  income: number;
  expense: number;
  netFlow: number;
}

export interface AssetAllocationDto {
  categoryName: string;
  totalValue: number;
  percentage: number;
  color: string;
  targetPercentage: number;
  deviation: number;
}

export interface TargetAllocationDto {
  categoryId: string;
  categoryName: string;
  targetPercentage: number;
}

export interface TargetAllocationInput {
  categoryId: string;
  targetPercentage: number;
}

export interface AssetPerformanceDto {
  symbol: string;
  name: string;
  returnPercentage: number;
  returnValue: number;
}

export interface PortfolioHistoryDataPointDto {
  date: string;
  totalValue: number;
}

export interface PerformanceAnalyticsDto {
  topPerformers: AssetPerformanceDto[];
  worstPerformers: AssetPerformanceDto[];
  totalValueHistory: PortfolioHistoryDataPointDto[];
}

export interface DividendMonthlyAnalyticsDto {
  month: string;
  amount: number;
}
