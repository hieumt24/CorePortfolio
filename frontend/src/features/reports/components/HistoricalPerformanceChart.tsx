import { CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { SnapshotDto } from '../types';

type HistoricalPerformanceChartProps = {
  data: SnapshotDto[];
  isGeneratingMock: boolean;
  onGenerateMock: () => void;
};

const formatCurrency = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value);

const formatAxisValue = (value: number) =>
  new Intl.NumberFormat('vi-VN', {
    notation: 'compact',
    maximumFractionDigits: 1,
  }).format(value);

const formatDate = (dateValue: string) =>
  new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit' }).format(new Date(dateValue));

export function HistoricalPerformanceChart({
  data,
  isGeneratingMock,
  onGenerateMock,
}: HistoricalPerformanceChartProps) {
  return (
    <section className="report-history-card glass-panel" aria-labelledby="history-chart-title">
      <div className="report-section-heading">
        <div>
          <h2 id="history-chart-title">Giá trị qua thời gian</h2>
          <p>So sánh giá trị thị trường và tổng vốn theo từng snapshot.</p>
        </div>
        {data.length > 0 && <span>{data.length} snapshots</span>}
      </div>

      {data.length === 0 ? (
        <div className="report-empty-state report-history-empty">
          <span aria-hidden="true">⌁</span>
          <strong>Chưa có lịch sử để so sánh</strong>
          <p>Snapshot hằng ngày sẽ tạo nên đường xu hướng của danh mục.</p>
          <button
            className="btn btn-outline"
            type="button"
            onClick={onGenerateMock}
            disabled={isGeneratingMock}
            aria-busy={isGeneratingMock}
          >
            {isGeneratingMock ? 'Đang tạo…' : 'Tạo 30 ngày dữ liệu mẫu'}
          </button>
        </div>
      ) : (
        <div
          className="report-history-chart"
          role="img"
          aria-label="Biểu đồ đường so sánh giá trị hiện tại và vốn đã đầu tư theo thời gian"
        >
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={data} margin={{ top: 16, right: 12, left: 8, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 6" stroke="var(--report-grid-line)" vertical={false} />
              <XAxis
                dataKey="date"
                tickFormatter={formatDate}
                stroke="var(--text-muted)"
                tick={{ fill: 'var(--text-secondary)', fontSize: 12 }}
                tickLine={false}
                axisLine={false}
                minTickGap={28}
              />
              <YAxis
                tickFormatter={formatAxisValue}
                stroke="var(--text-muted)"
                tick={{ fill: 'var(--text-secondary)', fontSize: 12 }}
                tickLine={false}
                axisLine={false}
                width={54}
              />
              <Tooltip
                formatter={(value, name) => [formatCurrency(Number(value) || 0), name]}
                labelFormatter={(label) => new Date(label).toLocaleDateString('vi-VN')}
                contentStyle={{ background: 'var(--report-tooltip-bg)', border: '1px solid var(--glass-border)' }}
                itemStyle={{ color: 'var(--text-primary)' }}
                labelStyle={{ color: 'var(--text-secondary)' }}
                wrapperClassName="report-recharts-tooltip"
              />
              <Legend iconType="circle" iconSize={8} />
              <Line
                type="monotone"
                name="Giá trị hiện tại"
                dataKey="totalValue"
                stroke="var(--report-chart-2)"
                strokeWidth={3}
                dot={false}
                activeDot={{ r: 5, fill: 'var(--report-chart-2)', stroke: 'var(--bg-base)', strokeWidth: 2 }}
              />
              <Line
                type="monotone"
                name="Vốn đã đầu tư"
                dataKey="totalInvested"
                stroke="var(--report-chart-3)"
                strokeWidth={2}
                strokeDasharray="7 7"
                dot={false}
                activeDot={{ r: 4, fill: 'var(--report-chart-3)', stroke: 'var(--bg-base)', strokeWidth: 2 }}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </section>
  );
}
