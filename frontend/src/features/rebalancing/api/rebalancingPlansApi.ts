import { apiClient } from '../../../shared/api/baseClient';
import type { RebalanceExecutionPlan } from '../types';

export const rebalancingPlansApi = {
  getPlans: () => apiClient<RebalanceExecutionPlan[]>('/rebalancing/plans'),

  simulatePlan: (currency: string) =>
    apiClient<RebalanceExecutionPlan>('/rebalancing/plans/simulate', {
      method: 'POST',
      body: JSON.stringify({ currency }),
    }),

  applyPlan: (id: string) =>
    apiClient<void>(`/rebalancing/plans/${id}/apply`, {
      method: 'POST',
    }),
};
