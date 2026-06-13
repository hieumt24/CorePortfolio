import { apiClient } from '../../../shared/api/baseClient';
import type { CreateTransactionRequest, UpdateTransactionRequest, TransactionDto } from '../types';

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
