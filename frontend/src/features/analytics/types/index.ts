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

export interface TargetAllocationPlanDto {
  allocations: TargetAllocationDto[];
  totalPercentage: number;
  status: 'NotConfigured' | 'Complete' | 'Invalid' | string;
  isActionable: boolean;
  reason: string | null;
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

export interface PerformanceDataQualityDto {
  from: string;
  to: string;
  asOf: string | null;
  qualityStatus: 'Complete' | 'StalePrices' | 'Partial' | 'Unavailable' | string;
  portfolioCount: number;
  snapshotCount: number;
  expectedSnapshotCount: number;
  missingSnapshotCount: number;
  missingSnapshotDays: number;
  missingDates: string[];
  staleAssetCount: number;
  unclassifiedCashFlowCount: number;
  issues: string[];
}

export interface DividendMonthlyAnalyticsDto {
  month: string;
  amount: number;
}

export interface RebalanceSuggestionDto {
  categoryId: string;
  categoryName: string;
  currentValue: number;
  targetValue: number;
  differenceValue: number;
  action: string;
}

export interface RebalanceAssessmentDto {
  targetPlanStatus: 'NotConfigured' | 'Complete' | 'Invalid' | string;
  totalTargetPercentage: number;
  tolerancePercentagePoints: number;
  isActionable: boolean;
  reason: string | null;
  suggestions: RebalanceSuggestionDto[];
}
