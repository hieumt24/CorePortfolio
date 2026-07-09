export interface SavingGoal {
  id: string;
  portfolioId: string;
  portfolioName: string;
  cashAccountId: string | null;
  cashflowCategoryId: string;
  categoryName: string;
  name: string;
  description: string;
  targetAmount: number;
  currency: string;
  deadline: string;
  createdAt: string;
  isCompleted: boolean;
  cashAccountBalance: number;
  savingCashflowAmount: number;
  currentAmount: number;
  remainingAmount: number;
  progressPercentage: number;
  monthlyRequiredSaving: number;
  daysRemaining: number;
}

export interface SaveSavingGoalRequest {
  portfolioId: string;
  cashAccountId?: string | null;
  cashflowCategoryId: string;
  name: string;
  description: string;
  targetAmount: number;
  currency: string;
  deadline: string;
  isCompleted: boolean;
}
