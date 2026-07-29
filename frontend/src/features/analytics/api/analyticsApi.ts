import { apiClient } from '../../../shared/api/baseClient';
import type { 
  CashflowMonthlyAnalyticsDto, 
  AssetAllocationDto, 
  PerformanceAnalyticsDto, 
  PerformanceDataQualityDto,
  DividendMonthlyAnalyticsDto,
  TargetAllocationInput,
  TargetAllocationPlanDto,
  RebalanceAssessmentDto,
  AnalyticsOverviewDto,
} from '../types';

export const analyticsApi = {
  getOverview: async (filters: {
    portfolioId?: string;
    from: string;
    to: string;
    currency: string;
  }): Promise<AnalyticsOverviewDto> => {
    const params = new URLSearchParams({
      from: filters.from,
      to: filters.to,
      currency: filters.currency,
    });
    if (filters.portfolioId) params.set('portfolioId', filters.portfolioId);
    return apiClient<AnalyticsOverviewDto>(`/analytics/overview?${params.toString()}`);
  },

  getCashflowAnalytics: async (months: number = 6, currency: string = 'VND'): Promise<CashflowMonthlyAnalyticsDto[]> => {
    return apiClient<CashflowMonthlyAnalyticsDto[]>(`/analytics/cashflow?months=${months}&currency=${currency}`);
  },

  getAssetAllocation: async (currency: string = 'VND'): Promise<AssetAllocationDto[]> => {
    return apiClient<AssetAllocationDto[]>(`/analytics/allocation?currency=${currency}`);
  },

  getPerformanceAnalytics: async (currency: string = 'VND'): Promise<PerformanceAnalyticsDto> => {
    return apiClient<PerformanceAnalyticsDto>(`/analytics/performance?currency=${currency}`);
  },

  getPerformanceDataQuality: async (
    portfolioId?: string,
    from?: string,
    to?: string
  ): Promise<PerformanceDataQualityDto> => {
    const params = new URLSearchParams();
    if (portfolioId) params.set('portfolioId', portfolioId);
    if (from) params.set('from', from);
    if (to) params.set('to', to);
    const query = params.toString();
    return apiClient<PerformanceDataQualityDto>(
      `/performance/data-quality${query ? `?${query}` : ''}`
    );
  },

  getDividendAnalytics: async (months: number = 12, currency: string = 'VND'): Promise<DividendMonthlyAnalyticsDto[]> => {
    return apiClient<DividendMonthlyAnalyticsDto[]>(`/analytics/dividends?months=${months}&currency=${currency}`);
  },

  getTargetAllocations: async (): Promise<TargetAllocationPlanDto> => {
    return apiClient<TargetAllocationPlanDto>('/analytics/target-allocations');
  },

  updateTargetAllocations: async (inputs: TargetAllocationInput[]): Promise<TargetAllocationPlanDto> => {
    return apiClient<TargetAllocationPlanDto>('/analytics/target-allocations', {
      method: 'POST',
      body: JSON.stringify(inputs)
    });
  },

  triggerSnapshot: async (): Promise<{ message: string }> => {
    return apiClient<{ message: string }>('/reports/snapshots/trigger', {
      method: 'POST'
    });
  },

  getRebalanceSuggestions: async (currency: string = 'VND'): Promise<RebalanceAssessmentDto> => {
    return apiClient<RebalanceAssessmentDto>(`/rebalancing/suggestions?currency=${currency}`);
  },

  getCashflowHeatmap: async (): Promise<{ date: string, count: number, totalAmount: number }[]> => {
    return apiClient<{ date: string, count: number, totalAmount: number }[]>('/analytics/heatmap');
  }
};
