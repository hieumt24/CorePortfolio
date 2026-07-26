import { apiClient } from '../../../shared/api/baseClient';
import type {
  BenchmarkComparison,
  BenchmarkDefinition,
  PerformanceDrawdownSeries,
  PerformanceFilters,
  PerformanceMonthlyReturns,
  PerformanceSeries,
  PerformanceSummary,
} from '../types';

const buildQuery = (filters: PerformanceFilters) => {
  const params = new URLSearchParams();
  if (filters.portfolioId) params.set('portfolioId', filters.portfolioId);
  params.set('assetGroup', filters.assetGroup ?? 'All');
  if (filters.from) params.set('from', filters.from);
  if (filters.to) params.set('to', filters.to);
  params.set('currency', filters.currency ?? 'VND');
  return params.toString();
};

export const performanceApi = {
  getSummary: (filters: PerformanceFilters) =>
    apiClient<PerformanceSummary>(`/performance/summary?${buildQuery(filters)}`),

  getSeries: (filters: PerformanceFilters) =>
    apiClient<PerformanceSeries>(`/performance/series?${buildQuery(filters)}`),

  getDrawdowns: (filters: PerformanceFilters) =>
    apiClient<PerformanceDrawdownSeries>(`/performance/drawdowns?${buildQuery(filters)}`),

  getMonthlyReturns: (filters: PerformanceFilters) =>
    apiClient<PerformanceMonthlyReturns>(
      `/performance/monthly-returns?${buildQuery(filters)}`,
    ),

  getBenchmarks: () =>
    apiClient<BenchmarkDefinition[]>('/performance/benchmarks'),

  getBenchmarkComparison: (benchmarkId: string, filters: PerformanceFilters) =>
    apiClient<BenchmarkComparison>(
      `/performance/benchmark?benchmarkId=${encodeURIComponent(benchmarkId)}&${buildQuery(filters)}`,
    ),
};
