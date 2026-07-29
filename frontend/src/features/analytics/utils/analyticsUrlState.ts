export const analyticsPeriods = ['1M', '3M', '6M', 'YTD', '1Y', 'ALL'] as const;
export const analyticsTabs = ['overview', 'performance', 'allocation', 'cashflow', 'scenario'] as const;

export type AnalyticsPeriod = typeof analyticsPeriods[number];
export type AnalyticsTab = typeof analyticsTabs[number];

export interface AnalyticsUrlState {
  period: AnalyticsPeriod;
  tab: AnalyticsTab;
  currency: 'VND' | 'USD';
  portfolioId?: string;
}

const isOneOf = <T extends string>(value: string | null, values: readonly T[]): value is T =>
  Boolean(value && values.includes(value as T));

export const parseAnalyticsUrlState = (params: URLSearchParams): AnalyticsUrlState => {
  const period = params.get('period');
  const tab = params.get('tab');
  const currency = params.get('currency');
  const portfolioId = params.get('portfolioId')?.trim();

  return {
    period: isOneOf(period, analyticsPeriods) ? period : '6M',
    tab: isOneOf(tab, analyticsTabs) ? tab : 'overview',
    currency: currency === 'USD' ? 'USD' : 'VND',
    portfolioId: portfolioId || undefined,
  };
};

export const toAnalyticsSearchParams = (state: AnalyticsUrlState): URLSearchParams => {
  const params = new URLSearchParams({
    period: state.period,
    tab: state.tab,
    currency: state.currency,
  });
  if (state.portfolioId) params.set('portfolioId', state.portfolioId);
  return params;
};

const toDateValue = (date: Date) => date.toISOString().slice(0, 10);

export const resolveAnalyticsDateRange = (
  period: AnalyticsPeriod,
  now: Date = new Date(),
): { from: string; to: string } => {
  const to = new Date(Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate()));
  const from = new Date(to);

  switch (period) {
    case '1M':
      from.setUTCMonth(from.getUTCMonth() - 1);
      break;
    case '3M':
      from.setUTCMonth(from.getUTCMonth() - 3);
      break;
    case '6M':
      from.setUTCMonth(from.getUTCMonth() - 6);
      break;
    case 'YTD':
      from.setUTCMonth(0, 1);
      break;
    case '1Y':
      from.setUTCFullYear(from.getUTCFullYear() - 1);
      break;
    case 'ALL':
      from.setUTCFullYear(from.getUTCFullYear() - 10);
      break;
  }

  return { from: toDateValue(from), to: toDateValue(to) };
};
