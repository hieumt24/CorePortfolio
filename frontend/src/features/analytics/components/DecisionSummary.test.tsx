import { render } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { PerformanceSummary } from '../../performance/types';
import { DecisionSummary } from './DecisionSummary';

const metric = { value: 1, status: 'Available', reason: null };

const performance: PerformanceSummary = {
  currency: 'VND',
  from: '2026-07-29',
  to: '2026-07-29',
  startingNetAssetValue: 1_000_000_000,
  endingNetAssetValue: 1_000_000_000,
  netExternalFlow: 0,
  absoluteReturn: metric,
  timeWeightedReturnPercentage: metric,
  moneyWeightedReturnPercentage: metric,
  realizedPnl: 0,
  unrealizedPnl: 0,
  totalPnl: 0,
  maximumDrawdownPercentage: metric,
  bestMonthPercentage: metric,
  worstMonthPercentage: metric,
  monthlyVolatilityPercentage: metric,
  quality: {
    asOf: '2026-07-29T00:00:00Z',
    qualityStatus: 'Complete',
    missingSnapshotDays: 0,
    staleAssetCount: 0,
    unclassifiedCashFlowCount: 0,
  },
};

describe('DecisionSummary', () => {
  it('shows investment holdings as the primary value and labels cash-inclusive NAV', () => {
    const investmentPortfolioValue = 70_000_000;
    const money = new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0,
      notation: 'compact',
    });
    const view = render(
      <DecisionSummary
        performance={performance}
        investmentPortfolioValue={investmentPortfolioValue}
        currency="VND"
      />,
    );

    const primaryCard = view
      .getByText('Giá trị danh mục đầu tư hiện tại')
      .closest('article');
    expect(primaryCard?.querySelector('strong')?.textContent)
      .toBe(money.format(investmentPortfolioValue));
    expect(primaryCard?.querySelector('p')?.textContent).toBe(
      `NAV hiệu suất gồm tiền mặt: ${money.format(performance.endingNetAssetValue)} · Dòng tiền ngoài: ${money.format(performance.netExternalFlow)}`,
    );
  });
});
