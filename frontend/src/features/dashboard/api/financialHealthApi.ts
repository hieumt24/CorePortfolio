import { apiClient } from '../../../shared/api/baseClient';

export interface FinancialHealth {
  netWorth: number;
  investedValue: number;
  cashBalance: number;
  unrealizedPnl: number;
  monthlyIncome: number;
  monthlyExpense: number;
  monthlyNetFlow: number;
  budgetLimit: number;
  budgetSpent: number;
  budgetProgressPercentage: number;
  portfolioCount: number;
  budgetWarningCount: number;
  budgetExceededCount: number;
  asOf: string;
}

export const financialHealthApi = {
  get: (currency = 'VND') => apiClient<FinancialHealth>(`/dashboard/financial-health?currency=${currency}`),
};
