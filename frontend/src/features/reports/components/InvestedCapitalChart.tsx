import React from 'react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  ResponsiveContainer
} from 'recharts';

interface InvestedCapitalChartProps {
  totalInvested: number;
  currentValue: number;
}

export const InvestedCapitalChart: React.FC<InvestedCapitalChartProps> = ({ totalInvested, currentValue }) => {
  const data = [
    {
      name: 'Portfolio Value',
      'Total Invested': totalInvested,
      'Current Value': currentValue,
    }
  ];

  const formatCurrency = (value: number) => {
    if (value >= 1000000000) return (value / 1000000000).toFixed(1) + 'B';
    if (value >= 1000000) return (value / 1000000).toFixed(0) + 'M';
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0
    }).format(value);
  };

  const renderTooltip = ({ active, payload }: any) => {
    if (active && payload && payload.length) {
      return (
        <div className="custom-tooltip glass-panel" style={{ padding: '10px', fontSize: '14px', backgroundColor: '#1e293b', border: '1px solid #334155', borderRadius: '8px' }}>
          {payload.map((entry: any, index: number) => (
            <p key={`item-${index}`} style={{ margin: '4px 0', color: entry.color, fontWeight: 'bold' }}>
              {entry.name}: {new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(entry.value)}
            </p>
          ))}
        </div>
      );
    }
    return null;
  };

  return (
    <div className="chart-wrapper glass-panel">
      <h2>Invested vs Current Value</h2>
      <div style={{ width: '100%', height: 350 }}>
        <ResponsiveContainer>
          <BarChart
            data={data}
            margin={{ top: 20, right: 30, left: 40, bottom: 5 }}
            barSize={60}
          >
            <CartesianGrid strokeDasharray="3 3" stroke="#334155" vertical={false} />
            <XAxis dataKey="name" stroke="#94a3b8" tick={{ fill: '#94a3b8' }} />
            <YAxis tickFormatter={formatCurrency} stroke="#94a3b8" tick={{ fill: '#94a3b8' }} />
            <Tooltip content={renderTooltip} cursor={{ fill: 'rgba(255,255,255,0.05)' }} />
            <Legend />
            <Bar dataKey="Total Invested" fill="#10b981" radius={[4, 4, 0, 0]} />
            <Bar dataKey="Current Value" fill="#3b82f6" radius={[4, 4, 0, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
};
