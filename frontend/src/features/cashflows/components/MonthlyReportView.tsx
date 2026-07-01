import React, { useState } from 'react';
import { useMonthlyCashflowReport } from '../hooks/useCashflows';
import {
  BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend, ResponsiveContainer,
  LineChart, Line, PieChart, Pie, Cell
} from 'recharts';
import './MonthlyReportView.css';

export const MonthlyReportView: React.FC = () => {
  const [currency, setCurrency] = useState('VND');
  const [year, setYear] = useState(new Date().getFullYear());

  const { report, loading } = useMonthlyCashflowReport(currency, year);

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: currency,
      maximumFractionDigits: 0
    }).format(amount);
  };

  const chartData = report?.months.map(m => ({
    name: `T${m.month}`,
    Thu: m.income,
    Chi: m.expense,
    'Đầu tư': m.investment,
    'Tiết kiệm': m.saving,
    'Dòng tiền': m.netFlow
  })) || [];

  const pieData = React.useMemo(() => {
    if (!report) return [];
    return [
      { name: 'Thu nhập', value: report.yearTotalIncome || 0, fill: '#10b981' },
      { name: 'Chi tiêu', value: report.yearTotalExpense || 0, fill: '#ef4444' },
      { name: 'Đầu tư', value: report.months.reduce((sum, m) => sum + m.investment, 0), fill: '#8b5cf6' },
      { name: 'Tiết kiệm', value: report.months.reduce((sum, m) => sum + m.saving, 0), fill: '#f59e0b' }
    ].filter(item => item.value > 0);
  }, [report]);

  return (
    <div className="monthly-report-view">
      <div className="view-header glass-panel">
        <div className="header-info">
          <h2>Báo cáo Bức tranh Tài chính</h2>
          <p>Xem tổng quan thu chi, đầu tư và tiết kiệm theo năm</p>
        </div>
        <div className="filters">
          <input 
            type="number" 
            className="modern-select" 
            value={year} 
            onChange={e => setYear(Number(e.target.value))} 
            min="2020" max="2050"
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
                <h3>Tổng Thu Năm</h3>
                <p className="amount">{formatCurrency(report?.yearTotalIncome || 0)}</p>
              </div>
            </div>
            <div className="card expense-card">
              <div className="card-content">
                <h3>Tổng Chi Năm</h3>
                <p className="amount">{formatCurrency(report?.yearTotalExpense || 0)}</p>
              </div>
            </div>
            <div className="card net-card">
              <div className="card-content">
                <h3>Tích lũy Năm</h3>
                <p className={`amount ${(report?.yearNetFlow || 0) >= 0 ? 'positive' : 'negative'}`}>
                  {formatCurrency(report?.yearNetFlow || 0)}
                </p>
              </div>
            </div>
          </div>

          <div className="charts-grid">
            <div className="chart-container glass-panel">
              <h3>So sánh Thu Chi</h3>
              <div className="chart-wrapper">
                <ResponsiveContainer width="100%" height={300}>
                  <BarChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
                    <XAxis dataKey="name" stroke="#94a3b8" />
                    <YAxis stroke="#94a3b8" tickFormatter={(value) => new Intl.NumberFormat('en-US', { notation: "compact", compactDisplay: "short" }).format(value)} />
                    <Tooltip 
                      formatter={(value: any) => formatCurrency(Number(value) || 0)}
                      contentStyle={{ backgroundColor: 'rgba(15, 23, 42, 0.9)', border: 'none', borderRadius: '8px', color: '#fff' }}
                    />
                    <Legend />
                    <Bar dataKey="Thu" fill="#10b981" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="Chi" fill="#ef4444" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="Đầu tư" fill="#8b5cf6" radius={[4, 4, 0, 0]} />
                    <Bar dataKey="Tiết kiệm" fill="#f59e0b" radius={[4, 4, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </div>

            <div className="chart-container glass-panel">
              <h3>Biến động Dòng tiền</h3>
              <div className="chart-wrapper">
                <ResponsiveContainer width="100%" height={300}>
                  <LineChart data={chartData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
                    <XAxis dataKey="name" stroke="#94a3b8" />
                    <YAxis stroke="#94a3b8" tickFormatter={(value) => new Intl.NumberFormat('en-US', { notation: "compact", compactDisplay: "short" }).format(value)} />
                    <Tooltip 
                      formatter={(value: any) => formatCurrency(Number(value) || 0)}
                      contentStyle={{ backgroundColor: 'rgba(15, 23, 42, 0.9)', border: 'none', borderRadius: '8px', color: '#fff' }}
                    />
                    <Legend />
                    <Line type="monotone" dataKey="Dòng tiền" stroke="#3b82f6" strokeWidth={3} dot={{ r: 4 }} activeDot={{ r: 6 }} />
                  </LineChart>
                </ResponsiveContainer>
              </div>
            </div>

            <div className="chart-container glass-panel">
              <h3>Cơ cấu Dòng tiền</h3>
              <div className="chart-wrapper">
                <ResponsiveContainer width="100%" height={300}>
                  <PieChart>
                    <Pie
                      data={pieData}
                      dataKey="value"
                      nameKey="name"
                      cx="50%"
                      cy="50%"
                      innerRadius={60}
                      outerRadius={90}
                      paddingAngle={2}
                      stroke="none"
                    >
                      {pieData.map((entry, index) => (
                        <Cell key={`cell-${index}`} fill={entry.fill} stroke="none" />
                      ))}
                    </Pie>
                    <Tooltip 
                      formatter={(value: any, name: any) => {
                        const total = pieData.reduce((sum, item) => sum + item.value, 0);
                        const percentage = total > 0 ? ((value / total) * 100).toFixed(1) + '%' : '0%';
                        return [`${formatCurrency(value)} (${percentage})`, name];
                      }}
                      contentStyle={{ backgroundColor: 'rgba(15, 23, 42, 0.9)', border: 'none', borderRadius: '8px', color: '#fff' }}
                      itemStyle={{ color: '#e2e8f0' }}
                    />
                    <Legend wrapperStyle={{ fontSize: '12px' }} />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            </div>
          </div>

          <div className="trends-section glass-panel">
            <h3>Xu hướng Chi tiêu theo Hạng mục</h3>
            <div className="trends-list">
              {report?.categoryTrends.map(trend => (
                <div key={trend.categoryName} className="trend-item">
                  <div className="trend-header">
                    <span className="cat-icon" style={{ backgroundColor: `${trend.color}20`, color: trend.color }}>
                      {trend.icon}
                    </span>
                    <span className="cat-name">{trend.categoryName}</span>
                    <span className="trend-total">{formatCurrency(trend.monthlyAmounts.reduce((a, b) => a + b, 0))}</span>
                  </div>
                  <div className="trend-sparkline">
                    {trend.monthlyAmounts.map((amount, idx) => {
                      const max = Math.max(...trend.monthlyAmounts);
                      const height = max > 0 ? (amount / max) * 100 : 0;
                      return (
                        <div key={idx} className="sparkline-bar-container" title={`T${idx + 1}: ${formatCurrency(amount)}`}>
                          <div 
                            className="sparkline-bar" 
                            style={{ height: `${height}%`, backgroundColor: trend.color }}
                          ></div>
                        </div>
                      );
                    })}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
};
