import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Area,
  AreaChart,
  CartesianGrid,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import { getPortfolios } from '../../portfolios/api/portfolioApi';
import type { PortfolioDto } from '../../portfolios/types';
import { performanceApi } from '../api/performanceApi';
import type {
  BenchmarkComparison,
  BenchmarkDefinition,
  PerformanceDrawdownSeries,
  PerformanceMonthlyReturns,
  PerformanceQuality,
  PerformanceSeries,
  PerformanceSummary,
} from '../types';
import './PerformanceCenter.css';

type PeriodPreset = '1M' | '3M' | '6M' | 'YTD' | '1Y' | 'ALL';
type ChartMode = 'growth' | 'value';

const ISO_DATE_LENGTH = 10;

const toIsoDate = (date: Date) => date.toISOString().slice(0, ISO_DATE_LENGTH);

const getDateRange = (preset: PeriodPreset) => {
  const to = new Date();
  const from = new Date(to);

  switch (preset) {
    case '1M':
      from.setMonth(from.getMonth() - 1);
      break;
    case '3M':
      from.setMonth(from.getMonth() - 3);
      break;
    case '6M':
      from.setMonth(from.getMonth() - 6);
      break;
    case 'YTD':
      from.setMonth(0, 1);
      break;
    case '1Y':
      from.setFullYear(from.getFullYear() - 1);
      break;
    case 'ALL':
      from.setFullYear(2000, 0, 1);
      break;
  }

  return { from: toIsoDate(from), to: toIsoDate(to) };
};

const formatPercent = (value: number | null | undefined) =>
  value == null
    ? '—'
    : new Intl.NumberFormat('vi-VN', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
      }).format(value) + '%';

const formatMoney = (value: number | null | undefined, currency: string) =>
  value == null
    ? '—'
    : new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
        style: 'currency',
        currency,
        maximumFractionDigits: currency === 'VND' ? 0 : 2,
      }).format(value);

const shortDate = (value: string) =>
  new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit' }).format(
    new Date(value),
  );

const metricTone = (value: number | null | undefined) =>
  value == null ? 'neutral' : value >= 0 ? 'positive' : 'negative';

const qualityLabel = (quality: PerformanceQuality) => {
  if (quality.qualityStatus === 'Complete') return 'Dữ liệu đầy đủ';
  if (quality.qualityStatus === 'Unavailable') return 'Chưa có dữ liệu';
  if (quality.qualityStatus === 'Legacy') return 'Dữ liệu lịch sử';
  return 'Dữ liệu cần chú ý';
};

