import { apiClient } from '../../../shared/api/baseClient';
import type {
  CashflowCategory,
  CashflowRecord,
  CashflowSummary,
  CreateCashflowCategoryCommand,
  CreateCashflowRecordCommand,
} from '../types/cashflows';

export const cashflowsApi = {
  getCategories: (): Promise<CashflowCategory[]> => {
    return apiClient<CashflowCategory[]>('/cashflows/categories');
  },

  createCategory: (command: CreateCashflowCategoryCommand & { isGlobal?: boolean }): Promise<string> => {
    return apiClient<string>('/cashflows/categories', {
      method: 'POST',
      body: JSON.stringify(command)
    });
  },

  updateCategory: (id: string, command: CreateCashflowCategoryCommand): Promise<void> => {
    return apiClient<void>(`/cashflows/categories/${id}`, {
      method: 'PUT',
      body: JSON.stringify({ ...command, id })
    });
  },

  deleteCategory: (id: string): Promise<void> => {
    return apiClient<void>(`/cashflows/categories/${id}`, {
      method: 'DELETE'
    });
  },

  getCashflows: (
    page = 1,
    pageSize = 50,
    currency?: string,
    type?: number,
    startDate?: string,
    endDate?: string
  ): Promise<CashflowRecord[]> => {
    const params = new URLSearchParams({
      page: page.toString(),
      pageSize: pageSize.toString(),
    });
    if (currency) params.append('currency', currency);
    if (type) params.append('type', type.toString());
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    return apiClient<CashflowRecord[]>(`/cashflows?${params.toString()}`);
  },

  createCashflow: (command: CreateCashflowRecordCommand): Promise<string> => {
    return apiClient<string>('/cashflows', {
      method: 'POST',
      body: JSON.stringify(command)
    });
  },

  getSummary: (currency = 'VND', startDate?: string, endDate?: string): Promise<CashflowSummary> => {
    const params = new URLSearchParams({ currency });
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    return apiClient<CashflowSummary>(`/cashflows/summary?${params.toString()}`);
  },
};
