import { fireEvent, render, waitFor } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { analyticsApi } from '../api/analyticsApi';
import type {
  AnalyticsDecisionDto,
  AnalyticsOverviewDto,
} from '../types';
import { DecisionJournal } from './DecisionJournal';

const overview = {
  scope: {
    portfolioId: 'portfolio-1',
    portfolioName: 'Danh mục dài hạn',
    from: '2026-01-01T00:00:00Z',
    to: '2026-07-29T00:00:00Z',
    currency: 'VND',
    financialHealthIsGlobal: true,
  },
  dataQuality: {
    qualityStatus: 'Complete',
  },
} as AnalyticsOverviewDto;

const openDecision: AnalyticsDecisionDto = {
  id: 'decision-1',
  portfolioId: 'portfolio-1',
  portfolioName: 'Danh mục dài hạn',
  decisionType: 'Allocation',
  title: 'Giữ phân bổ trong biên mục tiêu',
  rationale: 'Dữ liệu hiện tại chưa cho thấy sai lệch cần hành động.',
  plannedAction: 'Theo dõi thêm một tháng.',
  riskTriggers: 'Drawdown vượt 15%.',
  reviewDate: '2026-08-29T00:00:00Z',
  status: 'Open',
  reviewOutcome: null,
  reviewNotes: '',
  createdAt: '2026-07-29T00:00:00Z',
  updatedAt: '2026-07-29T00:00:00Z',
  reviewedAt: null,
  isOverdue: false,
  snapshot: {
    from: '2026-01-01T00:00:00Z',
    to: '2026-07-29T00:00:00Z',
    currency: 'VND',
    dataQualityStatus: 'Complete',
    trackedPortfolioValue: 100_000_000,
    timeWeightedReturnPercentage: 5,
    moneyWeightedReturnPercentage: 4.5,
    maximumDrawdownPercentage: -3,
    insightCodes: ['NO_URGENT_SIGNAL'],
    methodologyVersion: 'decision-journal-v1',
  },
};

afterEach(() => {
  vi.restoreAllMocks();
});

describe('DecisionJournal', () => {
  it('creates a journal entry with the active analytics scope', async () => {
    vi.spyOn(analyticsApi, 'getDecisions').mockResolvedValue([]);
    const create = vi.spyOn(analyticsApi, 'createDecision')
      .mockResolvedValue(openDecision);
    const view = render(<DecisionJournal data={overview} />);

    fireEvent.click(view.getByRole('button', { name: 'Ghi quyết định mới' }));
    fireEvent.change(view.getByLabelText('Tiêu đề'), {
      target: { value: 'Theo dõi rủi ro phân bổ' },
    });
    fireEvent.change(view.getByLabelText('Luận điểm và bằng chứng đang dựa vào'), {
      target: { value: 'Tỷ trọng hiện tại vẫn nằm trong biên kiểm soát.' },
    });
    fireEvent.change(view.getByLabelText('Hành động dự kiến'), {
      target: { value: 'Chưa thay đổi danh mục.' },
    });
    fireEvent.click(view.getByRole('button', { name: 'Lưu cùng snapshot' }));

    await waitFor(() => expect(create).toHaveBeenCalledWith(expect.objectContaining({
      portfolioId: 'portfolio-1',
      currency: 'VND',
      title: 'Theo dõi rủi ro phân bổ',
      rationale: 'Tỷ trọng hiện tại vẫn nằm trong biên kiểm soát.',
      plannedAction: 'Chưa thay đổi danh mục.',
    })));
    view.unmount();
  });

  it('reviews an open decision with an explicit outcome and notes', async () => {
    vi.spyOn(analyticsApi, 'getDecisions').mockResolvedValue([openDecision]);
    const review = vi.spyOn(analyticsApi, 'reviewDecision')
      .mockResolvedValue({
        ...openDecision,
        status: 'Reviewed',
        reviewOutcome: 'Adjust',
        reviewNotes: 'Cần giảm mức tập trung.',
        reviewedAt: '2026-08-29T00:00:00Z',
      });
    const view = render(<DecisionJournal data={overview} />);

    fireEvent.click(await view.findByRole('button', { name: 'Review quyết định' }));
    fireEvent.change(view.getByLabelText('Kết quả'), {
      target: { value: 'Adjust' },
    });
    fireEvent.change(
      view.getByLabelText('Điều gì đã xảy ra so với luận điểm ban đầu?'),
      { target: { value: 'Cần giảm mức tập trung.' } },
    );
    fireEvent.click(view.getByRole('button', { name: 'Hoàn tất review' }));

    await waitFor(() => expect(review).toHaveBeenCalledWith('decision-1', {
      outcome: 'Adjust',
      notes: 'Cần giảm mức tập trung.',
    }));
    view.unmount();
  });
});