export function PerformanceCenter() {
  const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
  const [benchmarks, setBenchmarks] = useState<BenchmarkDefinition[]>([]);
  const [portfolioId, setPortfolioId] = useState('');
  const [benchmarkId, setBenchmarkId] = useState('');
  const [currency, setCurrency] = useState('VND');
  const [period, setPeriod] = useState<PeriodPreset>('1Y');
  const [chartMode, setChartMode] = useState<ChartMode>('growth');
  const [summary, setSummary] = useState<PerformanceSummary | null>(null);
  const [series, setSeries] = useState<PerformanceSeries | null>(null);
  const [drawdowns, setDrawdowns] = useState<PerformanceDrawdownSeries | null>(null);
  const [monthly, setMonthly] = useState<PerformanceMonthlyReturns | null>(null);
  const [comparison, setComparison] = useState<BenchmarkComparison | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let active = true;

    Promise.all([getPortfolios(), performanceApi.getBenchmarks()])
      .then(([portfolioRows, benchmarkRows]) => {
        if (!active) return;
        setPortfolios(portfolioRows);
        setBenchmarks(benchmarkRows);
        const preferred = benchmarkRows.find(
          (benchmark) => benchmark.isDefault && benchmark.currency === currency,
        );
        setBenchmarkId((current) => current || preferred?.id || benchmarkRows[0]?.id || '');
      })
      .catch(() => {
        if (active) setError('Không thể tải danh mục và benchmark.');
      });

    return () => {
      active = false;
    };
  }, [currency]);

  const loadPerformance = useCallback(async () => {
    setLoading(true);
    setError(null);
    const range = getDateRange(period);
    const filters = {
      portfolioId: portfolioId || undefined,
      assetGroup: 'All',
      currency,
      ...range,
    };

    try {
      const [summaryResult, seriesResult, drawdownResult, monthlyResult, comparisonResult] =
        await Promise.all([
          performanceApi.getSummary(filters),
          performanceApi.getSeries(filters),
          performanceApi.getDrawdowns(filters),
          performanceApi.getMonthlyReturns(filters),
          benchmarkId
            ? performanceApi.getBenchmarkComparison(benchmarkId, filters)
            : Promise.resolve(null),
        ]);

      setSummary(summaryResult);
      setSeries(seriesResult);
      setDrawdowns(drawdownResult);
      setMonthly(monthlyResult);
      setComparison(comparisonResult);
    } catch {
      setError('Không thể tải dữ liệu hiệu suất. Hãy thử lại sau.');
    } finally {
      setLoading(false);
    }
  }, [benchmarkId, currency, period, portfolioId]);

  useEffect(() => {
    void loadPerformance();
  }, [loadPerformance, reloadKey]);

  const growthData = useMemo(() => {
    if (comparison) {
      return comparison.points.map((point) => ({
        date: point.date,
        portfolio: point.portfolioGrowthIndex,
        benchmark: point.benchmarkGrowthIndex,
      }));
    }

    return (series?.points ?? []).map((point) => ({
      date: point.date,
      portfolio: point.growthIndex,
      benchmark: null,
    }));
  }, [comparison, series]);

  const valueData = useMemo(
    () =>
      (series?.points ?? []).map((point) => ({
        date: point.date,
        value: point.netAssetValue,
        flow: point.netExternalFlow,
      })),
    [series],
  );

  const selectedBenchmark = benchmarks.find((item) => item.id === benchmarkId);
  const quality = summary?.quality;
  const hasData = Boolean(series?.points.length);

  return (
    <main className="performance-center">
      <header className="performance-hero">
        <div>
          <Link className="performance-back" to="/analytics">
            ← Phân tích tổng quan
          </Link>
          <p className="performance-eyebrow">Performance Center</p>
          <h1>Hiệu suất, rõ ràng đến từng dòng tiền.</h1>
          <p className="performance-subtitle">
            Tách biến động thị trường khỏi tiền nạp/rút và so sánh danh mục trên cùng
            một điểm xuất phát.
          </p>
        </div>
        {quality && (
          <div className={`quality-pill quality-${quality.qualityStatus.toLowerCase()}`}>
            <span className="quality-dot" />
            {qualityLabel(quality)}
          </div>
        )}
      </header>

      <section className="performance-filters" aria-label="Bộ lọc hiệu suất">
        <label>
          <span>Danh mục</span>
          <select value={portfolioId} onChange={(event) => setPortfolioId(event.target.value)}>
            <option value="">Tất cả danh mục</option>
            {portfolios.map((portfolio) => (
              <option key={portfolio.id} value={portfolio.id}>
                {portfolio.name}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Đơn vị</span>
          <select value={currency} onChange={(event) => setCurrency(event.target.value)}>
            <option value="VND">VND</option>
            <option value="USD">USD</option>
          </select>
        </label>
        <label>
          <span>Benchmark</span>
          <select value={benchmarkId} onChange={(event) => setBenchmarkId(event.target.value)}>
            <option value="">Không so sánh</option>
            {benchmarks
              .filter((benchmark) => benchmark.isActive)
              .map((benchmark) => (
                <option key={benchmark.id} value={benchmark.id}>
                  {benchmark.name} · {benchmark.symbol}
                </option>
              ))}
          </select>
        </label>
        <div className="period-picker" aria-label="Khoảng thời gian">
          {(['1M', '3M', '6M', 'YTD', '1Y', 'ALL'] as PeriodPreset[]).map((item) => (
            <button
              className={period === item ? 'active' : ''}
              key={item}
              onClick={() => setPeriod(item)}
              type="button"
            >
              {item === 'ALL' ? 'Tất cả' : item}
            </button>
          ))}
        </div>
      </section>

      {error && (
        <section className="performance-message error" role="alert">
          <div>
            <strong>Không tải được Performance Center</strong>
            <p>{error}</p>
          </div>
          <button type="button" onClick={() => setReloadKey((value) => value + 1)}>
            Thử lại
          </button>
        </section>
      )}

      {!error && quality && quality.qualityStatus !== 'Complete' && (
        <section className="performance-message warning">
          <div>
            <strong>{qualityLabel(quality)}</strong>
            <p>
              {quality.missingSnapshotDays} ngày thiếu snapshot · {quality.staleAssetCount} tài
              sản có giá cũ · {quality.unclassifiedCashFlowCount} dòng tiền chưa phân loại.
            </p>
          </div>
          <span>{quality.asOf ? `Cập nhật ${shortDate(quality.asOf)}` : 'Chưa có snapshot'}</span>
        </section>
      )}

      <section className="performance-kpis" aria-label="Chỉ số hiệu suất">
        <article className="performance-kpi featured">
          <span>TWR</span>
          <strong className={metricTone(summary?.timeWeightedReturnPercentage.value)}>
            {loading ? '…' : formatPercent(summary?.timeWeightedReturnPercentage.value)}
          </strong>
          <small title={summary?.timeWeightedReturnPercentage.reason ?? undefined}>
            Loại trừ tác động nạp/rút
          </small>
        </article>
        <article className="performance-kpi">
          <span>XIRR</span>
          <strong className={metricTone(summary?.moneyWeightedReturnPercentage.value)}>
            {loading ? '…' : formatPercent(summary?.moneyWeightedReturnPercentage.value)}
          </strong>
          <small title={summary?.moneyWeightedReturnPercentage.reason ?? undefined}>
            Lợi suất theo dòng tiền
          </small>
        </article>
        <article className="performance-kpi">
          <span>Tổng P&amp;L</span>
          <strong className={metricTone(summary?.totalPnl)}>
            {loading ? '…' : formatMoney(summary?.totalPnl, currency)}
          </strong>
          <small>
            Đã chốt {formatMoney(summary?.realizedPnl, currency)}
          </small>
        </article>
        <article className="performance-kpi">
          <span>Max drawdown</span>
          <strong className={metricTone(summary?.maximumDrawdownPercentage.value)}>
            {loading ? '…' : formatPercent(summary?.maximumDrawdownPercentage.value)}
          </strong>
          <small title={summary?.maximumDrawdownPercentage.reason ?? undefined}>
            Mức giảm sâu nhất
          </small>
        </article>
      </section>

      {!error && !loading && !hasData && (
        <section className="performance-empty">
          <div className="empty-orbit" aria-hidden="true">◎</div>
          <h2>Chưa đủ snapshot để tính hiệu suất</h2>
          <p>
            Hãy cập nhật giá và tạo snapshot hằng ngày. Performance Center cần tối thiểu hai
            mốc để tính lợi suất.
          </p>
          <Link to="/analytics">Tạo snapshot tại trang Phân tích</Link>
        </section>
      )}

      {!error && (loading || hasData) && (
        <>
          <section className="performance-panel performance-chart-panel">
            <div className="panel-heading">
              <div>
                <p className="panel-kicker">Đường hiệu suất</p>
                <h2>{chartMode === 'growth' ? 'Tăng trưởng chuẩn hóa' : 'Giá trị ròng'}</h2>
              </div>
              <div className="chart-toggle">
                <button
                  className={chartMode === 'growth' ? 'active' : ''}
                  onClick={() => setChartMode('growth')}
                  type="button"
                >
                  Growth
                </button>
                <button
                  className={chartMode === 'value' ? 'active' : ''}
                  onClick={() => setChartMode('value')}
                  type="button"
                >
                  NAV
                </button>
              </div>
            </div>
            <div className="chart-legend">
              <span><i className="legend-portfolio" />Danh mục</span>
              {chartMode === 'growth' && selectedBenchmark && (
                <span><i className="legend-benchmark" />{selectedBenchmark.symbol}</span>
              )}
              {comparison && comparison.missingBenchmarkDays > 0 && (
                <em>{comparison.missingBenchmarkDays} ngày benchmark bị trống, không nội suy</em>
              )}
            </div>
            <div className="performance-chart">
              {loading ? (
                <div className="chart-skeleton" />
              ) : chartMode === 'growth' ? (
                <ResponsiveContainer width="100%" height="100%">
                  <LineChart data={growthData}>
                    <CartesianGrid stroke="rgba(148, 163, 184, .12)" vertical={false} />
                    <XAxis dataKey="date" tickFormatter={shortDate} minTickGap={34} />
                    <YAxis domain={['auto', 'auto']} width={48} />
                    <Tooltip
                      labelFormatter={(label) => new Date(String(label)).toLocaleDateString('vi-VN')}
                      formatter={(value: any) =>
                        value == null ? 'Thiếu dữ liệu' : Number(value).toFixed(2)
                      }
                    />
                    <Line
                      dataKey="portfolio"
                      dot={false}
                      stroke="#8b5cf6"
                      strokeWidth={3}
                      type="monotone"
                    />
                    <Line
                      connectNulls={false}
                      dataKey="benchmark"
                      dot={false}
                      stroke="#22d3ee"
                      strokeDasharray="6 5"
                      strokeWidth={2}
                      type="linear"
                    />
                  </LineChart>
                </ResponsiveContainer>
              ) : (
                <ResponsiveContainer width="100%" height="100%">
                  <AreaChart data={valueData}>
                    <defs>
                      <linearGradient id="navFill" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor="#8b5cf6" stopOpacity={0.42} />
                        <stop offset="100%" stopColor="#8b5cf6" stopOpacity={0.02} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid stroke="rgba(148, 163, 184, .12)" vertical={false} />
                    <XAxis dataKey="date" tickFormatter={shortDate} minTickGap={34} />
                    <YAxis tickFormatter={(value) => Intl.NumberFormat('vi', { notation: 'compact' }).format(value)} />
                    <Tooltip
                      labelFormatter={(label) => new Date(String(label)).toLocaleDateString('vi-VN')}
                      formatter={(value: any) =>
                        formatMoney(value == null ? null : Number(value), currency)
                      }
                    />
                    <Area
                      dataKey="value"
                      fill="url(#navFill)"
                      stroke="#a78bfa"
                      strokeWidth={3}
                      type="monotone"
                    />
                  </AreaChart>
                </ResponsiveContainer>
              )}
            </div>
          </section>

          <div className="performance-grid">
            <section className="performance-panel">
              <div className="panel-heading">
                <div>
                  <p className="panel-kicker">Rủi ro</p>
                  <h2>Drawdown</h2>
                </div>
                <strong className="negative">
                  {formatPercent(drawdowns?.maximumDrawdownPercentage.value)}
                </strong>
              </div>
              <div className="secondary-chart">
                {loading ? (
                  <div className="chart-skeleton" />
                ) : (
                  <ResponsiveContainer width="100%" height="100%">
                    <AreaChart data={drawdowns?.points ?? []}>
                      <defs>
                        <linearGradient id="drawdownFill" x1="0" y1="0" x2="0" y2="1">
                          <stop offset="0%" stopColor="#fb7185" stopOpacity={0.08} />
                          <stop offset="100%" stopColor="#fb7185" stopOpacity={0.38} />
                        </linearGradient>
                      </defs>
                      <XAxis dataKey="date" tickFormatter={shortDate} minTickGap={36} />
                      <YAxis tickFormatter={(value) => `${value}%`} width={44} />
                      <Tooltip
                        formatter={(value: any) =>
                          formatPercent(value == null ? null : Number(value))
                        }
                      />
                      <Area
                        dataKey="drawdownPercentage"
                        fill="url(#drawdownFill)"
                        stroke="#fb7185"
                        strokeWidth={2}
                        type="monotone"
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                )}
              </div>
            </section>

            <section className="performance-panel">
              <div className="panel-heading">
                <div>
                  <p className="panel-kicker">Theo tháng</p>
                  <h2>Return heatmap</h2>
                </div>
                <span className="volatility">
                  Volatility {formatPercent(monthly?.monthlyVolatilityPercentage.value)}
                </span>
              </div>
              <div className="monthly-heatmap">
                {(monthly?.months ?? []).map((month) => {
                  const value = month.returnPercentage;
                  const tone =
                    value == null
                      ? 'missing'
                      : value >= 5
                        ? 'gain-strong'
                        : value >= 0
                          ? 'gain'
                          : value <= -5
                            ? 'loss-strong'
                            : 'loss';
                  return (
                    <div className={`month-cell ${tone}`} key={month.month}>
                      <span>{month.month.slice(5, 7)}/{month.month.slice(2, 4)}</span>
                      <strong>{formatPercent(value)}</strong>
                    </div>
                  );
                })}
                {!loading && !monthly?.months.length && (
                  <p className="heatmap-empty">Chưa có đủ dữ liệu theo tháng.</p>
                )}
              </div>
              <div className="month-extremes">
                <span>Tốt nhất <strong className="positive">{formatPercent(monthly?.bestMonthPercentage.value)}</strong></span>
                <span>Thấp nhất <strong className="negative">{formatPercent(monthly?.worstMonthPercentage.value)}</strong></span>
              </div>
            </section>
          </div>
        </>
      )}
    </main>
  );
}
