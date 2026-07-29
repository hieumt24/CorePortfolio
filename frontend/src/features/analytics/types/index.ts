import type {
  PerformanceSeries,
  PerformanceSummary,
} from '../../performance/types';

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

export interface AnalyticsScopeDto {
  portfolioId: string | null;
  portfolioName: string;
  from: string;
  to: string;
  currency: string;
  financialHealthIsGlobal: boolean;
}

export interface FinancialHealthDto {
  netWorth: number;
  investedValue: number;
  cashBalance: number;
  unrealizedPnl: number;
  monthlyIncome: number;
  monthlyExpense: number;
  monthlyNetFlow: number;
  budgetLimit: number;
  budgetSpent: number;
  budgetProgressPercentage: number;
  portfolioCount: number;
  budgetWarningCount: number;
  budgetExceededCount: number;
  asOf: string;
}

export interface AnalyticsGoalSummaryDto {
  activeCount: number;
  completedCount: number;
  atRiskCount: number;
  totalRemaining: number;
}

export interface AnalyticsDcaSummaryDto {
  activeCount: number;
  insufficientCashCount: number;
  nextExecutionDate: string | null;
}

export interface AnalyticsAttentionDto {
  code: string;
  severity: 'Critical' | 'Warning' | 'Info' | 'Positive' | string;
  title: string;
  detail: string;
  deepLink: string | null;
}

export interface AnalyticsInsightEvidenceDto {
  key: string;
  label: string;
  value: number;
  unit: string;
  source: string;
}

export interface AnalyticsInsightActionDto {
  label: string;
  href: string;
}

export interface AnalyticsInsightDto {
  code: string;
  category: 'DataQuality' | 'Risk' | 'Allocation' | 'Cashflow' | 'Goals' | 'Performance' | 'General' | string;
  severity: 'Critical' | 'Warning' | 'Info' | 'Positive' | string;
  confidence: 'High' | 'Medium' | 'Low' | string;
  priority: number;
  title: string;
  observation: string;
  interpretation: string;
  whyItMatters: string;
  evidence: AnalyticsInsightEvidenceDto[];
  limitations: string[];
  action: AnalyticsInsightActionDto | null;
}

export interface AnalyticsInsightsDto {
  scope: AnalyticsScopeDto;
  generatedAt: string;
  methodologyVersion: string;
  methodologyDescription: string;
  disclaimer: string;
  summary: {
    totalCount: number;
    criticalCount: number;
    warningCount: number;
    infoCount: number;
    positiveCount: number;
  };
  items: AnalyticsInsightDto[];
}

export interface AnalyticsOverviewDto {
  scope: AnalyticsScopeDto;
  performance: PerformanceSummary;
  series: PerformanceSeries;
  dataQuality: PerformanceDataQualityDto;
  financialHealth: FinancialHealthDto;
  allocation: AssetAllocationDto[];
  cashflow: CashflowMonthlyAnalyticsDto[];
  goals: AnalyticsGoalSummaryDto;
  dca: AnalyticsDcaSummaryDto;
  insights: AnalyticsInsightsDto;
  attention: AnalyticsAttentionDto[];
}
