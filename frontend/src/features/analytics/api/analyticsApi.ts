import { apiClient } from '../../../shared/api/baseClient';
import type { 
  CashflowMonthlyAnalyticsDto, 
  AssetAllocationDto, 
  PerformanceAnalyticsDto, 
  DividendMonthlyAnalyticsDto,
  TargetAllocationDto,
  TargetAllocationInput,
  RebalanceSuggestionDto
} from '../types';

export const analyticsApi = {
  getCashflowAnalytics: async (months: number = 6, currency: string = 'VND'): Promise<CashflowMonthlyAnalyticsDto[]> => {
    return apiClient<CashflowMonthlyAnalyticsDto[]>(`/analytics/cashflow?months=${months}&currency=${currency}`);
  },

  getAssetAllocation: async (currency: string = 'VND'): Promise<AssetAllocationDto[]> => {
    return apiClient<AssetAllocationDto[]>(`/analytics/allocation?currency=${currency}`);
  },

  getPerformanceAnalytics: async (currency: string = 'VND'): Promise<PerformanceAnalyticsDto> => {
    return apiClient<PerformanceAnalyticsDto>(`/analytics/performance?currency=${currency}`);
  },

  getDividendAnalytics: async (months: number = 12, currency: string = 'VND'): Promise<DividendMonthlyAnalyticsDto[]> => {
    return apiClient<DividendMonthlyAnalyticsDto[]>(`/analytics/dividends?months=${months}&currency=${currency}`);
  },

  getTargetAllocations: async (): Promise<TargetAllocationDto[]> => {
    return apiClient<TargetAllocationDto[]>('/analytics/target-allocations');
  },

  updateTargetAllocations: async (inputs: TargetAllocationInput[]): Promise<boolean> => {
    return apiClient<boolean>('/analytics/target-allocations', {
      method: 'POST',
      body: JSON.stringify(inputs)
    });
  },

  triggerSnapshot: async (): Promise<{ message: string }> => {
    return apiClient<{ message: string }>('/reports/snapshots/trigger', {
      method: 'POST'
    });
  },

  getRebalanceSuggestions: async (currency: string = 'VND'): Promise<RebalanceSuggestionDto[]> => {
    return apiClient<RebalanceSuggestionDto[]>(`/rebalancing/suggestions?currency=${currency}`);
  },

  getCashflowHeatmap: async (): Promise<{ date: string, count: number, totalAmount: number }[]> => {
    return apiClient<{ date: string, count: number, totalAmount: number }[]>('/analytics/heatmap');
  }
};
