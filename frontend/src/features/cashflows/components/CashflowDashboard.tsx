import React, { useState, useMemo } from 'react';
import { useCashflowsList, useCashflowSummary } from '../hooks/useCashflows';
import { AddCashflowModal } from './AddCashflowModal';
import { CashflowType } from '../types/cashflows';
import './CashflowDashboard.css';

const getDateRange = (filter: string) => {
  const now = new Date();
  let startDate: string | undefined;
  let endDate: string | undefined;

  switch (filter) {
    case 'thisMonth':
      startDate = new Date(now.getFullYear(), now.getMonth(), 1).toISOString();
      endDate = new Date(now.getFullYear(), now.getMonth() + 1, 0, 23, 59, 59).toISOString();
      break;
    case 'lastMonth':
      startDate = new Date(now.getFullYear(), now.getMonth() - 1, 1).toISOString();
      endDate = new Date(now.getFullYear(), now.getMonth(), 0, 23, 59, 59).toISOString();
      break;
    case 'thisYear':
      startDate = new Date(now.getFullYear(), 0, 1).toISOString();
      endDate = new Date(now.getFullYear(), 11, 31, 23, 59, 59).toISOString();
      break;
    default:
      startDate = undefined;
      endDate = undefined;
      break;
  }
  return { startDate, endDate };
};

