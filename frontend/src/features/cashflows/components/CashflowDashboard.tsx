import React, { useState, useMemo } from 'react';
import { useCashflowsList, useCashflowSummary } from '../hooks/useCashflows';
import { AddCashflowModal } from './AddCashflowModal';
import { CashflowType } from '../types/cashflows';
import type { CashflowRecord } from '../types/cashflows';
import { cashflowsApi } from '../api/cashflowsApi';
import { useNotification } from '../../../context/NotificationContext';
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
  const { showNotification } = useNotification();
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [cashflowToEdit, setCashflowToEdit] = useState<CashflowRecord | null>(null);
  const [defaultType, setDefaultType] = useState<CashflowType>(CashflowType.Income);
  
  // API Filters
  const [currency, setCurrency] = useState('VND');
  const [dateFilter, setDateFilter] = useState('thisMonth');

  // Local Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [categoryFilter, setCategoryFilter] = useState<string>('all');
  const [showCharts, setShowCharts] = useState(false);

  const { startDate, endDate } = useMemo(() => getDateRange(dateFilter), [dateFilter]);

  const { summary, loading: isSummaryLoading, refetch: refetchSummary } = useCashflowSummary(currency, startDate, endDate);
  const { records: cashflows, loading: isCashflowsLoading, refetch: refetchCashflows } = useCashflowsList(1, 500, currency, startDate, endDate);

  const handleCashflowAdded = () => {
    setIsAddModalOpen(false);
    setIsEditModalOpen(false);
    setCashflowToEdit(null);
    refetchSummary();
    refetchCashflows();
  };

  const handleEdit = (record: CashflowRecord) => {
    setCashflowToEdit(record);
    setDefaultType(record.type);
    setIsEditModalOpen(true);
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa giao dịch này?')) return;
    try {
      await cashflowsApi.deleteCashflow(id);
      showNotification('Xóa giao dịch thành công!', 'success');
      refetchSummary();
      refetchCashflows();
    } catch (error) {
      console.error(error);
      showNotification('Đã có lỗi xảy ra khi xóa giao dịch.', 'error');
    }
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

  // Extract unique categories for local filter
  const availableCategories = useMemo(() => {
    if (!cashflows) return [];
    const cats = new Set<string>();
    cashflows.forEach(c => cats.add(c.categoryName));
    return Array.from(cats).sort();
  }, [cashflows]);

  // Apply local filtering
  const filteredCashflows = useMemo(() => {
    if (!cashflows) return [];
    return cashflows.filter(c => {
      if (searchQuery) {
        const q = searchQuery.toLowerCase();
        if (!c.description?.toLowerCase().includes(q) && 
            !c.categoryName.toLowerCase().includes(q) &&
            !c.portfolioName.toLowerCase().includes(q)) {
          return false;
        }
      }
      if (typeFilter !== 'all') {
        if (typeFilter === 'income' && c.type !== CashflowType.Income) return false;
        if (typeFilter === 'expense' && c.type !== CashflowType.Expense) return false;
      }
      if (categoryFilter !== 'all' && c.categoryName !== categoryFilter) {
        return false;
      }
      return true;
    });
  }, [cashflows, searchQuery, typeFilter, categoryFilter]);

  return (
    <div className="cashflow-dashboard">
      {/* Header Area */}
      <div className="dashboard-header-premium">
        <div className="header-info">
          <h1>Quản lý Thu Chi</h1>
          <p>Theo dõi dòng tiền thông minh và trực quan</p>
        </div>
        <div className="header-actions">
          <div className="global-filters">
            <select className="modern-select" value={dateFilter} onChange={(e) => setDateFilter(e.target.value)}>
              <option value="all">Tất cả thời gian</option>
              <option value="thisMonth">Tháng này</option>
              <option value="lastMonth">Tháng trước</option>
              <option value="thisYear">Năm nay</option>
            </select>
            <select className="modern-select" value={currency} onChange={(e) => setCurrency(e.target.value)}>
              <option value="VND">VND</option>
              <option value="USD">USD</option>
            </select>
          </div>
          <div className="action-buttons">
            <button className="btn btn-income glow-effect" onClick={() => handleOpenModal(CashflowType.Income)}>
              + Thêm Thu
            </button>
            <button className="btn btn-expense glow-effect" onClick={() => handleOpenModal(CashflowType.Expense)}>
              - Thêm Chi
            </button>
          </div>
        </div>
      </div>

      {/* Summary Cards Area */}
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

      <div className="main-content-area">
        {/* Advanced Filters Bar */}
        <div className="advanced-filters-bar glass-panel">
          <div className="filter-group search-group">
            <span className="filter-icon">🔍</span>
            <input 
              type="text" 
              placeholder="Tìm kiếm giao dịch (Mô tả, Danh mục, Ví)..." 
              className="search-input"
              value={searchQuery}
              onChange={e => setSearchQuery(e.target.value)}
            />
          </div>
          <div className="filter-group">
            <select className="filter-select" value={typeFilter} onChange={e => setTypeFilter(e.target.value)}>
              <option value="all">Tất cả loại giao dịch</option>
              <option value="income">Thu nhập</option>
              <option value="expense">Chi tiêu</option>
            </select>
          </div>
          <div className="filter-group">
            <select className="filter-select" value={categoryFilter} onChange={e => setCategoryFilter(e.target.value)}>
              <option value="all">Tất cả danh mục</option>
              {availableCategories.map(c => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
          </div>
          <div className="filter-group">
            <button className={`btn-toggle-charts ${showCharts ? 'active' : ''}`} onClick={() => setShowCharts(!showCharts)}>
              {showCharts ? 'Ẩn Biểu đồ' : '📊 Xem Biểu đồ'}
            </button>
          </div>
        </div>

        <div className={`dashboard-body ${showCharts ? 'with-charts' : 'list-only'}`}>
          {/* History List */}
          <div className="history-section glass-panel">
            <div className="section-header border-bottom">
              <h2>Lịch sử Giao dịch ({filteredCashflows.length})</h2>
            </div>
            {isCashflowsLoading ? (
              <div className="loading-state">
                 <div className="spinner"></div>
                 <p>Đang tải dữ liệu...</p>
              </div>
            ) : (
              <div className="transactions-list ledger-view">
                {filteredCashflows.length === 0 && (
                  <div className="empty-state">
                    <div className="empty-icon">📝</div>
                    <p>Không tìm thấy giao dịch nào phù hợp.</p>
                  </div>
                )}
                {filteredCashflows.map((record) => (
                  <div key={record.id} className="ledger-item">
                    <div className="ledger-date">
                      <div className="date-main">{new Date(record.date).toLocaleDateString('vi-VN', { day: '2-digit', month: 'short' })}</div>
                      <div className="time-sub">{new Date(record.date).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</div>
                    </div>
                    
                    <div className="ledger-icon" style={{ backgroundColor: `${record.categoryColor}20`, color: record.categoryColor }}>
                      {record.categoryIcon}
                    </div>
                    
                    <div className="ledger-details">
                      <div className="ledger-title-row">
                        <h4>{record.categoryName}</h4>
                        <span className="portfolio-badge">{record.portfolioName}</span>
                      </div>
                      {record.description && <p className="ledger-desc">{record.description}</p>}
                    </div>
                    
                    <div className={`ledger-amount ${record.type === CashflowType.Income ? 'positive' : 'negative'}`}>
                      {record.type === CashflowType.Income ? '+' : '-'} {formatCurrency(record.amount)}
                    </div>
                    
                    <div className="ledger-actions">
                      <button className="icon-action-btn edit" onClick={() => handleEdit(record)} title="Sửa">✏️</button>
                      <button className="icon-action-btn delete" onClick={() => handleDelete(record.id)} title="Xóa">🗑️</button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Charts Area - Collapsible */}
          {showCharts && (
            <div className="charts-sidebar">
              <div className="chart-card glass-panel">
                <h2>Phân bổ Chi Tiêu</h2>
                {isSummaryLoading ? (
                  <div className="loading-state"><div className="spinner"></div></div>
                ) : (
                  <div className="category-bars">
                    {summary?.expenseByCategory.length === 0 && <p className="empty-state">Không có dữ liệu.</p>}
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
                            </div>
                          </div>
                          <div className="progress-bg">
                            <div className="progress-fill" style={{ width: `${percentage}%`, backgroundColor: cat.color }}></div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
              
              <div className="chart-card glass-panel mt-4">
                <h2>Nguồn Thu Nhập</h2>
                {isSummaryLoading ? (
                  <div className="loading-state"><div className="spinner"></div></div>
                ) : (
                  <div className="category-bars">
                    {summary?.incomeByCategory.length === 0 && <p className="empty-state">Không có dữ liệu.</p>}
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
                            </div>
                          </div>
                          <div className="progress-bg">
                            <div className="progress-fill" style={{ width: `${percentage}%`, backgroundColor: cat.color }}></div>
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>
            </div>
          )}
        </div>
      </div>

      {isAddModalOpen && (
        <AddCashflowModal 
          defaultType={defaultType} 
          onClose={handleCashflowAdded} 
        />
      )}
      
      {isEditModalOpen && cashflowToEdit && (
        <AddCashflowModal 
          defaultType={defaultType} 
          onClose={handleCashflowAdded} 
          cashflowToEdit={cashflowToEdit}
        />
      )}
    </div>
  );
};
