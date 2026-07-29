import { useState } from 'react';
import { fireEvent, render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import type { AnalyticsOverviewDto } from '../types';
import type { AnalyticsTab } from '../utils/analyticsUrlState';
import { AnalyticsWorkspace } from './AnalyticsWorkspace';
import { DataTrustBanner } from './DataTrustBanner';

const overview: AnalyticsOverviewDto = {
  scope: {
    portfolioId: null,
    portfolioName: 'Tất cả danh mục',
    from: '2026-01-01',
    to: '2026-07-29',
    currency: 'VND',
    financialHealthIsGlobal: false,
  },
  performance: {
    currency: 'VND',
    from: '2026-01-01',
    to: '2026-07-29',
    startingNetAssetValue: 100,
    endingNetAssetValue: 110,
    netExternalFlow: 5,
    absoluteReturn: { value: 5, status: 'Available', reason: null },
    timeWeightedReturnPercentage: { value: 4.8, status: 'Available', reason: null },
    moneyWeightedReturnPercentage: { value: 4.4, status: 'Available', reason: null },
    realizedPnl: 3,
    unrealizedPnl: 2,
    totalPnl: 5,
    maximumDrawdownPercentage: { value: -2, status: 'Available', reason: null },
    bestMonthPercentage: { value: 2, status: 'Available', reason: null },
    worstMonthPercentage: { value: -1, status: 'Available', reason: null },
    monthlyVolatilityPercentage: { value: 1.2, status: 'Available', reason: null },
    quality: {
      asOf: '2026-07-29T00:00:00Z',
      qualityStatus: 'Complete',
      missingSnapshotDays: 0,
      staleAssetCount: 0,
      unclassifiedCashFlowCount: 0,
    },
  },
  series: {
    currency: 'VND',
    from: '2026-01-01',
    to: '2026-07-29',
    quality: {
      asOf: '2026-07-29T00:00:00Z',
      qualityStatus: 'Complete',
      missingSnapshotDays: 0,
      staleAssetCount: 0,
      unclassifiedCashFlowCount: 0,
    },
    points: [{
      date: '2026-07-29',
      netAssetValue: 110,
      netExternalFlow: 5,
      cumulativeExternalFlow: 5,
      periodReturnPercentage: 1,
      growthIndex: 104.8,
      qualityStatus: 'Complete',
    }],
  },
  dataQuality: {
    from: '2026-01-01',
    to: '2026-07-29',
    asOf: '2026-07-29T00:00:00Z',
    qualityStatus: 'Complete',
    portfolioCount: 1,
    snapshotCount: 1,
    expectedSnapshotCount: 1,
    missingSnapshotCount: 0,
    missingSnapshotDays: 0,
    missingDates: [],
    staleAssetCount: 0,
    unclassifiedCashFlowCount: 0,
    issues: [],
  },
  financialHealth: {
    netWorth: 110,
    investedValue: 100,
    cashBalance: 10,
    unrealizedPnl: 2,
    monthlyIncome: 20,
    monthlyExpense: 12,
    monthlyNetFlow: 8,
    budgetLimit: 15,
    budgetSpent: 12,
    budgetProgressPercentage: 80,
    portfolioCount: 1,
    budgetWarningCount: 1,
    budgetExceededCount: 0,
    asOf: '2026-07-29T00:00:00Z',
  },
  allocation: [{
    categoryName: 'Stock',
    totalValue: 70,
    percentage: 70,
    color: '#3b82f6',
    targetPercentage: 60,
    deviation: 10,
  }],
  cashflow: [{ month: '07/2026', income: 20, expense: 12, netFlow: 8 }],
  goals: { activeCount: 2, completedCount: 1, atRiskCount: 1, totalRemaining: 30 },
  dca: { activeCount: 1, insufficientCashCount: 0, nextExecutionDate: '2026-08-01' },
  insights: {
    scope: {
      portfolioId: null,
      portfolioName: 'Tất cả danh mục',
      from: '2026-01-01',
      to: '2026-07-29',
      currency: 'VND',
      financialHealthIsGlobal: false,
    },
    generatedAt: '2026-07-29T00:00:00Z',
    methodologyVersion: 'rules-v1',
    methodologyDescription: 'Quy tắc xác định.',
    disclaimer: 'Không phải khuyến nghị đầu tư.',
    summary: {
      totalCount: 1,
      criticalCount: 0,
      warningCount: 0,
      infoCount: 0,
      positiveCount: 1,
    },
    items: [],
  },
  attention: [{
    code: 'NO_URGENT_SIGNAL',
    severity: 'Positive',
    title: 'Ổn định',
    detail: 'Không có cảnh báo.',
    deepLink: null,
  }],
};

const WorkspaceHarness = () => {
  const [tab, setTab] = useState<AnalyticsTab>('overview');
  return (
    <AnalyticsWorkspace
      data={overview}
      activeTab={tab}
      onTabChange={setTab}
      onOpenTargets={() => undefined}
    />
  );
};

describe('AnalyticsWorkspace', () => {
  it('exposes five accessible decision tabs', () => {
    const view = render(<MemoryRouter><WorkspaceHarness /></MemoryRouter>);
    expect(view.getAllByRole('tab')).toHaveLength(5);
    expect(view.getByRole('tab', { name: 'Tổng quan' }).getAttribute('aria-selected')).toBe('true');
    view.unmount();
  });

  it('switches workspace content without losing tab semantics', () => {
    const view = render(<MemoryRouter><WorkspaceHarness /></MemoryRouter>);
    fireEvent.click(view.getByRole('tab', { name: 'Phân bổ' }));
    expect(view.getByRole('tab', { name: 'Phân bổ' }).getAttribute('aria-selected')).toBe('true');
    expect(view.getByRole('heading', { name: 'Khoảng cách so với mục tiêu' })).toBeTruthy();
    view.unmount();
  });

  it('supports arrow-key navigation across tabs', () => {
    const view = render(<MemoryRouter><WorkspaceHarness /></MemoryRouter>);
    fireEvent.keyDown(view.getByRole('tab', { name: 'Tổng quan' }), { key: 'ArrowRight' });
    expect(view.getByRole('tab', { name: 'Hiệu suất' }).getAttribute('aria-selected')).toBe('true');
    view.unmount();
  });

  it('explains partial data instead of presenting it as complete', () => {
    const view = render(<DataTrustBanner quality={{ ...overview.dataQuality, qualityStatus: 'Partial', missingSnapshotDays: 4 }} />);
    expect(view.getByText('Chỉ nên dùng như tín hiệu định hướng')).toBeTruthy();
    expect(view.getByText('4 ngày')).toBeTruthy();
    view.unmount();
  });
});
