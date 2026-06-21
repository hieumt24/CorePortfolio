export enum CashflowType {
  Income = 1,
  Expense = 2,
}

export interface CashflowCategory {
  id: string;
  name: string;
  type: CashflowType;
  icon: string;
  color: string;
  isGlobal: boolean;
}

export interface CashflowRecord {
  id: string;
  portfolioId: string;
  portfolioName: string;
  categoryId: string;
  categoryName: string;
  categoryIcon: string;
  categoryColor: string;
  type: CashflowType;
  amount: number;
  currency: string;
  date: string;
  description: string;
}

export interface CategorySummary {
  categoryName: string;
  icon: string;
  color: string;
  amount: number;
}

export interface CashflowSummary {
  totalIncome: number;
  totalExpense: number;
  netFlow: number;
  incomeByCategory: CategorySummary[];
  expenseByCategory: CategorySummary[];
}

export interface CreateCashflowCategoryCommand {
  name: string;
  type: CashflowType;
  icon: string;
  color: string;
}

export interface CreateCashflowRecordCommand {
  portfolioId: string;
  categoryId: string;
  amount: number;
  currency: string;
  date: string;
  description: string;
}
