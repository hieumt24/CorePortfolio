import { fireEvent, render, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { analyticsApi } from '../api/analyticsApi';
import type {
  AnalyticsOverviewDto,
  AnalyticsScenarioDto,
} from '../types';
import { ScenarioLab } from './ScenarioLab';

const overview = {
  scope: {
    portfolioId: 'portfolio-1',
    portfolioName: 'Tăng trưởng',
    from: '2026-01-01T00:00:00Z',
    to: '2026-07-29T00:00:00Z',
    currency: 'VND',
    financialHealthIsGlobal: true,
  },
  allocation: [{
    categoryName: 'Stock',
    totalValue: 100_000_000,
    percentage: 100,
    color: '#3b82f6',
    targetPercentage: 100,
    deviation: 0,
  }],
  cashflow: [
    { month: '06/2026', income: 20_000_000, expense: 15_000_000, netFlow: 5_000_000 },
    { month: '07/2026', income: 20_000_000, expense: 17_000_000, netFlow: 3_000_000 },
  ],
} as AnalyticsOverviewDto;

const scenario = {
  scope: overview.scope,
  generatedAt: '2026-07-29T00:00:00Z',
  methodologyVersion: 'scenario-rules-v1',
  confidence: 'High',
  horizonMonths: 12,
  baseline: {
    trackedPortfolioValue: 100_000_000,
    averageMonthlyNetFlow: 4_000_000,
    cashflowSampleMonthCount: 2,
  },
  outcome: {
    stressedPortfolioValue: 90_000_000,
    portfolioValueChange: -10_000_000,
    portfolioValueChangePercentage: -10,
    scenarioMonthlyNetFlow: 4_000_000,
    baselineCumulativeNetFlow: 48_000_000,
    scenarioCumulativeNetFlow: 48_000_000,
    cumulativeNetFlowDifference: 0,
    combinedPlanningDelta: -10_000_000,
    breakEvenMonthlyImprovement: 0,
    worstAffectedCategory: 'Stock',
  },
  allocations: [{
    categoryName: 'Stock',
    currentValue: 100_000_000,
    shockPercentage: -10,
    stressedValue: 90_000_000,
    valueChange: -10_000_000,
    currentPercentage: 100,
    stressedPercentage: 100,
  }],
  assumptions: ['Không giả định lợi suất tương lai.'],
  disclaimer: 'Không phải khuyến nghị đầu tư.',
} satisfies AnalyticsScenarioDto;

afterEach(() => {
  vi.restoreAllMocks();
});

describe('ScenarioLab', () => {
  it('applies a stress preset to every visible category', () => {
    const view = render(<ScenarioLab data={overview} />);

    fireEvent.click(view.getByRole('button', { name: 'Giảm 10%' }));

    expect((view.getByLabelText('Thay đổi giá Stock') as HTMLInputElement).value)
      .toBe('-10');
    view.unmount();
  });

  it('submits the selected scope and renders explainable results', async () => {
    const evaluate = vi.spyOn(analyticsApi, 'evaluateScenario')
      .mockResolvedValue(scenario);
    const view = render(<ScenarioLab data={overview} />);

    fireEvent.click(view.getByRole('button', { name: 'Giảm 10%' }));
    fireEvent.click(view.getByRole('button', { name: 'Chạy mô phỏng' }));

    await waitFor(() => expect(evaluate).toHaveBeenCalledWith(expect.objectContaining({
      portfolioId: 'portfolio-1',
      currency: 'VND',
      horizonMonths: 12,
      shocks: [{ categoryName: 'Stock', changePercentage: -10 }],
    })));
    expect(await view.findByText('Điều gì thay đổi sau 12 tháng?')).toBeTruthy();
    expect(view.getByText('Tin cậy cao')).toBeTruthy();
    expect(view.getByText('Không phải khuyến nghị đầu tư.')).toBeTruthy();
    view.unmount();
  });
});
