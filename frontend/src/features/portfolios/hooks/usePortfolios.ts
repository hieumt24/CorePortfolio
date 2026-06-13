import { useState, useEffect } from 'react';
import { getPortfolios } from '../api/portfolioApi';
import type { PortfolioDto } from '../types';

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
  const [summary, setSummary] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchSummary = async () => {
    if (!id) return;
    try {
      setLoading(true);
      setError(null);
      const { getPortfolioSummary } = await import('../api/portfolioApi');
      const data = await getPortfolioSummary(id);
      setSummary(data);
    } catch (err: any) {
      setError(err.message || 'Failed to load portfolio details');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSummary();
  }, [id]);

  return { summary, loading, error, refetch: fetchSummary };
};
