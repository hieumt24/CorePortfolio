import { apiClient } from '../../../shared/api/baseClient';
import type { CreateTransactionRequest, TransactionDto } from '../types';

export const createTransaction = (data: CreateTransactionRequest): Promise<{ id: string }> => {
  return apiClient<{ id: string }>('/transactions', {
    method: 'POST',
    body: JSON.stringify(data),
  });
};

export const getAssetTransactions = (assetId: string): Promise<TransactionDto[]> => {
  return apiClient<TransactionDto[]>(`/assets/${assetId}/transactions`);
};
