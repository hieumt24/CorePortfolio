import { useCallback, useEffect, useMemo, useState } from 'react';
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import type { TooltipContentProps } from 'recharts';
import { analyticsApi } from '../../analytics/api/analyticsApi';
import { settingsApi } from '../../admin/api/settingsApi';
import { useNotification } from '../../../context/NotificationContext';
import { GlobalReportSkeleton } from '../../../shared/components/Skeleton';
import { getGlobalHistory, getGlobalReport, mockGlobalHistory } from '../api/reportsApi';
import type { GlobalReportDto, SnapshotDto } from '../types';
import { HistoricalPerformanceChart } from './HistoricalPerformanceChart';
import { InvestedCapitalChart } from './InvestedCapitalChart';
import './GlobalReportDashboard.css';

const REPORT_COLORS = [
  'var(--report-chart-1)',
  'var(--report-chart-2)',
  'var(--report-chart-3)',
  'var(--report-chart-4)',
  'var(--report-chart-5)',
  'var(--report-chart-6)',
  'var(--report-chart-7)',
  'var(--report-chart-8)',
];

const formatVnd = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value);

const formatCompactVnd = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    notation: 'compact',
    maximumFractionDigits: 1,
  }).format(value);

const fetchReportData = () =>
  Promise.all([
    getGlobalReport(),
    settingsApi.getSetting('USD_TO_VND'),
    getGlobalHistory(),
  ]);

