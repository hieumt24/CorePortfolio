import React, { useState, useMemo } from 'react';
import { useDailyCashflowSummary } from '../hooks/useCashflows';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer
} from 'recharts';
import './DailyExpenseView.css';

export const DailyExpenseView: React.FC = () => {
  const [currency, setCurrency] = useState('VND');
  const [month, setMonth] = useState(() => {
    const d = new Date();
    return `${d.getFullYear()}-${(d.getMonth() + 1).toString().padStart(2, '0')}`;
  });

  const { summary, loading } = useDailyCashflowSummary(currency, month);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  const chartData = useMemo(() => {
    if (!summary?.days) return [];
    // The days are usually returned sorted (e.g., latest first or oldest first). We want them in chronological order for the chart.
    // Assuming they are sorted chronologically from the API or we can just sort them to be sure.
    const sortedDays = [...summary.days].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());
    return sortedDays.map(day => ({
      dateStr: new Date(day.date).getDate().toString().padStart(2, '0'),
      Thu: day.income,
      Chi: day.expense
    }));
  }, [summary]);

  return (
    <div className="daily-expense-view">
      <div className="view-header glass-panel">
        <div className="header-info">
          <h2>Thống kê Hàng Ngày</h2>
          <p>Xem chi tiết chi tiêu theo từng ngày trong tháng</p>
        </div>
        <div className="filters">
          <input 
            type="month" 
            className="modern-select" 
            value={month} 
            onChange={e => setMonth(e.target.value)} 
          />
          <select className="modern-select" value={currency} onChange={(e) => setCurrency(e.target.value)}>
            <option value="VND">VND</option>
            <option value="USD">USD</option>
          </select>
        </div>
      </div>

      {loading ? (
        <div className="loading-state"><div className="spinner"></div></div>
      ) : (
        <>
          <div className="summary-cards">
            <div className="card income-card">
              <div className="card-content">
                <h3>Tổng Thu Tháng</h3>
                <p className="amount">{formatCurrency(summary?.monthTotalIncome || 0)}</p>
              </div>
            </div>
            <div className="card expense-card">
              <div className="card-content">
                <h3>Tổng Chi Tháng</h3>
                <p className="amount">{formatCurrency(summary?.monthTotalExpense || 0)}</p>
              </div>
            </div>
            <div className="card saving-card">
              <div className="card-content">
                <h3>Trung bình Chi/Ngày</h3>
                <p className="amount">{formatCurrency(summary?.dailyAverage || 0)}</p>
              </div>
            </div>
          </div>

          {chartData.length > 0 && (
            <div className="daily-chart glass-panel" style={{ marginTop: '1.5rem', marginBottom: '1.5rem', padding: '1.5rem' }}>
              <h3 style={{ margin: '0 0 1rem 0', fontSize: '1.1rem', color: '#fff' }}>Biểu đồ Thu Chi theo ngày</h3>
              <div style={{ height: 300 }}>
                <ResponsiveContainer width="100%" height="100%">
                  <BarChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
                    <XAxis dataKey="dateStr" stroke="#94a3b8" />
                    <YAxis stroke="#94a3b8" tickFormatter={(value) => new Intl.NumberFormat('en-US', { notation: "compact", compactDisplay: "short" }).format(value)} />
                    <Tooltip 
                      formatter={(value: any) => formatCurrency(Number(value) || 0)}
                      contentStyle={{ backgroundColor: 'rgba(15, 23, 42, 0.9)', border: 'none', borderRadius: '8px', color: '#fff' }}
                    />
                    <Legend />
                    <Bar dataKey="Thu" fill="#10b981" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="Chi" fill="#ef4444" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>
          )}

          <div className="days-list glass-panel">
            {summary?.days.map((day) => {
              const isToday = new Date().toISOString().slice(0, 10) === new Date(day.date).toISOString().slice(0, 10);
              return (
                <div key={day.date} className={`day-item ${isToday ? 'today' : ''}`}>
                  <div className="day-header">
                    <div className="day-date">
                      <span className="day-number">{new Date(day.date).getDate()}</span>
                      <span className="day-name">
                        {new Date(day.date).toLocaleDateString('vi-VN', { weekday: 'short' })}
                      </span>
                    </div>
                    <div className="day-totals">
                      {day.income > 0 && <span className="positive">+{formatCurrency(day.income)}</span>}
                      {day.expense > 0 && <span className="negative">-{formatCurrency(day.expense)}</span>}
                    </div>
                  </div>
                  
                  {day.expense > 0 && (
                    <div className="day-breakdown">
                      {day.expenseBreakdown.map(cat => (
                        <div key={cat.categoryName} className="breakdown-item">
                          <span className="cat-icon" style={{ backgroundColor: `${cat.color}20`, color: cat.color }}>
                            {cat.icon}
                          </span>
                          <span className="cat-name">{cat.categoryName}</span>
                          <span className="cat-amount">{formatCurrency(cat.amount)}</span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              );
            }).reverse()}
          </div>
        </>
      )}
    </div>
  );
};
