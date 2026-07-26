export interface PerformanceMetric {
  value: number | null;
  status: string;
  reason: string | null;
}

export interface PerformanceQuality {
  asOf: string | null;
  qualityStatus: string;
  missingSnapshotDays: number;
  staleAssetCount: number;
  unclassifiedCashFlowCount: number;
}

export interface PerformanceSummary {
  currency: string;
  from: string;
  to: string;
  startingNetAssetValue: number;
  endingNetAssetValue: number;
  netExternalFlow: number;
  absoluteReturn: PerformanceMetric;
  timeWeightedReturnPercentage: PerformanceMetric;
  moneyWeightedReturnPercentage: PerformanceMetric;
  realizedPnl: number;
  unrealizedPnl: number;
  totalPnl: number;
  maximumDrawdownPercentage: PerformanceMetric;
  bestMonthPercentage: PerformanceMetric;
  worstMonthPercentage: PerformanceMetric;
  monthlyVolatilityPercentage: PerformanceMetric;
  quality: PerformanceQuality;
}

export interface PerformanceSeriesPoint {
  date: string;
  netAssetValue: number;
  netExternalFlow: number;
  cumulativeExternalFlow: number;
  periodReturnPercentage: number | null;
  growthIndex: number;
  qualityStatus: string;
}

export interface PerformanceSeries {
  currency: string;
  from: string;
  to: string;
  points: PerformanceSeriesPoint[];
  quality: PerformanceQuality;
}

export interface PerformanceDrawdownPoint {
  date: string;
  growthIndex: number;
  peakGrowthIndex: number;
  drawdownPercentage: number;
}

export interface PerformanceDrawdownSeries {
  from: string;
  to: string;
  maximumDrawdownPercentage: PerformanceMetric;
  points: PerformanceDrawdownPoint[];
  quality: PerformanceQuality;
}

export interface PerformanceMonthlyReturn {
  month: string;
  returnPercentage: number | null;
  status: string;
  reason: string | null;
}

export interface PerformanceMonthlyReturns {
  from: string;
  to: string;
  months: PerformanceMonthlyReturn[];
  bestMonthPercentage: PerformanceMetric;
  worstMonthPercentage: PerformanceMetric;
  monthlyVolatilityPercentage: PerformanceMetric;
  quality: PerformanceQuality;
}

export interface BenchmarkDefinition {
  id: string;
  name: string;
  symbol: string;
  marketAssetId: string | null;
  assetGroup: string;
  isDefault: boolean;
  currency: string;
  isActive: boolean;
  pricePointCount: number;
  lastPriceDate: string | null;
}

export interface BenchmarkComparisonPoint {
  date: string;
  portfolioGrowthIndex: number;
  benchmarkGrowthIndex: number | null;
  hasBenchmarkGap: boolean;
}

export interface BenchmarkComparison {
  benchmarkId: string;
  benchmarkName: string;
  benchmarkSymbol: string;
  benchmarkCurrency: string;
  baseDate: string | null;
  points: BenchmarkComparisonPoint[];
  missingBenchmarkDays: number;
  qualityStatus: string;
  portfolioQuality: PerformanceQuality;
}

export interface PerformanceFilters {
  portfolioId?: string;
  assetGroup?: string;
  from?: string;
  to?: string;
  currency?: string;
}
