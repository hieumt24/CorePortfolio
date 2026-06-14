import { apiClient } from '../../../shared/api/baseClient';
import type { CreateTransactionRequest, UpdateTransactionRequest, TransactionDto, GlobalTransactionDto, PaginatedResult } from '../types';

export const createTransaction = (data: CreateTransactionRequest): Promise<{ id: string }> => {
  return apiClient<{ id: string }>('/transactions', {
    method: 'POST',
    body: JSON.stringify(data),
  });
};

export const getAssetTransactions = (assetId: string): Promise<TransactionDto[]> => {
  return apiClient<TransactionDto[]>(`/assets/${assetId}/transactions`);
};

export const updateTransaction = (id: string, data: UpdateTransactionRequest): Promise<void> => {
  return apiClient<void>(`/transactions/${id}`, {
    method: 'PUT',
    body: JSON.stringify(data),
  });
};

export const deleteTransaction = (id: string): Promise<void> => {
  return apiClient<void>(`/transactions/${id}`, {
    method: 'DELETE',
  });
};

export const getAllTransactions = (params?: {
  portfolioId?: string;
  assetId?: string;
  type?: number;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}): Promise<PaginatedResult<GlobalTransactionDto>> => {
  const searchParams = new URLSearchParams();
  if (params?.portfolioId) searchParams.append('portfolioId', params.portfolioId);
  if (params?.assetId) searchParams.append('assetId', params.assetId);
  if (params?.type !== undefined) searchParams.append('type', params.type.toString());
  if (params?.startDate) searchParams.append('startDate', params.startDate);
  if (params?.endDate) searchParams.append('endDate', params.endDate);
  if (params?.page) searchParams.append('page', params.page.toString());
  if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());

  const queryString = searchParams.toString() ? `?${searchParams.toString()}` : '';
  return apiClient<PaginatedResult<GlobalTransactionDto>>(`/transactions${queryString}`);
};
