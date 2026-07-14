import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { useCashflowsList, useCashflowSummary } from '../hooks/useCashflows';
import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Legend } from 'recharts';
import { AddCashflowModal } from './AddCashflowModal';
import { DailyExpenseView } from './DailyExpenseView';
import { MonthlyReportView } from './MonthlyReportView';
import { CashflowType } from '../types/cashflows';
import type { CashflowRecord } from '../types/cashflows';
import { cashflowsApi } from '../api/cashflowsApi';
import { getBudgetsProgress, type BudgetProgress } from '../../budgets/api/budgetApi';
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

const getBudgetMonthFromFilter = (filter: string) => {
  const now = new Date();
  const targetDate = filter === 'lastMonth'
    ? new Date(now.getFullYear(), now.getMonth() - 1, 1)
    : new Date(now.getFullYear(), now.getMonth(), 1);

  return { year: targetDate.getFullYear(), month: targetDate.getMonth() + 1 };
};

type TabType = 'overview' | 'daily' | 'monthly';

const getBudgetToneLabel = (budget: BudgetProgress) => {
  if (budget.isExceeded) return 'Vượt budget';
  if (budget.rawProgressPercentage >= 80) return 'Gần chạm budget';
  return 'Trong budget';
};

export const CashflowDashboard: React.FC = () => {
  const { showNotification } = useNotification();
  const [activeTab, setActiveTab] = useState<TabType>('overview');
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
  const [budgetProgress, setBudgetProgress] = useState<BudgetProgress[]>([]);
  const [isBudgetLoading, setIsBudgetLoading] = useState(false);
  const budgetAlertKeyRef = useRef('');

  const { startDate, endDate } = useMemo(() => getDateRange(dateFilter), [dateFilter]);
  const budgetMonth = useMemo(() => getBudgetMonthFromFilter(dateFilter), [dateFilter]);

  const { summary, loading: isSummaryLoading, refetch: refetchSummary } = useCashflowSummary(currency, startDate, endDate);
  const { records: cashflows, loading: isCashflowsLoading, refetch: refetchCashflows } = useCashflowsList(1, 500, currency, startDate, endDate);

  const refetchBudgets = useCallback(async () => {
    try {
      setIsBudgetLoading(true);
      setBudgetProgress(await getBudgetsProgress({ year: budgetMonth.year, month: budgetMonth.month, currency }));
    } catch (error) {
      console.error('Failed to load budget progress', error);
    } finally {
      setIsBudgetLoading(false);
    }
  }, [budgetMonth.month, budgetMonth.year, currency]);

  useEffect(() => {
    refetchBudgets();
  }, [refetchBudgets]);

  const budgetSummary = useMemo(() => {
    const totalLimit = budgetProgress.reduce((sum, budget) => sum + budget.monthlyLimit, 0);
    const totalSpent = budgetProgress.reduce((sum, budget) => sum + budget.spentAmount, 0);
    const progress = totalLimit > 0 ? Math.min((totalSpent / totalLimit) * 100, 100) : 0;
    return { totalLimit, totalSpent, progress };
  }, [budgetProgress]);

  const budgetRiskCounts = useMemo(() => {
    const exceeded = budgetProgress.filter(budget => budget.isExceeded).length;
    const warning = budgetProgress.filter(budget => !budget.isExceeded && budget.rawProgressPercentage >= 80).length;
    return { exceeded, warning };
  }, [budgetProgress]);

  const exceededBudgets = useMemo(
    () => budgetProgress.filter(budget => budget.isExceeded),
    [budgetProgress]
  );

  const budgetByCategoryName = useMemo(
    () => new Map(budgetProgress.map(budget => [budget.categoryName, budget])),
    [budgetProgress]
  );

  useEffect(() => {
    if (exceededBudgets.length === 0) return;
    const alertKey = `${budgetMonth.year}-${budgetMonth.month}-${currency}-${exceededBudgets.map(b => b.categoryId).join('|')}`;
    if (budgetAlertKeyRef.current === alertKey) return;
    budgetAlertKeyRef.current = alertKey;
    showNotification(`Có ${exceededBudgets.length} danh mục đã vượt ngân sách tháng ${budgetMonth.month}/${budgetMonth.year}.`, 'error');
  }, [budgetMonth.month, budgetMonth.year, currency, exceededBudgets, showNotification]);

  const handleCashflowAdded = () => {
    setIsAddModalOpen(false);
    setIsEditModalOpen(false);
    setCashflowToEdit(null);
    refetchSummary();
    refetchCashflows();
    refetchBudgets();
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
      refetchBudgets();
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
          <div className="dashboard-tabs">
            <button className={`tab-btn ${activeTab === 'overview' ? 'active' : ''}`} onClick={() => setActiveTab('overview')}>Tổng quan</button>
            <button className={`tab-btn ${activeTab === 'daily' ? 'active' : ''}`} onClick={() => setActiveTab('daily')}>Ngày</button>
            <button className={`tab-btn ${activeTab === 'monthly' ? 'active' : ''}`} onClick={() => setActiveTab('monthly')}>Tháng</button>
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

      {activeTab === 'daily' && <DailyExpenseView />}
      {activeTab === 'monthly' && <MonthlyReportView />}

      {activeTab === 'overview' && (
        <>
          {/* Summary Cards Area */}
          <div className="summary-cards">
            <div className="cf-card income-card">
              <div className="card-icon">↓</div>
              <div className="card-content">
                <h3>Tổng Thu Nhập</h3>
                <p className="amount">{isSummaryLoading ? '...' : formatCurrency(summary?.totalIncome || 0)}</p>
              </div>
            </div>
            <div className="cf-card expense-card">
              <div className="card-icon">↑</div>
              <div className="card-content">
                <h3>Tổng Chi Tiêu</h3>
                <p className="amount">{isSummaryLoading ? '...' : formatCurrency(summary?.totalExpense || 0)}</p>
              </div>
            </div>
            <div className="cf-card investment-card">
              <div className="card-icon">📈</div>
              <div className="card-content">
                <h3>Tổng Đầu Tư</h3>
                <p className="amount">{isSummaryLoading ? '...' : formatCurrency(summary?.totalInvestment || 0)}</p>
              </div>
            </div>
            <div className="cf-card saving-card">
              <div className="card-icon">🏦</div>
              <div className="card-content">
                <h3>Tổng Tiết Kiệm</h3>
                <p className="amount">{isSummaryLoading ? '...' : formatCurrency(summary?.totalSaving || 0)}</p>
              </div>
            </div>
            <div className="cf-card net-card">
              <div className="card-icon">≈</div>
              <div className="card-content">
                <h3>Dòng Tiền Thuần</h3>
                <p className={`amount ${summary?.netFlow && summary.netFlow < 0 ? 'negative' : 'positive'}`}>
                  {isSummaryLoading ? '...' : formatCurrency(summary?.netFlow || 0)}
                </p>
              </div>
            </div>
            <div className="cf-card cash-card">
              <div className="card-icon">💵</div>
              <div className="card-content">
                <h3>Tiền mặt</h3>
                <p className={`amount ${((summary?.totalIncome || 0) - (summary?.totalExpense || 0) - (summary?.totalInvestment || 0) - (summary?.totalSaving || 0)) < 0 ? 'negative' : 'positive'}`}>
                  {isSummaryLoading ? '...' : formatCurrency((summary?.totalIncome || 0) - (summary?.totalExpense || 0) - (summary?.totalInvestment || 0) - (summary?.totalSaving || 0))}
                </p>
              </div>
            </div>
          </div>

          <div className={`cashflow-budget-panel glass-panel ${exceededBudgets.length > 0 ? 'has-alert' : ''}`}>
            <div className="cashflow-budget-header">
              <div>
                <span className="budget-kicker">Budget linked</span>
                <h2>Ngân sách tháng {budgetMonth.month}/{budgetMonth.year}</h2>
                <p>Theo dõi mục tiêu chi tiêu theo category ngay trong Cashflow.</p>
              </div>
              <div className="budget-panel-actions">
                <div className="budget-total-meter">
                  <span>{isBudgetLoading ? '...' : `${budgetSummary.progress.toFixed(1)}%`}</span>
                  <small>{formatCurrency(budgetSummary.totalSpent)} / {formatCurrency(budgetSummary.totalLimit)}</small>
                </div>
                <Link className="btn btn-outline btn-sm" to="/budgets">Mở Budgets</Link>
              </div>
            </div>

            {exceededBudgets.length > 0 && (
              <div className="budget-alert-banner">
                <strong>Vượt ngân sách</strong>
                <span>{exceededBudgets.map(budget => budget.categoryName).join(', ')} đã vượt hạn mức tháng này.</span>
              </div>
            )}

            {budgetProgress.length === 0 ? (
              <div className="budget-empty-inline">Chưa có budget nào cho các category chi tiêu.</div>
            ) : (
              <>
              <div className="budget-health-row">
                <span>{budgetProgress.length} budget đang theo dõi</span>
                <span>{budgetRiskCounts.warning} cần chú ý</span>
                <span>{budgetRiskCounts.exceeded} vượt mức</span>
              </div>
              <div className="cashflow-budget-grid">
                {budgetProgress.map(budget => (
                  <div key={budget.id} className={`cashflow-budget-card ${budget.alertLevel.toLowerCase()}`}>
                    <div className="budget-card-title">
                      <span style={{ color: budget.categoryColor }}>{budget.categoryIcon || budget.categoryName.slice(0, 1)}</span>
                      <div>
                        <strong>{budget.categoryName}</strong>
                        <small>{budget.rawProgressPercentage.toFixed(1)}% budget</small>
                      </div>
                    </div>
                    <div className="budget-card-values">
                      <span>{formatCurrency(budget.spentAmount)}</span>
                      <span>{formatCurrency(budget.monthlyLimit)}</span>
                    </div>
                    <div className="budget-mini-track">
                      <div style={{ width: `${Math.min(budget.rawProgressPercentage, 100)}%` }} />
                    </div>
                    {budget.isExceeded && (
                      <small className="budget-over-text">Vượt {formatCurrency(budget.spentAmount - budget.monthlyLimit)}</small>
                    )}
                  </div>
                ))}
              </div>
              </>
            )}
          </div>

          <div className="main-content-area">
            {/* Advanced Filters Bar */}
            <div className="cf-filters-bar glass-panel">
              <div className="cf-filter-group">
                <select className="cf-select" value={dateFilter} onChange={(e) => setDateFilter(e.target.value)}>
                  <option value="all">Tất cả thời gian</option>
                  <option value="thisMonth">Tháng này</option>
                  <option value="lastMonth">Tháng trước</option>
                  <option value="thisYear">Năm nay</option>
                </select>
                <select className="cf-select" value={currency} onChange={(e) => setCurrency(e.target.value)}>
                  <option value="VND">VND</option>
                  <option value="USD">USD</option>
                </select>
              </div>
              <div className="cf-filter-group cf-search-group">
                <span className="cf-filter-icon">🔍</span>
                <input 
                  type="text" 
                  placeholder="Tìm kiếm giao dịch..." 
                  className="cf-search-input"
                  value={searchQuery}
                  onChange={e => setSearchQuery(e.target.value)}
                />
              </div>
              <div className="cf-filter-group">
                <select className="cf-select" value={typeFilter} onChange={e => setTypeFilter(e.target.value)}>
                  <option value="all">Tất cả loại giao dịch</option>
                  <option value="income">Thu nhập</option>
                  <option value="expense">Chi tiêu</option>
                </select>
              </div>
              <div className="cf-filter-group">
                <select className="cf-select" value={categoryFilter} onChange={e => setCategoryFilter(e.target.value)}>
                  <option value="all">Tất cả danh mục</option>
                  {availableCategories.map(c => (
                    <option key={c} value={c}>{c}</option>
                  ))}
                </select>
              </div>
              <div className="cf-filter-group">
                <button className={`btn-toggle-charts ${showCharts ? 'active' : ''}`} onClick={() => setShowCharts(!showCharts)}>
                  {showCharts ? 'Ẩn Biểu đồ' : '📊 Xem Biểu đồ'}
                </button>
              </div>
            </div>

            <div className={`dashboard-body ${showCharts ? 'with-charts' : 'list-only'}`}>
              {/* History List */}
              <div className="history-section glass-panel">
                <div className="section-header">
                  <h2>Lịch sử Giao dịch ({filteredCashflows.length})</h2>
                </div>
                {isCashflowsLoading ? (
                  <div className="loading-state">
                     <div className="spinner"></div>
                     <p>Đang tải dữ liệu...</p>
                  </div>
                ) : (
                  <div className="cf-ledger-view">
                    {filteredCashflows.length === 0 && (
                      <div className="empty-state">
                        <div className="empty-icon">📝</div>
                        <p>Không tìm thấy giao dịch nào phù hợp.</p>
                      </div>
                    )}
                    {filteredCashflows.map((record) => {
                      const recordBudget = budgetByCategoryName.get(record.categoryName);
                      return (
                      <div key={record.id} className={`cf-ledger-item ${recordBudget?.isExceeded ? 'budget-exceeded' : recordBudget && recordBudget.rawProgressPercentage >= 80 ? 'budget-warning' : ''}`}>
                        <div className="cf-ledger-date">
                          <div className="date-main">{new Date(record.date).toLocaleDateString('vi-VN', { day: '2-digit', month: 'short' })}</div>
                          <div className="time-sub">{new Date(record.date).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</div>
                        </div>
                        
                        <div className="cf-ledger-icon" style={{ backgroundColor: `${record.categoryColor}15`, color: record.categoryColor }}>
                          {record.categoryIcon}
                        </div>
                        
                        <div className="cf-ledger-details">
                          <div className="cf-ledger-title-row">
                            <h4>{record.categoryName}</h4>
                            <span className="portfolio-badge">{record.portfolioName}</span>
                            {recordBudget && (
                              <span className={`ledger-budget-chip ${recordBudget.isExceeded ? 'exceeded' : recordBudget.rawProgressPercentage >= 80 ? 'warning' : 'healthy'}`}>
                                {getBudgetToneLabel(recordBudget)} · {recordBudget.rawProgressPercentage.toFixed(0)}%
                              </span>
                            )}
                          </div>
                          {record.description && <p className="cf-ledger-desc">{record.description}</p>}
                        </div>
                        
                        <div className={`cf-ledger-amount ${record.type === CashflowType.Income ? 'positive' : 'negative'}`}>
                          {record.type === CashflowType.Income ? '+' : '-'} {formatCurrency(record.amount)}
                        </div>
                        
                        <div className="cf-ledger-actions">
                          <button className="cf-btn-text" onClick={() => handleEdit(record)} title="Sửa">Edit</button>
                          <button className="cf-btn-text danger" onClick={() => handleDelete(record.id)} title="Xóa">Del</button>
                        </div>
                      </div>
                      );
                    })}
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
                      <>
                        <div style={{ height: 260, width: '100%', marginBottom: '1.5rem' }}>
                          {summary?.expenseByCategory.length === 0 ? (
                            <p className="empty-state">Không có dữ liệu.</p>
                          ) : (
                            <ResponsiveContainer width="100%" height="100%">
                              <PieChart>
                                <Pie
                                  data={summary?.expenseByCategory}
                                  dataKey="amount"
                                  nameKey="categoryName"
                                  cx="50%"
                                  cy="50%"
                                  innerRadius={60}
                                  outerRadius={90}
                                  paddingAngle={2}
                                  stroke="none"
                                >
                                  {summary?.expenseByCategory.map((entry, index) => (
                                    <Cell key={`cell-${index}`} fill={entry.color || '#8884d8'} stroke="none" />
                                  ))}
                                </Pie>
                                <Tooltip 
                                  formatter={(value: any, name: any) => {
                                    const percentage = summary?.totalExpense ? ((value / summary.totalExpense) * 100).toFixed(1) + '%' : '0%';
                                    return [`${formatCurrency(value)} (${percentage})`, name];
                                  }}
                                  contentStyle={{ backgroundColor: 'rgba(15, 23, 42, 0.9)', border: '1px solid rgba(255,255,255,0.1)', borderRadius: '8px', color: '#fff' }}
                                  itemStyle={{ color: '#e2e8f0' }}
                                />
                                <Legend wrapperStyle={{ fontSize: '12px' }} />
                              </PieChart>
                            </ResponsiveContainer>
                          )}
                        </div>

                        <div className="category-bars">
                          {summary?.expenseByCategory.map((cat, idx) => {
                            const percentage = summary.totalExpense > 0 ? (cat.amount / summary.totalExpense) * 100 : 0;
                            const categoryBudget = budgetByCategoryName.get(cat.categoryName);
                            return (
                              <div key={idx} className="category-bar-item">
                                <div className="cat-info">
                                  <span className="cat-name">
                                    <span className="cat-icon-small" style={{ backgroundColor: `${cat.color}20`, color: cat.color }}>{cat.icon}</span> 
                                    {cat.categoryName}
                                  </span>
                                  <div className="cat-stats">
                                    <span className="amount-text">{formatCurrency(cat.amount)}</span>
                                    <span className="percentage-text" style={{ marginLeft: '8px', fontSize: '0.85em', color: '#94a3b8', minWidth: '40px', textAlign: 'right', display: 'inline-block' }}>{percentage.toFixed(1)}%</span>
                                  </div>
                                </div>
                                {categoryBudget && (
                                  <div className={`category-budget-note ${categoryBudget.isExceeded ? 'exceeded' : ''}`}>
                                    Budget: {categoryBudget.rawProgressPercentage.toFixed(1)}%
                                  </div>
                                )}
                                <div className="progress-bg">
                                  <div className="progress-fill" style={{ width: `${percentage}%`, backgroundColor: cat.color }}></div>
                                </div>
                              </div>
                            );
                          })}
                        </div>
                      </>
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
                                  <span className="percentage-text" style={{ marginLeft: '8px', fontSize: '0.85em', color: '#94a3b8', minWidth: '40px', textAlign: 'right', display: 'inline-block' }}>{percentage.toFixed(1)}%</span>
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
        </>
      )}

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
