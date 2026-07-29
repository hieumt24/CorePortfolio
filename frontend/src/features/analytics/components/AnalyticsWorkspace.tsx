import { Link } from 'react-router-dom';
import type { KeyboardEvent } from 'react';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';
import type { AnalyticsOverviewDto } from '../types';
import { analyticsTabs, type AnalyticsTab } from '../utils/analyticsUrlState';
import { ScenarioLab } from './ScenarioLab';

interface AnalyticsWorkspaceProps {
  data: AnalyticsOverviewDto;
  activeTab: AnalyticsTab;
  onTabChange: (tab: AnalyticsTab) => void;
  onOpenTargets: () => void;
}

const tabLabels: Record<AnalyticsTab, string> = {
  overview: 'Tổng quan',
  performance: 'Hiệu suất',
  allocation: 'Phân bổ',
  cashflow: 'Dòng tiền & mục tiêu',
  scenario: 'Mô phỏng',
};

const compactNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN', { notation: 'compact', maximumFractionDigits: 1 }).format(value);

export const AnalyticsWorkspace = ({
  data,
  activeTab,
  onTabChange,
  onOpenTargets,
}: AnalyticsWorkspaceProps) => {
  const money = new Intl.NumberFormat(data.scope.currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency: data.scope.currency,
    maximumFractionDigits: data.scope.currency === 'VND' ? 0 : 2,
  });
  const chartSummary = data.series.points.length > 0
    ? `Chuỗi có ${data.series.points.length} điểm, từ ${money.format(data.series.points[0].netAssetValue)} đến ${money.format(data.series.points.at(-1)?.netAssetValue ?? 0)}.`
    : 'Chưa có điểm dữ liệu hiệu suất trong kỳ.';

  const handleTabKeyDown = (event: KeyboardEvent<HTMLButtonElement>, tab: AnalyticsTab) => {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return;
    event.preventDefault();
    const currentIndex = analyticsTabs.indexOf(tab);
    const nextIndex = event.key === 'Home'
      ? 0
      : event.key === 'End'
        ? analyticsTabs.length - 1
        : (currentIndex + (event.key === 'ArrowRight' ? 1 : -1) + analyticsTabs.length)
          % analyticsTabs.length;
    const nextTab = analyticsTabs[nextIndex];
    onTabChange(nextTab);
    document.getElementById(`analytics-tab-${nextTab}`)?.focus();
  };

  const renderPerformanceChart = (detailed = false) => (
    <section className={`analytics-primary-chart ${detailed ? 'is-detailed' : ''}`}>
      <div className="analytics-panel-heading">
        <div>
          <span className="analytics-eyebrow">Diễn biến trong kỳ</span>
          <h2>NAV, dòng tiền và tăng trưởng</h2>
        </div>
        <div className="analytics-chart-legend" aria-label="Chú giải biểu đồ">
          <span className="is-nav">NAV</span>
          <span className="is-flow">Dòng tiền lũy kế</span>
        </div>
      </div>
      {data.series.points.length === 0 ? (
        <div className="analytics-empty-state">
          <strong>Chưa có chuỗi hiệu suất</strong>
          <p>Tạo snapshot để theo dõi NAV và lợi suất theo thời gian.</p>
        </div>
      ) : (
        <>
          <p className="sr-only">{chartSummary}</p>
          <div className="analytics-chart-canvas" role="img" aria-label={chartSummary}>
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={data.series.points} margin={{ top: 12, right: 8, left: 0, bottom: 0 }}>
                <defs>
                  <linearGradient id="analyticsNavFill" x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor="#8b5cf6" stopOpacity={0.42} />
                    <stop offset="100%" stopColor="#8b5cf6" stopOpacity={0} />
                  </linearGradient>
                </defs>
                <CartesianGrid stroke="rgba(148, 163, 184, 0.12)" vertical={false} />
                <XAxis
                  dataKey="date"
                  tick={{ fill: '#94a3b8', fontSize: 11 }}
                  axisLine={false}
                  tickLine={false}
                  minTickGap={36}
                />
                <YAxis
                  tickFormatter={compactNumber}
                  tick={{ fill: '#94a3b8', fontSize: 11 }}
                  axisLine={false}
                  tickLine={false}
                  width={58}
                />
                <Tooltip
                  formatter={(value, name) => [
                    money.format(Number(value)),
                    name === 'netAssetValue' ? 'NAV' : 'Dòng tiền lũy kế',
                  ]}
                  contentStyle={{
                    background: '#111827',
                    border: '1px solid rgba(148, 163, 184, 0.24)',
                    borderRadius: 12,
                  }}
                />
                <Area
                  type="monotone"
                  dataKey="netAssetValue"
                  stroke="#a78bfa"
                  strokeWidth={3}
                  fill="url(#analyticsNavFill)"
                  dot={false}
                  activeDot={{ r: 4 }}
                />
                <Area
                  type="monotone"
                  dataKey="cumulativeExternalFlow"
                  stroke="#22d3ee"
                  strokeWidth={2}
                  fill="transparent"
                  dot={false}
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </>
      )}
    </section>
  );

  const renderAllocation = () => (
    <section className="analytics-allocation-workspace">
      <div className="analytics-panel-heading">
        <div>
          <span className="analytics-eyebrow">Phân bổ hiện tại</span>
          <h2>Khoảng cách so với mục tiêu</h2>
        </div>
        <button type="button" className="analytics-secondary-button" onClick={onOpenTargets}>
          Chỉnh mục tiêu
        </button>
      </div>
      {data.allocation.length === 0 ? (
        <div className="analytics-empty-state">
          <strong>Chưa có tài sản đầu tư</strong>
          <p>Phân bổ sẽ xuất hiện khi danh mục có giá trị đang theo dõi.</p>
        </div>
      ) : (
        <div className="analytics-allocation-layout">
          <div className="analytics-allocation-chart" role="img" aria-label="Biểu đồ tỷ trọng tài sản">
            <ResponsiveContainer width="100%" height="100%">
              <PieChart>
                <Pie
                  data={data.allocation}
                  dataKey="totalValue"
                  nameKey="categoryName"
                  innerRadius="62%"
                  outerRadius="86%"
                  paddingAngle={3}
                >
                  {data.allocation.map((item) => <Cell key={item.categoryName} fill={item.color} />)}
                </Pie>
                <Tooltip formatter={(value) => money.format(Number(value))} />
              </PieChart>
            </ResponsiveContainer>
            <div>
              <span>Tổng giá trị</span>
              <strong>{compactNumber(data.allocation.reduce((sum, item) => sum + item.totalValue, 0))}</strong>
              <small>{data.scope.currency}</small>
            </div>
          </div>
          <div className="analytics-table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Nhóm tài sản</th>
                  <th>Hiện tại</th>
                  <th>Mục tiêu</th>
                  <th>Độ lệch</th>
                </tr>
              </thead>
              <tbody>
                {data.allocation.map((item) => (
                  <tr key={item.categoryName}>
                    <td>
                      <span className="analytics-color-dot" style={{ background: item.color }} />
                      {item.categoryName}
                    </td>
                    <td>{item.percentage.toFixed(1)}%</td>
                    <td>{item.targetPercentage.toFixed(1)}%</td>
                    <td className={Math.abs(item.deviation) > 5 ? 'is-warning' : 'is-ok'}>
                      {item.deviation > 0 ? '+' : ''}{item.deviation.toFixed(1)}đ%
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
      <p className="analytics-context-note">
        Biên 5 điểm phần trăm chỉ là ngưỡng rà soát. Cân nhắc phí, thuế và dòng tiền mới trước khi điều chỉnh.
      </p>
    </section>
  );

  const renderCashflow = () => (
    <div className="analytics-cashflow-layout">
      <section className="analytics-cashflow-chart">
        <div className="analytics-panel-heading">
          <div>
            <span className="analytics-eyebrow">Khả năng tài trợ kế hoạch</span>
            <h2>Thu và chi theo tháng</h2>
          </div>
        </div>
        <div className="analytics-chart-canvas" role="img" aria-label="Biểu đồ thu chi theo tháng">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={data.cashflow} margin={{ top: 12, right: 8, left: 0, bottom: 0 }}>
              <CartesianGrid stroke="rgba(148, 163, 184, 0.12)" vertical={false} />
              <XAxis dataKey="month" tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} />
              <YAxis tickFormatter={compactNumber} tick={{ fill: '#94a3b8', fontSize: 11 }} axisLine={false} tickLine={false} width={58} />
              <Tooltip formatter={(value) => money.format(Number(value))} />
              <Bar dataKey="income" name="Thu" fill="#34d399" radius={[5, 5, 0, 0]} />
              <Bar dataKey="expense" name="Chi" fill="#fb7185" radius={[5, 5, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </section>
      <aside className="analytics-plan-summary">
        <span className="analytics-eyebrow">Kế hoạch đang chạy</span>
        <dl>
          <div>
            <dt>Mục tiêu đang mở</dt>
            <dd>{data.goals.activeCount}</dd>
            <small>{data.goals.atRiskCount} mục tiêu cần chú ý</small>
          </div>
          <div>
            <dt>Còn cần tích lũy</dt>
            <dd>{money.format(data.goals.totalRemaining)}</dd>
            <small>Trong phạm vi đã chọn</small>
          </div>
          <div>
            <dt>Kế hoạch DCA hoạt động</dt>
            <dd>{data.dca.activeCount}</dd>
            <small>{data.dca.insufficientCashCount} kế hoạch thiếu tiền mặt</small>
          </div>
          <div>
            <dt>Dòng tiền tháng này</dt>
            <dd className={data.financialHealth.monthlyNetFlow >= 0 ? 'is-positive' : 'is-negative'}>
              {money.format(data.financialHealth.monthlyNetFlow)}
            </dd>
            <small>
              {data.scope.financialHealthIsGlobal
                ? 'Chỉ số tổng thể của mọi danh mục'
                : 'Toàn bộ tài khoản trong phạm vi'}
            </small>
          </div>
        </dl>
        <div className="analytics-plan-links">
          <Link to="/saving-goals">Mở mục tiêu</Link>
          <Link to="/dca-plans">Mở lịch DCA</Link>
        </div>
      </aside>
    </div>
  );

  return (
    <section className="analytics-workspace">
      <div className="analytics-tabs" role="tablist" aria-label="Không gian phân tích">
        {analyticsTabs.map((tab) => (
          <button
            key={tab}
            id={`analytics-tab-${tab}`}
            type="button"
            role="tab"
            aria-selected={activeTab === tab}
            aria-controls={`analytics-panel-${tab}`}
            tabIndex={activeTab === tab ? 0 : -1}
            onClick={() => onTabChange(tab)}
            onKeyDown={(event) => handleTabKeyDown(event, tab)}
          >
            {tabLabels[tab]}
          </button>
        ))}
      </div>
      <div
        id={`analytics-panel-${activeTab}`}
        role="tabpanel"
        aria-labelledby={`analytics-tab-${activeTab}`}
        className="analytics-tab-panel"
      >
        {activeTab === 'overview' && (
          <div className="analytics-overview-stack">
            {renderPerformanceChart()}
            <div className="analytics-overview-facts">
              <article>
                <span>Lợi nhuận ghi nhận</span>
                <strong>{money.format(data.performance.realizedPnl)}</strong>
              </article>
              <article>
                <span>Lợi nhuận chưa ghi nhận</span>
                <strong>{money.format(data.performance.unrealizedPnl)}</strong>
              </article>
              <article>
                <span>Biến động tháng</span>
                <strong>
                  {data.performance.monthlyVolatilityPercentage.value === null
                    ? '—'
                    : `${data.performance.monthlyVolatilityPercentage.value.toFixed(2)}%`}
                </strong>
              </article>
            </div>
          </div>
        )}
        {activeTab === 'performance' && (
          <>
            {renderPerformanceChart(true)}
            <div className="analytics-deep-link">
              <div>
                <strong>Cần benchmark, heatmap tháng hoặc đường drawdown?</strong>
                <p>Performance Center giữ bộ công cụ chuyên sâu và giải thích phương pháp tính.</p>
              </div>
              <Link to="/analytics/performance">Mở Performance Center →</Link>
            </div>
          </>
        )}
        {activeTab === 'allocation' && renderAllocation()}
        {activeTab === 'cashflow' && renderCashflow()}
        {activeTab === 'scenario' && <ScenarioLab data={data} />}
      </div>
    </section>
  );
};
