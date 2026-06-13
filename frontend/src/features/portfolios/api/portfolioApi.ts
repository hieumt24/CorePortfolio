import { apiClient } from '../../../shared/api/baseClient';
import type { PortfolioDto, PortfolioSummaryDto } from '../types';

export const getPortfolios = (): Promise<PortfolioDto[]> => {
  return apiClient<PortfolioDto[]>('/portfolios');
};

export const getPortfolioSummary = (id: string): Promise<PortfolioSummaryDto> => {
  return apiClient<PortfolioSummaryDto>(`/portfolios/${id}/summary`);
};

export const createPortfolio = (data: { name: string; description: string }): Promise<{ id: string }> => {
  return apiClient<{ id: string }>('/portfolios', {
    method: 'POST',
    body: JSON.stringify(data),
  });
};

export const updatePortfolio = (id: string, data: { name: string; description: string }): Promise<void> => {
  return apiClient<void>(`/portfolios/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
};
