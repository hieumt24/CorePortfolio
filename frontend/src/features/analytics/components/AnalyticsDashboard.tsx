import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useNotification } from '../../../context/NotificationContext';
import { getPortfolios } from '../../portfolios/api/portfolioApi';
import type { PortfolioDto } from '../../portfolios/types';
import { DashboardSkeleton } from '../../../shared/components/Skeleton';
import { analyticsApi } from '../api/analyticsApi';
import type { AnalyticsOverviewDto } from '../types';
import {
  parseAnalyticsUrlState,
  resolveAnalyticsDateRange,
  toAnalyticsSearchParams,
  type AnalyticsTab,
  type AnalyticsUrlState,
} from '../utils/analyticsUrlState';
import { AnalyticsFilterBar } from './AnalyticsFilterBar';
import { AnalyticsWorkspace } from './AnalyticsWorkspace';
import { DataTrustBanner } from './DataTrustBanner';
import { DecisionSummary } from './DecisionSummary';
import { InsightRail } from './InsightRail';
import { TargetAllocationModal } from './TargetAllocationModal';
import './AnalyticsDashboard.css';

const getErrorMessage = (error: unknown) =>
  error instanceof Error && error.message
    ? error.message
    : 'Không thể tải báo cáo phân tích. Vui lòng thử lại.';

export const AnalyticsDashboard = () => {
  const { showNotification } = useNotification();
  const [searchParams, setSearchParams] = useSearchParams();
  const urlState = useMemo(() => parseAnalyticsUrlState(searchParams), [searchParams]);
  const dateRange = useMemo(
    () => resolveAnalyticsDateRange(urlState.period),
    [urlState.period],
  );
  const [overview, setOverview] = useState<AnalyticsOverviewDto | null>(null);
  const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);
  const [snapshotLoading, setSnapshotLoading] = useState(false);
  const [targetModalOpen, setTargetModalOpen] = useState(false);

  const updateUrlState = useCallback((patch: Partial<AnalyticsUrlState>) => {
    setSearchParams(toAnalyticsSearchParams({ ...urlState, ...patch }), { replace: true });
  }, [setSearchParams, urlState]);

  useEffect(() => {
    let active = true;
    void getPortfolios()
      .then((result) => {
        if (active) setPortfolios(result);
      })
      .catch(() => {
        if (active) setPortfolios([]);
      });
    return () => {
      active = false;
    };
  }, []);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    void analyticsApi.getOverview({
      portfolioId: urlState.portfolioId,
      currency: urlState.currency,
      ...dateRange,
    })
      .then((result) => {
        if (active) setOverview(result);
      })
      .catch((reason) => {
        if (active) setError(getErrorMessage(reason));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [
    dateRange.from,
    dateRange.to,
    reloadKey,
    urlState.currency,
    urlState.portfolioId,
  ]);

  const refreshOverview = () => setReloadKey((value) => value + 1);

  const handleSnapshot = async () => {
    try {
      setSnapshotLoading(true);
      await analyticsApi.triggerSnapshot();
      showNotification('Đã cập nhật snapshot mới nhất.', 'success');
      refreshOverview();
    } catch (reason) {
      showNotification(getErrorMessage(reason), 'error');
    } finally {
      setSnapshotLoading(false);
    }
  };

  return (
    <main className="analytics-decision-page">
      <header className="analytics-page-header">
        <div>
          <span className="analytics-eyebrow">Báo cáo phân tích</span>
          <h1>Ra quyết định từ dữ liệu đã kiểm chứng.</h1>
          <p>
            Đọc hiệu suất, rủi ro, phân bổ và khả năng tài trợ kế hoạch trong cùng một phạm vi.
          </p>
        </div>
        <div className="analytics-header-actions">
          <Link to="/analytics/performance" className="analytics-secondary-button">
            Phân tích chuyên sâu
          </Link>
          <button
            type="button"
            className="analytics-primary-button"
            disabled={snapshotLoading}
            onClick={handleSnapshot}
          >
            {snapshotLoading ? 'Đang cập nhật…' : 'Cập nhật snapshot'}
          </button>
        </div>
      </header>

      <AnalyticsFilterBar
        state={urlState}
        portfolios={portfolios}
        disabled={loading}
        onChange={updateUrlState}
      />

      {loading && !overview ? <DashboardSkeleton /> : error ? (
        <section className="analytics-page-error" role="alert">
          <span className="analytics-eyebrow">Không tải được workspace</span>
          <h2>Dữ liệu phân tích đang tạm gián đoạn</h2>
          <p>{error}</p>
          <button type="button" onClick={refreshOverview}>Thử lại</button>
        </section>
      ) : overview ? (
        <>
          <DataTrustBanner quality={overview.dataQuality} />
          <DecisionSummary performance={overview.performance} currency={overview.scope.currency} />
          <div className="analytics-decision-layout">
            <AnalyticsWorkspace
              data={overview}
              activeTab={urlState.tab}
              onTabChange={(tab: AnalyticsTab) => updateUrlState({ tab })}
              onOpenTargets={() => setTargetModalOpen(true)}
            />
            <InsightRail insights={overview.insights} />
          </div>
          {loading && <div className="analytics-refresh-indicator" role="status">Đang đổi phạm vi…</div>}
        </>
      ) : null}

      <TargetAllocationModal
        isOpen={targetModalOpen}
        onClose={() => setTargetModalOpen(false)}
        onSaved={refreshOverview}
      />
    </main>
  );
};
