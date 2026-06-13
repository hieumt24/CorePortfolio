import { apiClient } from '../../../shared/api/baseClient';
import type { CreateAssetRequest } from '../types';

export const createAsset = (data: CreateAssetRequest): Promise<{ id: string }> => {
  const { portfolioId, ...bodyData } = data;
  return apiClient<{ id: string }>(`/portfolios/${portfolioId}/assets`, {
    method: 'POST',
    body: JSON.stringify(bodyData),
  });
};
