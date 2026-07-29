import { fireEvent, render } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import type { AnalyticsInsightsDto } from '../types';
import { InsightRail } from './InsightRail';

const insights: AnalyticsInsightsDto = {
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
  methodologyDescription: 'Các quy tắc xác định, không dùng dự báo.',
  disclaimer: 'Không phải khuyến nghị đầu tư.',
  summary: {
    totalCount: 2,
    criticalCount: 0,
    warningCount: 1,
    infoCount: 1,
    positiveCount: 0,
  },
  items: [
    {
      code: 'DRAWDOWN',
      category: 'Risk',
      severity: 'Warning',
      confidence: 'High',
      priority: 78,
      title: 'Rà soát drawdown',
      observation: 'Drawdown là -12%.',
      interpretation: 'Đo mức giảm từ đỉnh.',
      whyItMatters: 'Bổ sung góc nhìn rủi ro.',
      evidence: [{
        key: 'maximumDrawdownPercentage',
        label: 'Drawdown lớn nhất',
        value: -12,
        unit: 'percentagePoints',
        source: 'Performance summary',
      }],
      limitations: ['Không phải dự báo.'],
      action: { label: 'Xem drawdown', href: '/analytics/performance' },
    },
    {
      code: 'RETURN_GAP',
      category: 'Performance',
      severity: 'Info',
      confidence: 'Medium',
      priority: 46,
      title: 'TWR và XIRR khác biệt',
      observation: 'Chênh 6 điểm phần trăm.',
      interpretation: 'Dòng tiền ảnh hưởng lợi suất cá nhân.',
      whyItMatters: 'Cần đọc cả hai chỉ số.',
      evidence: [],
      limitations: [],
      action: null,
    },
  ],
};

describe('InsightRail', () => {
  it('shows methodology, severity and confidence', () => {
    const view = render(<MemoryRouter><InsightRail insights={insights} /></MemoryRouter>);
    expect(view.getByText('rules-v1')).toBeTruthy();
    expect(view.getByText('Cần rà soát')).toBeTruthy();
    expect(view.getByText('Tin cậy cao')).toBeTruthy();
    view.unmount();
  });

  it('filters insight categories without changing the source result', () => {
    const view = render(<MemoryRouter><InsightRail insights={insights} /></MemoryRouter>);
    fireEvent.click(view.getByRole('button', { name: 'Rủi ro' }));
    expect(view.getByText('Rà soát drawdown')).toBeTruthy();
    expect(view.queryByText('TWR và XIRR khác biệt')).toBeNull();
    expect(insights.items).toHaveLength(2);
    view.unmount();
  });

  it('exposes evidence and limitations in an expandable explanation', () => {
    const view = render(<MemoryRouter><InsightRail insights={insights} /></MemoryRouter>);
    const summary = view.getAllByText('Vì sao tín hiệu xuất hiện?')[0];
    fireEvent.click(summary);
    expect(summary.parentElement?.hasAttribute('open')).toBe(true);
    expect(view.getByText('Drawdown lớn nhất')).toBeTruthy();
    expect(view.getByText('Không phải dự báo.')).toBeTruthy();
    view.unmount();
  });
});
