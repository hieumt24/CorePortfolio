import { apiClient } from '../../../shared/api/baseClient';

export interface BudgetProgress {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  monthlyLimit: number;
  spentAmount: number;
  remainingAmount: number;
  rawProgressPercentage: number;
  progressPercentage: number;
  isExceeded: boolean;
  alertLevel: 'Healthy' | 'Warning' | 'Exceeded';
  year: number;
  month: number;
}

export interface SetBudgetRequest {
  categoryId: string;
  monthlyLimit: number;
}

export interface BudgetProgressParams {
  year?: number;
  month?: number;
  currency?: string;
}

export const getBudgetsProgress = async (params?: BudgetProgressParams): Promise<BudgetProgress[]> => {
  const searchParams = new URLSearchParams();
  if (params?.year) searchParams.append('year', params.year.toString());
  if (params?.month) searchParams.append('month', params.month.toString());
  if (params?.currency) searchParams.append('currency', params.currency);
  const query = searchParams.toString();
  return apiClient<BudgetProgress[]>(`/budgets/progress${query ? `?${query}` : ''}`);
};

export const setBudget = async (request: SetBudgetRequest): Promise<{ id: string }> => {
  return apiClient<{ id: string }>('/budgets', {
    method: 'POST',
    body: JSON.stringify(request)
  });
};
