import { describe, expect, it } from 'vitest';
import {
  parseAnalyticsUrlState,
  resolveAnalyticsDateRange,
  toAnalyticsSearchParams,
} from './analyticsUrlState';

describe('analytics URL state', () => {
  it('uses safe defaults for unknown values', () => {
    expect(parseAnalyticsUrlState(new URLSearchParams('period=nope&tab=other&currency=EUR')))
      .toEqual({
        period: '6M',
        tab: 'overview',
        currency: 'VND',
        portfolioId: undefined,
      });
  });

  it('round trips the decision scope', () => {
    const state = {
      period: 'YTD' as const,
      tab: 'allocation' as const,
      currency: 'USD' as const,
      portfolioId: 'portfolio-1',
    };
    expect(parseAnalyticsUrlState(toAnalyticsSearchParams(state))).toEqual(state);
  });

  it('keeps the scenario workspace in the URL', () => {
    const state = {
      period: '1Y' as const,
      tab: 'scenario' as const,
      currency: 'VND' as const,
      portfolioId: undefined,
    };

    expect(parseAnalyticsUrlState(toAnalyticsSearchParams(state))).toEqual(state);
  });

  it('keeps the decision journal in the URL', () => {
    const state = {
      period: '6M' as const,
      tab: 'journal' as const,
      currency: 'USD' as const,
      portfolioId: undefined,
    };

    expect(parseAnalyticsUrlState(toAnalyticsSearchParams(state))).toEqual(state);
  });

  it('resolves YTD without depending on local timezone', () => {
    expect(resolveAnalyticsDateRange('YTD', new Date('2026-07-29T20:00:00Z')))
      .toEqual({ from: '2026-01-01', to: '2026-07-29' });
  });

  it('caps all history at the backend supported ten-year range', () => {
    expect(resolveAnalyticsDateRange('ALL', new Date('2026-07-29T00:00:00Z')))
      .toEqual({ from: '2016-07-29', to: '2026-07-29' });
  });
});
