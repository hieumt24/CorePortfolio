import React, { useEffect, useState } from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend
} from 'recharts';
import { getGlobalHistory, mockGlobalHistory } from '../api/reportsApi';
import type { SnapshotDto } from '../types';
import { useNotification } from '../../../context/NotificationContext';

export const HistoricalPerformanceChart: React.FC = () => {
  const [data, setData] = useState<SnapshotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const { showNotification } = useNotification();

  const fetchHistory = async () => {
    try {
      setLoading(true);
      const history = await getGlobalHistory();
      setData(history);
    } catch (error) {
      console.error('Failed to fetch history:', error);
      showNotification('Failed to fetch history data', 'error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchHistory();
  }, []);

  const handleGenerateMock = async () => {
    try {
      await mockGlobalHistory();
      showNotification('Mock data generated', 'success');
      fetchHistory();
    } catch (error) {
      console.error('Failed to generate mock data', error);
      showNotification('Failed to generate mock data', 'error');
    }
  };

  const formatCurrency = (value: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND',
      maximumFractionDigits: 0
    }).format(value);
  };

  const formatDate = (dateStr: string) => {
    const d = new Date(dateStr);
    return `${d.getDate()}/${d.getMonth() + 1}`;
  };

  if (loading) {
    return (
      <div className="chart-wrapper glass-panel" style={{ marginTop: '2rem' }}>
        <h2>Historical Performance</h2>
        <p>Loading chart data...</p>
      </div>
    );
  }

  return (
    <div className="chart-wrapper glass-panel" style={{ marginTop: '2rem' }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '1.5rem' }}>
        <h2>Historical Performance</h2>
        {data.length === 0 && (
          <button className="btn-secondary" style={{ padding: '0.5rem 1rem' }} onClick={handleGenerateMock}>
            Generate Mock Data (30 days)
          </button>
        )}
      </div>

      {data.length === 0 ? (
        <p>No historical data available yet. Snapshots are taken daily.</p>
      ) : (
        <div style={{ height: '400px', width: '100%' }}>
          <ResponsiveContainer>
            <LineChart
              data={data}
              margin={{ top: 20, right: 30, left: 40, bottom: 10 }}
            >
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" vertical={false} />
              <XAxis 
                dataKey="date" 
                tickFormatter={formatDate}
                stroke="#94a3b8"
                tick={{ fill: '#94a3b8' }}
              />
              <YAxis 
                tickFormatter={(value) => {
                  if (value >= 1000000000) return (value / 1000000000).toFixed(1) + 'B';
                  if (value >= 1000000) return (value / 1000000).toFixed(0) + 'M';
                  return value.toString();
                }}
                stroke="#94a3b8"
                tick={{ fill: '#94a3b8' }}
              />
              <Tooltip 
                formatter={(value: any) => [formatCurrency(Number(value) || 0), '']}
                labelFormatter={(label) => new Date(label).toLocaleDateString('vi-VN')}
                contentStyle={{ 
                  backgroundColor: 'transparent',
                  border: 'none'
                }}
                wrapperClassName="custom-tooltip"
                itemStyle={{ color: '#ffffff' }}
              />
              <Legend />
              <Line 
                type="monotone" 
                name="Total Value"
                dataKey="totalValue" 
                stroke="#3b82f6" 
                strokeWidth={3}
                dot={false}
                activeDot={{ r: 6, fill: '#3b82f6', stroke: '#1e293b', strokeWidth: 2 }}
              />
              <Line 
                type="monotone" 
                name="Total Invested"
                dataKey="totalInvested" 
                stroke="#10b981" 
                strokeWidth={2}
                strokeDasharray="5 5"
                dot={false}
              />
            </LineChart>
          </ResponsiveContainer>
        </div>
      )}
    </div>
  );
};
