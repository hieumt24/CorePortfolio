import React, { useState } from 'react';
import { useDailyCashflowSummary } from '../hooks/useCashflows';
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
