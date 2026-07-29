import { useEffect, useMemo, useState } from 'react';
import type { FormEvent } from 'react';
import {
  formatVietnamDate,
  formatVietnamDateTime,
  vietnamTodayIso,
} from '../../../shared/utils/dateTime';
import { analyticsApi } from '../api/analyticsApi';
import type {
  AnalyticsDecisionDto,
  AnalyticsDecisionOutcome,
  AnalyticsDecisionReviewContextDto,
  AnalyticsDecisionReviewMetricDto,
  AnalyticsDecisionStatus,
  AnalyticsDecisionType,
  AnalyticsOverviewDto,
} from '../types';

interface DecisionJournalProps {
  data: AnalyticsOverviewDto;
}

type StatusFilter = 'All' | AnalyticsDecisionStatus;

const decisionTypeLabels: Record<AnalyticsDecisionType, string> = {
  Observation: 'Theo dõi',
  Allocation: 'Phân bổ',
  Cashflow: 'Dòng tiền',
  Risk: 'Rủi ro',
  Goal: 'Mục tiêu',
};

const outcomeLabels: Record<AnalyticsDecisionOutcome, string> = {
  OnTrack: 'Đúng hướng',
  Adjust: 'Cần điều chỉnh',
  Closed: 'Đóng quyết định',
};

