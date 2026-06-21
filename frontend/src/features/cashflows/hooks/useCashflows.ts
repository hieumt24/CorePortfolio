import { useState, useEffect, useCallback } from 'react';
import { cashflowsApi } from '../api/cashflowsApi';
import type { CashflowCategory, CashflowSummary, CashflowRecord, CreateCashflowRecordCommand } from '../types/cashflows';

export const useCashflowCategories = () => {
  const [categories, setCategories] = useState<CashflowCategory[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchCategories = async () => {
    try {
      setLoading(true);
      const data = await cashflowsApi.getCategories();
      setCategories(data);
    } catch (error) {
      console.error('Failed to fetch categories', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCategories();
  }, []);

  return { categories, loading };
};

export const useCashflowSummary = (currency = 'VND', startDate?: string, endDate?: string) => {
  const [summary, setSummary] = useState<CashflowSummary | null>(null);
  const [loading, setLoading] = useState(true);

  const fetchSummary = useCallback(async () => {
    try {
      setLoading(true);
      const data = await cashflowsApi.getSummary(currency, startDate, endDate);
      setSummary(data);
    } catch (error) {
      console.error('Failed to fetch summary', error);
    } finally {
      setLoading(false);
    }
  }, [currency, startDate, endDate]);

  useEffect(() => {
    fetchSummary();
  }, [fetchSummary]);

  return { summary, loading, refetch: fetchSummary };
};

export const useCashflowsList = (page = 1, pageSize = 50, currency = 'VND', startDate?: string, endDate?: string) => {
  const [records, setRecords] = useState<CashflowRecord[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchRecords = useCallback(async () => {
    try {
      setLoading(true);
      const data = await cashflowsApi.getCashflows(page, pageSize, currency, undefined, startDate, endDate);
      setRecords(data);
    } catch (error) {
      console.error('Failed to fetch cashflows', error);
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, currency, startDate, endDate]);

  useEffect(() => {
    fetchRecords();
  }, [fetchRecords]);

  return { records, loading, refetch: fetchRecords };
};

export const useCreateCashflow = () => {
  const [isPending, setIsPending] = useState(false);

  const mutate = async (
    command: CreateCashflowRecordCommand,
    options?: { onSuccess?: () => void; onError?: (err: any) => void }
  ) => {
    try {
      setIsPending(true);
      await cashflowsApi.createCashflow(command);
      options?.onSuccess?.();
    } catch (error) {
      console.error('Failed to create cashflow', error);
      options?.onError?.(error);
    } finally {
      setIsPending(false);
    }
  };

  return { mutate, isPending };
};
