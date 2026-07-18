import { Bar, BarChart, CartesianGrid, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';

type InvestedCapitalChartProps = {
  totalInvested: number;
  currentValue: number;
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

export function InvestedCapitalChart({ totalInvested, currentValue }: InvestedCapitalChartProps) {
  const difference = currentValue - totalInvested;
  const data = [
    { name: 'Vốn đầu tư', value: totalInvested, color: 'var(--report-chart-3)' },
    { name: 'Giá trị hiện tại', value: currentValue, color: 'var(--report-chart-2)' },
  ];

  return (
    <section className="report-capital-card glass-panel" aria-labelledby="capital-chart-title">
      <div className="report-section-heading">
        <div>
          <h2 id="capital-chart-title">Vốn và giá trị hiện tại</h2>
          <p>Khoảng cách giữa chi phí tích lũy và giá trị thị trường.</p>
        </div>
        <span className={difference >= 0 ? 'positive' : 'negative'}>
          {difference >= 0 ? '+' : ''}{formatCurrency(difference)}
        </span>
      </div>
      <div
        className="report-capital-chart"
        role="img"
        aria-label={`Vốn đầu tư ${formatCurrency(totalInvested)}, giá trị hiện tại ${formatCurrency(currentValue)}`}
      >
        <ResponsiveContainer width="100%" height="100%">
          <BarChart data={data} layout="vertical" margin={{ top: 4, right: 24, left: 8, bottom: 0 }} barSize={28}>
            <CartesianGrid strokeDasharray="3 6" stroke="var(--report-grid-line)" horizontal={false} />
            <XAxis
              type="number"
              tickFormatter={formatAxisValue}
              tick={{ fill: 'var(--text-secondary)', fontSize: 12 }}
              tickLine={false}
              axisLine={false}
            />
            <YAxis
              type="category"
              dataKey="name"
              width={112}
              tick={{ fill: 'var(--text-secondary)', fontSize: 12 }}
              tickLine={false}
              axisLine={false}
            />
            <Tooltip
              formatter={(value) => [formatCurrency(Number(value) || 0), 'Giá trị']}
              cursor={{ fill: 'var(--report-hover-surface)' }}
              contentStyle={{ background: 'var(--report-tooltip-bg)', border: '1px solid var(--glass-border)' }}
              itemStyle={{ color: 'var(--text-primary)' }}
              labelStyle={{ color: 'var(--text-secondary)' }}
              wrapperClassName="report-recharts-tooltip"
            />
            <Bar dataKey="value" radius={[0, 8, 8, 0]}>
              {data.map((item) => <Cell key={item.name} fill={item.color} />)}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      </div>
    </section>
  );
}