const createDefaultReviewDate = () => {
  const date = new Date(`${vietnamTodayIso()}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + 30);
  return date.toISOString().slice(0, 10);
};

const createEmptyForm = () => ({
  decisionType: 'Observation' as AnalyticsDecisionType,
  title: '',
  rationale: '',
  plannedAction: '',
  riskTriggers: '',
  reviewDate: createDefaultReviewDate(),
});

const metric = (value: number | null, suffix = '%') =>
  value === null ? '—' : `${value.toFixed(2)}${suffix}`;

const signedMetric = (value: number | null, suffix = '%') =>
  value === null
    ? '—'
    : `${value > 0 ? '+' : ''}${value.toFixed(2)}${suffix}`;

export const DecisionJournal = ({ data }: DecisionJournalProps) => {
  const [decisions, setDecisions] = useState<AnalyticsDecisionDto[]>([]);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All');
  const [showCreateForm, setShowCreateForm] = useState(false);
  const [form, setForm] = useState(createEmptyForm);
  const [reviewingId, setReviewingId] = useState<string | null>(null);
  const [reviewOutcome, setReviewOutcome] =
    useState<AnalyticsDecisionOutcome>('OnTrack');
  const [reviewNotes, setReviewNotes] = useState('');
  const [reviewContext, setReviewContext] =
    useState<AnalyticsDecisionReviewContextDto | null>(null);
  const [reviewContextLoading, setReviewContextLoading] = useState(false);
  const [reviewContextError, setReviewContextError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const portfolioId = data.scope.portfolioId ?? undefined;
  const scopeKey = `${portfolioId ?? 'all'}:${data.scope.from}:${data.scope.to}:${data.scope.currency}`;
  const counts = useMemo(() => ({
    open: decisions.filter((item) => item.status === 'Open').length,
    overdue: decisions.filter((item) => item.isOverdue).length,
    reviewed: decisions.filter((item) => item.status === 'Reviewed').length,
  }), [decisions]);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    void analyticsApi.getDecisions({
      portfolioId,
      status: statusFilter === 'All' ? undefined : statusFilter,
    })
      .then((result) => {
        if (active) setDecisions(result);
      })
      .catch((reason) => {
        if (active) {
          setError(reason instanceof Error && reason.message
            ? reason.message
            : 'Không thể tải nhật ký quyết định.');
        }
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [portfolioId, reloadKey, statusFilter]);

  useEffect(() => {
    setForm(createEmptyForm());
    setShowCreateForm(false);
    setReviewingId(null);
    setReviewContext(null);
    setReviewContextError(null);
  }, [scopeKey]);

  const loadReviewContext = async (id: string) => {
    try {
      setReviewContextLoading(true);
      setReviewContextError(null);
      setReviewContext(null);
      setReviewContext(await analyticsApi.getDecisionReviewContext(id));
    } catch (reason) {
      setReviewContextError(reason instanceof Error && reason.message
        ? reason.message
        : 'Không thể tải dữ liệu đối chiếu.');
    } finally {
      setReviewContextLoading(false);
    }
  };

  const beginReview = (id: string) => {
    setReviewingId(id);
    setReviewNotes('');
    setReviewOutcome('OnTrack');
    void loadReviewContext(id);
  };

  const createDecision = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    try {
      setSubmitting(true);
      setError(null);
      await analyticsApi.createDecision({
        portfolioId,
        from: data.scope.from.slice(0, 10),
        to: data.scope.to.slice(0, 10),
        currency: data.scope.currency,
        ...form,
      });
      setForm(createEmptyForm());
      setShowCreateForm(false);
      setStatusFilter('All');
      setReloadKey((value) => value + 1);
    } catch (reason) {
      setError(reason instanceof Error && reason.message
        ? reason.message
        : 'Không thể ghi quyết định.');
    } finally {
      setSubmitting(false);
    }
  };

  const reviewDecision = async (
    event: FormEvent<HTMLFormElement>,
    id: string,
  ) => {
    event.preventDefault();
    try {
      setSubmitting(true);
      setError(null);
      await analyticsApi.reviewDecision(id, {
        outcome: reviewOutcome,
        notes: reviewNotes,
      });
      setReviewingId(null);
      setReviewNotes('');
      setReviewOutcome('OnTrack');
      setReviewContext(null);
      setReviewContextError(null);
      setReloadKey((value) => value + 1);
    } catch (reason) {
      setError(reason instanceof Error && reason.message
        ? reason.message
        : 'Không thể hoàn tất review.');
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <section className="analytics-decision-journal" aria-labelledby="decision-journal-title">
      <div className="analytics-panel-heading journal-heading">
        <div>
          <span className="analytics-eyebrow">Decision Journal</span>
          <h2 id="decision-journal-title">Ghi lại luận điểm, rồi quay lại kiểm chứng</h2>
          <p>
            Mỗi bản ghi tự chụp KPI và insight của đúng phạm vi hiện tại; dữ liệu lịch sử không bị thay bằng số liệu mới.
          </p>
        </div>
        <button
          type="button"
          className="analytics-primary-button"
          onClick={() => setShowCreateForm((value) => !value)}
        >
          {showCreateForm ? 'Đóng biểu mẫu' : 'Ghi quyết định mới'}
        </button>
      </div>

      <div className="journal-summary-grid" aria-label="Tổng quan nhật ký">
        <article>
          <span>Đang theo dõi</span>
          <strong>{counts.open}</strong>
        </article>
        <article className={counts.overdue > 0 ? 'is-overdue' : ''}>
          <span>Đã đến hạn</span>
          <strong>{counts.overdue}</strong>
        </article>
        <article>
          <span>Đã review</span>
          <strong>{counts.reviewed}</strong>
        </article>
        <article>
          <span>Phạm vi</span>
          <strong>{data.scope.portfolioName}</strong>
        </article>
      </div>

      {showCreateForm && (
        <form className="journal-create-form" onSubmit={(event) => void createDecision(event)}>
          <div className="journal-form-intro">
            <div>
              <span className="analytics-eyebrow">Bản ghi mới</span>
              <h3>Điều gì đang được quyết định?</h3>
            </div>
            <span>
              Snapshot: {data.dataQuality.qualityStatus} · {data.scope.currency}
            </span>
          </div>
          <div className="journal-form-grid">
            <label>
              <span>Loại quyết định</span>
              <select
                value={form.decisionType}
                onChange={(event) => setForm((current) => ({
                  ...current,
                  decisionType: event.target.value as AnalyticsDecisionType,
                }))}
              >
                {Object.entries(decisionTypeLabels).map(([value, label]) => (
                  <option key={value} value={value}>{label}</option>
                ))}
              </select>
            </label>
            <label>
              <span>Ngày xem lại</span>
              <input
                type="date"
                min={vietnamTodayIso()}
                required
                value={form.reviewDate}
                onChange={(event) => setForm((current) => ({
                  ...current,
                  reviewDate: event.target.value,
                }))}
              />
            </label>
            <label className="is-wide">
              <span>Tiêu đề</span>
              <input
                required
                minLength={3}
                maxLength={120}
                value={form.title}
                placeholder="Ví dụ: Giữ tỷ trọng cổ phiếu trong biên mục tiêu"
                onChange={(event) => setForm((current) => ({
                  ...current,
                  title: event.target.value,
                }))}
              />
            </label>
            <label className="is-wide">
              <span>Luận điểm và bằng chứng đang dựa vào</span>
              <textarea
                required
                minLength={10}
                maxLength={2000}
                value={form.rationale}
                placeholder="Điều gì trong dữ liệu khiến bạn nghiêng về quyết định này?"
                onChange={(event) => setForm((current) => ({
                  ...current,
                  rationale: event.target.value,
                }))}
              />
            </label>
            <label>
              <span>Hành động dự kiến</span>
              <textarea
                required
                minLength={3}
                maxLength={1000}
                value={form.plannedAction}
                placeholder="Theo dõi, điều chỉnh ngân sách hoặc chuẩn bị phương án…"
                onChange={(event) => setForm((current) => ({
                  ...current,
                  plannedAction: event.target.value,
                }))}
              />
            </label>
            <label>
              <span>Điều kiện buộc phải xem xét lại</span>
              <textarea
                maxLength={1000}
                value={form.riskTriggers}
                placeholder="Ví dụ: drawdown vượt ngưỡng hoặc dòng tiền âm 2 tháng"
                onChange={(event) => setForm((current) => ({
                  ...current,
                  riskTriggers: event.target.value,
                }))}
              />
            </label>
          </div>
          <div className="journal-form-footer">
            <p>
              Nhật ký ghi lại quyết định tham khảo; không tạo lệnh hay thay đổi danh mục.
            </p>
            <button type="submit" className="analytics-primary-button" disabled={submitting}>
              {submitting ? 'Đang lưu…' : 'Lưu cùng snapshot'}
            </button>
          </div>
        </form>
      )}

      <div className="journal-toolbar">
        <div role="group" aria-label="Lọc trạng thái nhật ký">
          {(['All', 'Open', 'Reviewed'] as const).map((status) => (
            <button
              key={status}
              type="button"
              className={statusFilter === status ? 'is-active' : ''}
              onClick={() => setStatusFilter(status)}
            >
              {status === 'All' ? 'Tất cả' : status === 'Open' ? 'Đang mở' : 'Đã review'}
            </button>
          ))}
        </div>
        <small>Tối đa 100 bản ghi gần nhất trong phạm vi.</small>
      </div>

      {error && (
        <div className="scenario-error" role="alert">
          <strong>Nhật ký đang gián đoạn</strong>
          <span>{error}</span>
        </div>
      )}

      {loading ? (
        <div className="journal-loading" role="status">Đang tải nhật ký…</div>
      ) : decisions.length === 0 ? (
        <div className="analytics-empty-state journal-empty">
          <strong>Chưa có quyết định trong bộ lọc này</strong>
          <p>Ghi lại một luận điểm để có điểm đối chiếu khách quan khi dữ liệu thay đổi.</p>
        </div>
      ) : (
        <div className="journal-list">
          {decisions.map((decision) => {
            const money = new Intl.NumberFormat(
              decision.snapshot.currency === 'VND' ? 'vi-VN' : 'en-US',
              {
                style: 'currency',
                currency: decision.snapshot.currency,
                maximumFractionDigits: decision.snapshot.currency === 'VND' ? 0 : 2,
              },
            );
            return (
              <article
                key={decision.id}
                className={`journal-card ${decision.isOverdue ? 'is-overdue' : ''}`}
              >
                <div className="journal-card-header">
                  <div>
                    <div className="journal-card-badges">
                      <span>{decisionTypeLabels[decision.decisionType as AnalyticsDecisionType] ?? decision.decisionType}</span>
                      <span className={`is-${decision.status.toLowerCase()}`}>
                        {decision.status === 'Open' ? 'Đang mở' : 'Đã review'}
                      </span>
                      {decision.isOverdue && <span className="is-due">Đến hạn review</span>}
                    </div>
                    <h3>{decision.title}</h3>
                    <small>
                      {decision.portfolioName} · tạo {formatVietnamDateTime(decision.createdAt)}
                    </small>
                  </div>
                  <div className="journal-review-date">
                    <span>Ngày xem lại</span>
                    <strong>{formatVietnamDate(decision.reviewDate)}</strong>
                  </div>
                </div>

                <div className="journal-thesis-grid">
                  <div>
                    <span>Luận điểm</span>
                    <p>{decision.rationale}</p>
                  </div>
                  <div>
                    <span>Hành động dự kiến</span>
                    <p>{decision.plannedAction}</p>
                  </div>
                  {decision.riskTriggers && (
                    <div>
                      <span>Điều kiện xem xét lại</span>
                      <p>{decision.riskTriggers}</p>
                    </div>
                  )}
                </div>

                <details className="journal-snapshot">
                  <summary>Bằng chứng tại thời điểm quyết định</summary>
                  <div className="journal-snapshot-grid">
                    <div>
                      <span>Giá trị theo dõi</span>
                      <strong>{money.format(decision.snapshot.trackedPortfolioValue)}</strong>
                    </div>
                    <div>
                      <span>TWR</span>
                      <strong>{metric(decision.snapshot.timeWeightedReturnPercentage)}</strong>
                    </div>
                    <div>
                      <span>XIRR</span>
                      <strong>{metric(decision.snapshot.moneyWeightedReturnPercentage)}</strong>
                    </div>
                    <div>
                      <span>Drawdown</span>
                      <strong>{metric(decision.snapshot.maximumDrawdownPercentage)}</strong>
                    </div>
                  </div>
                  <p>
                    Chất lượng: <strong>{decision.snapshot.dataQualityStatus}</strong>
                    {' · '}Kỳ {formatVietnamDate(decision.snapshot.from)}–{formatVietnamDate(decision.snapshot.to)}
                  </p>
                  {decision.snapshot.insightCodes.length > 0 && (
                    <div className="journal-insight-codes">
                      {decision.snapshot.insightCodes.map((code) => <code key={code}>{code}</code>)}
                    </div>
                  )}
                  <small>Phương pháp: {decision.snapshot.methodologyVersion}</small>
                </details>

                {decision.status === 'Reviewed' ? (
                  <div className="journal-review-result">
                    <span>
                      {outcomeLabels[decision.reviewOutcome as AnalyticsDecisionOutcome]
                        ?? decision.reviewOutcome}
                    </span>
                    <p>{decision.reviewNotes}</p>
                    <small>Review {formatVietnamDateTime(decision.reviewedAt)}</small>
                  </div>
                ) : reviewingId === decision.id ? (
                  <div className="journal-review-workspace">
                    {reviewContextLoading && (
                      <div className="journal-context-loading" role="status">
                        Đang dựng đối chiếu cùng kỳ…
                      </div>
                    )}
                    {reviewContextError && (
                      <div className="journal-context-error" role="alert">
                        <span>{reviewContextError}</span>
                        <button
                          type="button"
                          onClick={() => void loadReviewContext(decision.id)}
                        >
                          Thử lại
                        </button>
                      </div>
                    )}
                    {reviewContext?.decisionId === decision.id && (
                      <ReviewContext
                        context={reviewContext}
                        currency={decision.snapshot.currency}
                      />
                    )}
                    <form
                      className="journal-review-form"
                      onSubmit={(event) => void reviewDecision(event, decision.id)}
                    >
                      <label>
                        <span>Kết quả</span>
                        <select
                          value={reviewOutcome}
                          onChange={(event) =>
                            setReviewOutcome(event.target.value as AnalyticsDecisionOutcome)}
                        >
                          {Object.entries(outcomeLabels).map(([value, label]) => (
                            <option key={value} value={value}>{label}</option>
                          ))}
                        </select>
                      </label>
                      <label>
                        <span>Điều gì đã xảy ra so với luận điểm ban đầu?</span>
                        <textarea
                          required
                          minLength={3}
                          maxLength={2000}
                          value={reviewNotes}
                          onChange={(event) => setReviewNotes(event.target.value)}
                        />
                      </label>
                      <div>
                        <button
                          type="button"
                          className="analytics-secondary-button"
                          onClick={() => {
                            setReviewingId(null);
                            setReviewContext(null);
                            setReviewContextError(null);
                          }}
                        >
                          Hủy
                        </button>
                        <button
                          type="submit"
                          className="analytics-primary-button"
                          disabled={submitting}
                        >
                          {submitting ? 'Đang lưu…' : 'Hoàn tất review'}
                        </button>
                      </div>
                    </form>
                  </div>
                ) : (
                  <button
                    type="button"
                    className="journal-review-trigger"
                    onClick={() => beginReview(decision.id)}
                  >
                    Review quyết định
                  </button>
                )}
              </article>
            );
          })}
        </div>
      )}
    </section>
  );
};

const ReviewContext = ({
  context,
  currency,
}: {
  context: AnalyticsDecisionReviewContextDto;
  currency: string;
}) => {
  const money = new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  });
  const comparisons: Array<{
    label: string;
    value: AnalyticsDecisionReviewMetricDto;
    formatter: (value: number | null) => string;
  }> = [
    {
      label: 'Giá trị theo dõi',
      value: context.comparison.trackedPortfolioValue,
      formatter: (value) => value === null ? '—' : money.format(value),
    },
    {
      label: 'TWR',
      value: context.comparison.timeWeightedReturnPercentage,
      formatter: metric,
    },
    {
      label: 'XIRR',
      value: context.comparison.moneyWeightedReturnPercentage,
      formatter: metric,
    },
    {
      label: 'Drawdown',
      value: context.comparison.maximumDrawdownPercentage,
      formatter: metric,
    },
  ];

  return (
    <section className="journal-review-context" aria-label="Đối chiếu bằng chứng hiện tại">
      <div className="journal-context-heading">
        <div>
          <span className="analytics-eyebrow">Cùng độ dài kỳ gốc</span>
          <h4>Bằng chứng đã thay đổi thế nào?</h4>
        </div>
        <span className={`is-${context.comparison.readiness.toLowerCase()}`}>
          {context.comparison.readiness === 'Ready'
            ? 'Sẵn sàng đối chiếu'
            : context.comparison.readiness === 'Caution'
              ? 'Cần thận trọng'
              : 'Chưa khả dụng'}
        </span>
      </div>

      {context.reason && <p className="journal-context-reason">{context.reason}</p>}
      <div className="journal-comparison-grid">
        {comparisons.map((item) => (
          <article key={item.label}>
            <span>{item.label}</span>
            <div>
              <small>Ban đầu</small>
              <strong>{item.formatter(item.value.baseline)}</strong>
            </div>
            <i aria-hidden="true">→</i>
            <div>
              <small>Hiện tại</small>
              <strong>{item.formatter(item.value.current)}</strong>
            </div>
            <em className={
              item.value.delta === null
                ? ''
                : item.value.delta < 0 ? 'is-negative' : 'is-positive'
            }>
              Δ {item.label === 'Giá trị theo dõi'
                ? item.value.delta === null ? '—' : money.format(item.value.delta)
                : signedMetric(item.value.delta)}
            </em>
          </article>
        ))}
      </div>

      <div className="journal-insight-diff">
        <InsightDiffGroup
          label="Tín hiệu mới"
          tone="new"
          codes={context.comparison.newInsightCodes}
        />
        <InsightDiffGroup
          label="Đã không còn"
          tone="resolved"
          codes={context.comparison.resolvedInsightCodes}
        />
        <InsightDiffGroup
          label="Vẫn tồn tại"
          tone="persistent"
          codes={context.comparison.persistentInsightCodes}
        />
      </div>
      <p className="journal-context-disclaimer">{context.disclaimer}</p>
    </section>
  );
};

const InsightDiffGroup = ({
  label,
  tone,
  codes,
}: {
  label: string;
  tone: string;
  codes: string[];
}) => (
  <div className={`is-${tone}`}>
    <span>{label}</span>
    {codes.length === 0
      ? <small>Không có</small>
      : <div>{codes.map((code) => <code key={code}>{code}</code>)}</div>}
  </div>
);