export function GlobalReportDashboard() {
  const [reportData, setReportData] = useState<GlobalReportDto | null>(null);
  const [historyData, setHistoryData] = useState<SnapshotDto[]>([]);
  const [usdToVndRate, setUsdToVndRate] = useState(0);
  const [loading, setLoading] = useState(true);
  const [isSnapshotLoading, setIsSnapshotLoading] = useState(false);
  const [isMockLoading, setIsMockLoading] = useState(false);
  const [loadError, setLoadError] = useState(false);
  const { showNotification } = useNotification();

  const loadReport = useCallback(async () => {
    try {
      const [report, rate, history] = await fetchReportData();
      setReportData(report);
      setHistoryData(history);
      setUsdToVndRate(rate ? Number.parseFloat(rate) : 0);
    } catch (error) {
      console.error('Failed to fetch global report data', error);
      setLoadError(true);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    let cancelled = false;

    void fetchReportData()
      .then(([report, rate, history]) => {
        if (cancelled) return;
        setReportData(report);
        setHistoryData(history);
        setUsdToVndRate(rate ? Number.parseFloat(rate) : 0);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        console.error('Failed to fetch global report data', error);
        setLoadError(true);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });

    return () => {
      cancelled = true;
    };
  }, []);

  const summary = useMemo(() => {
    if (!reportData) {
      return {
        totalInvested: 0,
        totalValue: 0,
        categoryData: [] as { name: string; value: number }[],
        portfolioData: [] as { name: string; value: number }[],
      };
    }

    const convertToVnd = (value: number, currency: string) =>
      currency === 'USD' ? value * usdToVndRate : value;

    const groupedCategories = new Map<string, number>();
    let totalInvested = 0;
    let totalValue = 0;

    reportData.allocationsByCategory.forEach((category) => {
      const value = convertToVnd(category.currentValue, category.currency);
      totalInvested += convertToVnd(category.totalInvested, category.currency);
      totalValue += value;
      groupedCategories.set(category.categoryName, (groupedCategories.get(category.categoryName) ?? 0) + value);
    });

    const categoryData = Array.from(groupedCategories, ([name, value]) => ({ name, value }))
      .filter((item) => item.value > 0)
      .sort((a, b) => b.value - a.value);

    const portfolioData = reportData.allocationsByPortfolio
      .map((portfolio) => ({
        name: portfolio.portfolioName,
        value: portfolio.currencies.reduce(
          (sum, currency) => sum + convertToVnd(currency.currentValue, currency.currency),
          0,
        ),
      }))
      .filter((item) => item.value > 0)
      .sort((a, b) => b.value - a.value);

    return { totalInvested, totalValue, categoryData, portfolioData };
  }, [reportData, usdToVndRate]);

  const performance = useMemo(() => {
    const sortedHistory = [...historyData].sort(
      (a, b) => new Date(a.date).getTime() - new Date(b.date).getTime(),
    );
    const currentProfit = summary.totalValue - summary.totalInvested;

    const forPeriod = (daysAgo: number) => {
      if (sortedHistory.length === 0) return { profit: 0, percentage: 0 };
      const targetDate = new Date();
      targetDate.setDate(targetDate.getDate() - daysAgo);
      const snapshot =
        sortedHistory.filter((item) => new Date(item.date) <= targetDate).at(-1) ?? sortedHistory[0];
      const historicalProfit = snapshot.totalValue - snapshot.totalInvested;
      const profit = currentProfit - historicalProfit;
      const percentage = snapshot.totalValue > 0 ? (profit / snapshot.totalValue) * 100 : 0;
      return { profit, percentage };
    };

    return [
      { label: '7 ngày', ...forPeriod(7) },
      { label: '30 ngày', ...forPeriod(30) },
      { label: '1 năm', ...forPeriod(365) },
      {
        label: 'Toàn thời gian',
        profit: currentProfit,
        percentage: summary.totalInvested > 0 ? (currentProfit / summary.totalInvested) * 100 : 0,
      },
    ];
  }, [historyData, summary.totalInvested, summary.totalValue]);

  const handleTakeSnapshot = async () => {
    try {
      setIsSnapshotLoading(true);
      await analyticsApi.triggerSnapshot();
      const [report, history] = await Promise.all([getGlobalReport(), getGlobalHistory()]);
      setReportData(report);
      setHistoryData(history);
      showNotification('Đã cập nhật giá trị danh mục mới nhất.', 'success');
    } catch (error) {
      console.error('Failed to update portfolio snapshot', error);
      showNotification('Không thể cập nhật giá trị danh mục. Vui lòng thử lại.', 'error');
    } finally {
      setIsSnapshotLoading(false);
    }
  };

  const handleRetry = () => {
    setLoading(true);
    setLoadError(false);
    void loadReport();
  };

  const handleGenerateMock = async () => {
    try {
      setIsMockLoading(true);
      await mockGlobalHistory();
      setHistoryData(await getGlobalHistory());
      showNotification('Đã tạo dữ liệu lịch sử mẫu.', 'success');
    } catch (error) {
      console.error('Failed to generate mock history', error);
      showNotification('Không thể tạo dữ liệu lịch sử mẫu.', 'error');
    } finally {
      setIsMockLoading(false);
    }
  };

  const renderAllocationTooltip = ({ active, payload }: TooltipContentProps) => {
    if (!active || !payload?.length) return null;
    const item = payload[0];
    const source = item.payload as { fill?: string } | undefined;
    return (
      <div className="report-tooltip" role="status">
        <span>{item.name?.toString() ?? ''}</span>
        <strong style={{ color: source?.fill }}>{formatVnd(Number(item.value) || 0)}</strong>
      </div>
    );
  };

  if (loading) return <GlobalReportSkeleton />;

  if (loadError || !reportData) {
    return (
      <div className="report-error glass-panel" role="alert">
        <span className="report-error-mark" aria-hidden="true">!</span>
        <h1>Chưa thể tải báo cáo</h1>
        <p>Kết nối dữ liệu đang gián đoạn. Các danh mục của bạn không bị ảnh hưởng.</p>
        <button className="btn btn-primary" type="button" onClick={handleRetry}>
          Thử tải lại
        </button>
      </div>
    );
  }

  const totalProfit = summary.totalValue - summary.totalInvested;
  const totalReturn = summary.totalInvested > 0 ? (totalProfit / summary.totalInvested) * 100 : 0;
  const profitTone = totalProfit >= 0 ? 'positive' : 'negative';

  const renderAllocationChart = (
    title: string,
    description: string,
    data: { name: string; value: number }[],
    colorOffset = 0,
  ) => (
    <article className="report-chart-card glass-panel">
      <div className="report-section-heading">
        <div>
          <h2>{title}</h2>
          <p>{description}</p>
        </div>
        <span>{data.length} nhóm</span>
      </div>
      {data.length > 0 ? (
        <div className="report-pie" role="img" aria-label={`${title}: ${data.length} nhóm phân bổ`}>
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={data}
                cx="50%"
                cy="46%"
                innerRadius="48%"
                outerRadius="70%"
                paddingAngle={3}
                dataKey="value"
                nameKey="name"
                labelLine={false}
                stroke="var(--report-chart-stroke)"
                strokeWidth={2}
              >
                {data.map((item, index) => (
                  <Cell
                    key={item.name}
                    fill={REPORT_COLORS[(index + colorOffset) % REPORT_COLORS.length]}
                  />
                ))}
              </Pie>
              <Tooltip content={renderAllocationTooltip} />
              <Legend verticalAlign="bottom" iconType="circle" iconSize={8} />
            </PieChart>
          </ResponsiveContainer>
          <div className="report-pie-center" aria-hidden="true">
            <small>Tổng giá trị</small>
            <strong>{formatCompactVnd(data.reduce((sum, item) => sum + item.value, 0))}</strong>
          </div>
        </div>
      ) : (
        <div className="report-empty-state">
          <span aria-hidden="true">◇</span>
          <strong>Chưa có dữ liệu phân bổ</strong>
          <p>Dữ liệu sẽ xuất hiện sau khi tài sản được ghi nhận.</p>
        </div>
      )}
    </article>
  );

  return (
    <main className="report-dashboard">
      <header className="report-header">
        <div className="report-heading-copy">
          <span className="report-kicker">Báo cáo hợp nhất</span>
          <h1>Sức khỏe toàn bộ danh mục</h1>
          <p>Theo dõi vốn, hiệu suất và mức phân bổ trên tất cả portfolio.</p>
        </div>
        <div className="report-header-actions">
          <span className="report-fx-rate">
            <small>Tỷ giá quy đổi</small>
            <strong>1 USD = {formatVnd(usdToVndRate)}</strong>
          </span>
          <button
            className="btn btn-primary report-snapshot-button"
            type="button"
            onClick={() => void handleTakeSnapshot()}
            disabled={isSnapshotLoading}
            aria-busy={isSnapshotLoading}
            data-state={isSnapshotLoading ? 'loading' : 'default'}
          >
            <span className="report-refresh-icon" aria-hidden="true">↻</span>
            {isSnapshotLoading ? 'Đang cập nhật…' : 'Cập nhật giá trị'}
          </button>
        </div>
      </header>

      <section className="report-value-grid" aria-label="Tổng quan giá trị danh mục">
        <article className="report-value-spotlight glass-panel">
          <div className="report-value-label">
            <span>Giá trị hiện tại</span>
            <span className={`report-status-dot ${profitTone}`}>{totalProfit >= 0 ? 'Đang tăng' : 'Đang giảm'}</span>
          </div>
          <strong>{formatVnd(summary.totalValue)}</strong>
          <p className={profitTone}>
            {totalProfit >= 0 ? '+' : ''}{formatVnd(totalProfit)} ({totalReturn >= 0 ? '+' : ''}{totalReturn.toFixed(2)}%)
            <span> so với vốn đầu tư</span>
          </p>
        </article>
        <article className="report-value-card glass-panel">
          <span>Vốn đã đầu tư</span>
          <strong>{formatVnd(summary.totalInvested)}</strong>
          <small>Cơ sở tính lợi nhuận toàn danh mục</small>
        </article>
        <article className={`report-value-card glass-panel ${profitTone}`}>
          <span>Lợi nhuận chưa thực hiện</span>
          <strong>{totalProfit >= 0 ? '+' : ''}{formatVnd(totalProfit)}</strong>
          <small>{totalReturn >= 0 ? '+' : ''}{totalReturn.toFixed(2)}% trên tổng vốn</small>
        </article>
      </section>

      <InvestedCapitalChart totalInvested={summary.totalInvested} currentValue={summary.totalValue} />

      <section className="report-performance-section" aria-labelledby="performance-title">
        <div className="report-section-heading report-section-heading-wide">
          <div>
            <h2 id="performance-title">Hiệu suất theo thời gian</h2>
            <p>Thay đổi lợi nhuận so với snapshot gần nhất của từng mốc.</p>
          </div>
        </div>
        <div className="report-period-grid">
          {performance.map((period) => {
            const tone = period.profit >= 0 ? 'positive' : 'negative';
            return (
              <article className={`report-period-card glass-panel ${tone}`} key={period.label}>
                <span>{period.label}</span>
                <strong>{period.profit >= 0 ? '+' : ''}{formatVnd(period.profit)}</strong>
                <small>
                  <span aria-hidden="true">{period.profit >= 0 ? '↗' : '↘'}</span>
                  {Math.abs(period.percentage).toFixed(2)}%
                </small>
              </article>
            );
          })}
        </div>
      </section>

      <section className="report-allocation-grid" aria-label="Phân bổ danh mục">
        {renderAllocationChart('Phân bổ theo danh mục', 'Tỷ trọng theo nhóm tài sản.', summary.categoryData)}
        {renderAllocationChart('Phân bổ theo portfolio', 'Tỷ trọng giữa các portfolio.', summary.portfolioData, 3)}
      </section>

      <HistoricalPerformanceChart
        data={historyData}
        isGeneratingMock={isMockLoading}
        onGenerateMock={() => void handleGenerateMock()}
      />
    </main>
  );
}