export const CashflowDashboard: React.FC = () => {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [defaultType, setDefaultType] = useState<CashflowType>(CashflowType.Income);
  const [currency, setCurrency] = useState('VND');
  const [dateFilter, setDateFilter] = useState('thisMonth');

  const { startDate, endDate } = useMemo(() => getDateRange(dateFilter), [dateFilter]);

  const { summary, loading: isSummaryLoading, refetch: refetchSummary } = useCashflowSummary(currency, startDate, endDate);
  const { records: cashflows, loading: isCashflowsLoading, refetch: refetchCashflows } = useCashflowsList(1, 100, currency, startDate, endDate);

  const handleCashflowAdded = () => {
    setIsAddModalOpen(false);
    refetchSummary();
    refetchCashflows();
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  const formatDate = (dateStr: string) => {
    return new Date(dateStr).toLocaleDateString('vi-VN', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const handleOpenModal = (type: CashflowType) => {
    setDefaultType(type);
    setIsAddModalOpen(true);
  };

  return (
    <div className="cashflow-dashboard">
      <div className="dashboard-header">
        <div className="header-title">
          <h1>Quản lý Thu Chi</h1>
          <p className="subtitle">Theo dõi dòng tiền thông minh và trực quan</p>
        </div>
        <div className="header-actions">
          <select 
            className="modern-select"
            value={dateFilter} 
            onChange={(e) => setDateFilter(e.target.value)}
          >
            <option value="all">Tất cả thời gian</option>
            <option value="thisMonth">Tháng này</option>
            <option value="lastMonth">Tháng trước</option>
            <option value="thisYear">Năm nay</option>
          </select>
          <select 
            className="modern-select"
            value={currency} 
            onChange={(e) => setCurrency(e.target.value)}
          >
            <option value="VND">VND</option>
            <option value="USD">USD</option>
          </select>
          <button className="btn btn-income" onClick={() => handleOpenModal(CashflowType.Income)}>
            + Thêm Thu
          </button>
          <button className="btn btn-expense" onClick={() => handleOpenModal(CashflowType.Expense)}>
            - Thêm Chi
          </button>
        </div>
      </div>

      <div className="summary-cards">
        <div className="card income-card">
          <div className="card-icon">↓</div>
          <div className="card-content">
            <h3>Tổng Thu Nhập</h3>
            <p className="amount">{isSummaryLoading ? '...' : formatCurrency(summary?.totalIncome || 0)}</p>
          </div>
        </div>
        <div className="card expense-card">
          <div className="card-icon">↑</div>
          <div className="card-content">
            <h3>Tổng Chi Tiêu</h3>
            <p className="amount">{isSummaryLoading ? '...' : formatCurrency(summary?.totalExpense || 0)}</p>
          </div>
        </div>
        <div className="card net-card">
          <div className="card-icon">≈</div>
          <div className="card-content">
            <h3>Dòng Tiền Thuần</h3>
            <p className={`amount ${summary?.netFlow && summary.netFlow < 0 ? 'negative' : 'positive'}`}>
              {isSummaryLoading ? '...' : formatCurrency(summary?.netFlow || 0)}
            </p>
          </div>
        </div>
      </div>

      <div className="dashboard-grid">
        <div className="history-section">
          <div className="section-header">
            <h2>Lịch sử Giao dịch</h2>
          </div>
          {isCashflowsLoading ? (
            <div className="loading-state">
               <div className="spinner"></div>
               <p>Đang tải dữ liệu...</p>
            </div>
          ) : (
            <div className="transactions-list">
              {cashflows?.length === 0 && (
                <div className="empty-state">
                  <div className="empty-icon">📝</div>
                  <p>Chưa có giao dịch nào trong thời gian này.</p>
                </div>
              )}
              {cashflows?.map((record) => (
                <div key={record.id} className="transaction-item">
                  <div className="transaction-icon" style={{ backgroundColor: `${record.categoryColor}20`, color: record.categoryColor }}>
                    {record.categoryIcon}
                  </div>
                  <div className="transaction-details">
                    <h4>{record.categoryName}</h4>
                    <div className="meta-info">
                      <span className="portfolio-tag">{record.portfolioName}</span>
                      <span className="date-tag">{formatDate(record.date)}</span>
                    </div>
                    {record.description && <p className="description">{record.description}</p>}
                  </div>
                  <div className={`transaction-amount ${record.type === CashflowType.Income ? 'positive' : 'negative'}`}>
                    {record.type === CashflowType.Income ? '+' : '-'} {formatCurrency(record.amount)}
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="charts-section">
          <div className="chart-card">
            <h2>Phân bổ Chi Tiêu</h2>
            {isSummaryLoading ? (
              <div className="loading-state"><div className="spinner"></div></div>
            ) : (
              <div className="category-bars">
                {summary?.expenseByCategory.length === 0 && <p className="empty-state">Không có dữ liệu chi tiêu.</p>}
                {summary?.expenseByCategory.map((cat, idx) => {
                  const percentage = summary.totalExpense > 0 ? (cat.amount / summary.totalExpense) * 100 : 0;
                  return (
                    <div key={idx} className="category-bar-item">
                      <div className="cat-info">
                        <span className="cat-name">
                          <span className="cat-icon-small" style={{ backgroundColor: `${cat.color}20`, color: cat.color }}>{cat.icon}</span> 
                          {cat.categoryName}
                        </span>
                        <div className="cat-stats">
                          <span className="amount-text">{formatCurrency(cat.amount)}</span>
                          <span className="percent-badge">{percentage.toFixed(1)}%</span>
                        </div>
                      </div>
                      <div className="progress-bg">
                        <div 
                          className="progress-fill" 
                          style={{ width: `${percentage}%`, backgroundColor: cat.color, boxShadow: `0 0 10px ${cat.color}80` }}
                        ></div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
          
          <div className="chart-card mt-4">
            <h2>Nguồn Thu Nhập</h2>
            {isSummaryLoading ? (
              <div className="loading-state"><div className="spinner"></div></div>
            ) : (
              <div className="category-bars">
                {summary?.incomeByCategory.length === 0 && <p className="empty-state">Không có dữ liệu thu nhập.</p>}
                {summary?.incomeByCategory.map((cat, idx) => {
                  const percentage = summary.totalIncome > 0 ? (cat.amount / summary.totalIncome) * 100 : 0;
                  return (
                    <div key={idx} className="category-bar-item">
                      <div className="cat-info">
                        <span className="cat-name">
                          <span className="cat-icon-small" style={{ backgroundColor: `${cat.color}20`, color: cat.color }}>{cat.icon}</span> 
                          {cat.categoryName}
                        </span>
                        <div className="cat-stats">
                          <span className="amount-text">{formatCurrency(cat.amount)}</span>
                          <span className="percent-badge">{percentage.toFixed(1)}%</span>
                        </div>
                      </div>
                      <div className="progress-bg">
                        <div 
                          className="progress-fill" 
                          style={{ width: `${percentage}%`, backgroundColor: cat.color, boxShadow: `0 0 10px ${cat.color}80` }}
                        ></div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </div>

      {isAddModalOpen && (
        <AddCashflowModal 
          defaultType={defaultType} 
          onClose={handleCashflowAdded} 
        />
      )}
    </div>
  );
};
