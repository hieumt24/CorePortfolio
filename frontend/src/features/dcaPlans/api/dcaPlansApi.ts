import { apiClient } from '../../../shared/api/baseClient';
import type { DcaMarketAsset, DcaPlan, SaveDcaPlanRequest } from '../types';

export const dcaPlansApi = {
  getPlans: () => apiClient<DcaPlan[]>('/dca-plans'),

  getMarketAssets: () => apiClient<DcaMarketAsset[]>('/dca-plans/market-assets'),

  createPlan: (request: SaveDcaPlanRequest) =>
    apiClient<{ id: string }>('/dca-plans', {
      method: 'POST',
      body: JSON.stringify(request),
    }),

  updatePlan: (id: string, request: SaveDcaPlanRequest) =>
    apiClient<void>(`/dca-plans/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    }),

  deletePlan: (id: string) =>
    apiClient<void>(`/dca-plans/${id}`, {
      method: 'DELETE',
    }),
};
