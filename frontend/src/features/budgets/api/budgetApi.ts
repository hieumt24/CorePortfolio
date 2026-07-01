import { apiClient } from '../../../shared/api/baseClient';

export interface BudgetProgress {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  monthlyLimit: number;
  spentAmount: number;
  progressPercentage: number;
}

export interface SetBudgetRequest {
  categoryId: string;
  monthlyLimit: number;
}

export const getBudgetsProgress = async (): Promise<BudgetProgress[]> => {
  return apiClient<BudgetProgress[]>('/budgets/progress');
};

export const setBudget = async (request: SetBudgetRequest): Promise<{ id: string }> => {
  return apiClient<{ id: string }>('/budgets', {
    method: 'POST',
    body: JSON.stringify(request)
  });
};
