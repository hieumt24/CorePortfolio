export const CashflowType = {
  Income: 1,
  Expense: 2,
  Investment: 3,
  Saving: 4,
} as const;

export type CashflowType = typeof CashflowType[keyof typeof CashflowType];

export interface CashflowCategory {
  id: string;
  name: string;
  type: CashflowType;
  icon: string;
  color: string;
  isGlobal: boolean;
  sortOrder: number;
  parentCategoryId: string | null;
  subCategories: CashflowCategory[];
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
  totalInvestment: number;
  totalSaving: number;
  netFlow: number;
  incomeByCategory: CategorySummary[];
  expenseByCategory: CategorySummary[];
  investmentByCategory: CategorySummary[];
  savingByCategory: CategorySummary[];
}

export interface DayCategoryBreakdownDto {
  categoryName: string;
  icon: string;
  color: string;
  amount: number;
}

export interface DaySummaryDto {
  date: string;
  income: number;
  expense: number;
  netFlow: number;
  expenseBreakdown: DayCategoryBreakdownDto[];
}

export interface DailyCashflowSummaryDto {
  days: DaySummaryDto[];
  monthTotalIncome: number;
  monthTotalExpense: number;
  monthNetFlow: number;
  dailyAverage: number;
}

export interface MonthSummaryDto {
  month: number;
  year: number;
  income: number;
  expense: number;
  investment: number;
  saving: number;
  netFlow: number;
}

export interface CategoryTrendDto {
  categoryName: string;
  icon: string;
  color: string;
  monthlyAmounts: number[];
}

export interface MonthlyCashflowReportDto {
  months: MonthSummaryDto[];
  yearTotalIncome: number;
  yearTotalExpense: number;
  yearNetFlow: number;
  categoryTrends: CategoryTrendDto[];
}

export interface CreateCashflowCategoryCommand {
  name: string;
  type: CashflowType;
  icon: string;
  color: string;
  sortOrder: number;
  parentCategoryId?: string | null;
}

export interface CreateCashflowRecordCommand {
  portfolioId: string;
  categoryId: string;
  amount: number;
  currency: string;
  date: string;
  description: string;
}
