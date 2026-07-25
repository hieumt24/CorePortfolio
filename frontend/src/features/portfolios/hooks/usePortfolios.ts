import { useState, useEffect } from 'react';
import { getPortfolioSummary, getPortfolios } from '../api/portfolioApi';
import type { PortfolioDto, PortfolioSummaryDto } from '../types';

export const usePortfolios = () => {
  const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<Error | null>(null);

  const fetchPortfolios = async () => {
    try {
      setLoading(true);
      const data = await getPortfolios();
      setPortfolios(data);
    } catch (err) {
      setError(err as Error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPortfolios();
  }, []);

  return { portfolios, loading, error, refetch: fetchPortfolios };
};

export const usePortfolioSummary = (id: string) => {
  const [summary, setSummary] = useState<PortfolioSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSummary = async () => {
    if (!id) return;
    try {
      setLoading(true);
      setError(null);
      const data = await getPortfolioSummary(id);
      setSummary(data);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Failed to load portfolio details');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSummary();
  }, [id]);

  return { summary, loading, error, refetch: fetchSummary };
};
