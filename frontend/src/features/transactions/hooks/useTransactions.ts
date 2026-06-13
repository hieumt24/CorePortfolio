import { useState, useEffect, useCallback } from 'react';
import type { TransactionDto } from '../types';
import { getAssetTransactions } from '../api/transactionApi';

export const useTransactions = (assetId: string | null) => {
  const [transactions, setTransactions] = useState<TransactionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchTransactions = useCallback(async () => {
    if (!assetId) return;
    try {
      setLoading(true);
      setError(null);
      const data = await getAssetTransactions(assetId);
      setTransactions(data);
    } catch (err: any) {
      setError(err.message || 'Failed to fetch transactions');
    } finally {
      setLoading(false);
    }
  }, [assetId]);

  useEffect(() => {
    fetchTransactions();
  }, [fetchTransactions]);

  return { transactions, loading, error, refetch: fetchTransactions };
};
