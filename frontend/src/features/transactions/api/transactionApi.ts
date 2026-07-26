import { apiClient } from '../../../shared/api/baseClient';
import type {
  CreateTransactionRequest,
  UpdateTransactionRequest,
  TransactionDto,
  TransactionAssetGroup,
  TransactionPageResult,
  TransactionSearchFilters,
} from '../types';

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

export const deleteAllTransactions = (assetGroup: TransactionAssetGroup): Promise<{ deletedCount: number }> => {
  const groupValues: Record<TransactionAssetGroup, string> = {
    all: 'All',
    crypto: 'Crypto',
    stock: 'Stock',
    fund: 'Fund',
  };
  return apiClient<{ deletedCount: number }>(`/transactions?assetGroup=${groupValues[assetGroup]}`, {
    method: 'DELETE',
  });
};

export const getAllTransactions = (
  params?: TransactionSearchFilters,
): Promise<TransactionPageResult> => {
  const searchParams = new URLSearchParams();
  if (params?.portfolioId) searchParams.append('portfolioId', params.portfolioId);
  if (params?.assetId) searchParams.append('assetId', params.assetId);
  if (params?.type !== undefined) searchParams.append('type', params.type.toString());
  if (params?.startDate) searchParams.append('startDate', params.startDate);
  if (params?.endDate) searchParams.append('endDate', params.endDate);
  if (params?.search) searchParams.append('search', params.search);
  if (params?.assetGroup && params.assetGroup !== 'all') {
    const groupValues: Record<TransactionAssetGroup, string> = {
      all: 'All',
      crypto: 'Crypto',
      stock: 'Stock',
      fund: 'Fund',
    };
    searchParams.append('assetGroup', groupValues[params.assetGroup]);
  }
  if (params?.minAmount !== undefined) searchParams.append('minAmount', String(params.minAmount));
  if (params?.maxAmount !== undefined) searchParams.append('maxAmount', String(params.maxAmount));
  if (params?.sortBy) searchParams.append('sortBy', params.sortBy);
  if (params?.sortDirection) searchParams.append('sortDirection', params.sortDirection);
  if (params?.page) searchParams.append('page', params.page.toString());
  if (params?.pageSize) searchParams.append('pageSize', params.pageSize.toString());

  const queryString = searchParams.toString() ? `?${searchParams.toString()}` : '';
  return apiClient<TransactionPageResult>(`/transactions${queryString}`);
};
