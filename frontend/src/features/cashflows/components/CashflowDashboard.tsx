import React, { useState } from 'react';
import { useCashflowsList, useCashflowSummary } from '../hooks/useCashflows';
import { AddCashflowModal } from './AddCashflowModal';
import { CashflowType } from '../types/cashflows';
import './CashflowDashboard.css';

export const CashflowDashboard: React.FC = () => {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [defaultType, setDefaultType] = useState<CashflowType>(CashflowType.Income);
  const [currency, setCurrency] = useState('VND');

  const { summary, loading: isSummaryLoading, refetch: refetchSummary } = useCashflowSummary(currency);
  const { records: cashflows, loading: isCashflowsLoading, refetch: refetchCashflows } = useCashflowsList(1, 50, currency);

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
        <h1>Quản lý Thu Chi</h1>
        <div className="header-actions">
          <select 
            className="currency-selector"
            value={currency} 
            onChange={(e) => setCurrency(e.target.value)}
          >
            <option value="VND">VND</option>
            <option value="USD">USD</option>
          </select>
          <button className="btn btn-income" onClick={() => handleOpenModal(CashflowType.Income)}>
            + Thêm Thu nhập
          </button>
          <button className="btn btn-expense" onClick={() => handleOpenModal(CashflowType.Expense)}>
            - Thêm Chi tiêu
          </button>
        </div>
      </div>

      <div className="summary-cards">
        <div className="card income-card">
          <h3>Tổng Thu</h3>
          <p className="amount">{isSummaryLoading ? '...' : formatCurrency(summary?.totalIncome || 0)}</p>
        </div>
        <div className="card expense-card">
          <h3>Tổng Chi</h3>
          <p className="amount">{isSummaryLoading ? '...' : formatCurrency(summary?.totalExpense || 0)}</p>
        </div>
        <div className="card net-card">
          <h3>Dòng Tiền Thuần</h3>
          <p className={`amount ${summary?.netFlow && summary.netFlow < 0 ? 'negative' : 'positive'}`}>
            {isSummaryLoading ? '...' : formatCurrency(summary?.netFlow || 0)}
          </p>
        </div>
      </div>

      <div className="dashboard-grid">
        <div className="history-section">
          <h2>Lịch sử Giao dịch</h2>
          {isCashflowsLoading ? (
            <p>Đang tải...</p>
          ) : (
            <div className="transactions-list">
              {cashflows?.length === 0 && <p className="empty-state">Chưa có giao dịch nào.</p>}
              {cashflows?.map((record) => (
                <div key={record.id} className="transaction-item">
                  <div className="transaction-icon" style={{ background: record.categoryColor }}>
                    {record.categoryIcon}
                  </div>
                  <div className="transaction-details">
                    <h4>{record.categoryName}</h4>
                    <p>{record.portfolioName} • {formatDate(record.date)}</p>
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
            <h2>Chi tiêu theo Danh mục</h2>
            {isSummaryLoading ? (
              <p>Đang tải...</p>
            ) : (
              <div className="category-bars">
                {summary?.expenseByCategory.length === 0 && <p className="empty-state">Chưa có dữ liệu chi tiêu.</p>}
                {summary?.expenseByCategory.map((cat, idx) => {
                  const percentage = summary.totalExpense > 0 ? (cat.amount / summary.totalExpense) * 100 : 0;
                  return (
                    <div key={idx} className="category-bar-item">
                      <div className="cat-info">
                        <span className="cat-name">
                          <span className="cat-icon-small" style={{ backgroundColor: `${cat.color}33`, color: cat.color }}>{cat.icon}</span> 
                          {cat.categoryName}
                        </span>
                        <span>{formatCurrency(cat.amount)} ({percentage.toFixed(1)}%)</span>
                      </div>
                      <div className="progress-bg">
                        <div 
                          className="progress-fill" 
                          style={{ width: `${percentage}%`, backgroundColor: cat.color }}
                        ></div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
          
          <div className="chart-card mt-4">
            <h2>Thu nhập theo Danh mục</h2>
            {isSummaryLoading ? (
              <p>Đang tải...</p>
            ) : (
              <div className="category-bars">
                {summary?.incomeByCategory.length === 0 && <p className="empty-state">Chưa có dữ liệu thu nhập.</p>}
                {summary?.incomeByCategory.map((cat, idx) => {
                  const percentage = summary.totalIncome > 0 ? (cat.amount / summary.totalIncome) * 100 : 0;
                  return (
                    <div key={idx} className="category-bar-item">
                      <div className="cat-info">
                        <span className="cat-name">
                          <span className="cat-icon-small" style={{ backgroundColor: `${cat.color}33`, color: cat.color }}>{cat.icon}</span> 
                          {cat.categoryName}
                        </span>
                        <span>{formatCurrency(cat.amount)} ({percentage.toFixed(1)}%)</span>
                      </div>
                      <div className="progress-bg">
                        <div 
                          className="progress-fill" 
                          style={{ width: `${percentage}%`, backgroundColor: cat.color }}
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
