import React, { useEffect, useMemo, useState } from 'react';
import { cashflowsApi } from '../../cashflows/api/cashflowsApi';
import { CashflowType, type CashflowCategory, type CashflowRecord } from '../../cashflows/types/cashflows';
import { getBudgetsProgress, setBudget, type BudgetProgress } from '../api/budgetApi';
import './BudgetsPage.css';

type CategoryOption = {
  id: string;
  name: string;
  icon: string;
  color: string;
  path: string;
  depth: number;
  descendantIds: string[];
};

type BudgetTone = 'healthy' | 'warning' | 'danger';

const formatMoney = (amount: number, currency: string) =>
  new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  }).format(amount);

const formatCompactDate = (date: string) =>
  new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: 'short' }).format(new Date(date));

const toMonthInput = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;

const parseMonthInput = (value: string) => {
  const [year, month] = value.split('-').map(Number);
  return { year, month };
};

const getMonthRange = (value: string) => {
  const { year, month } = parseMonthInput(value);
  const start = new Date(year, month - 1, 1, 0, 0, 0);
  const end = new Date(year, month, 0, 23, 59, 59);
  return {
    year,
    month,
    startDate: start.toISOString(),
    endDate: end.toISOString(),
  };
};

const collectCategoryIds = (category: CashflowCategory): string[] => [
  category.id,
  ...(category.subCategories || []).flatMap(collectCategoryIds),
];

const flattenCategories = (categories: CashflowCategory[], parentPath = '', depth = 0): CategoryOption[] =>
  categories.flatMap(category => {
    const path = parentPath ? `${parentPath} / ${category.name}` : category.name;
    return [
      {
        id: category.id,
        name: category.name,
        icon: category.icon,
        color: category.color,
        path,
        depth,
        descendantIds: collectCategoryIds(category),
      },
      ...flattenCategories(category.subCategories || [], path, depth + 1),
    ];
  });

const getBudgetTone = (budget: BudgetProgress | { rawProgressPercentage: number }): BudgetTone => {
  if (budget.rawProgressPercentage >= 100) return 'danger';
  if (budget.rawProgressPercentage >= 80) return 'warning';
  return 'healthy';
};

const getToneLabel = (tone: BudgetTone) => {
  if (tone === 'danger') return 'Vượt mức';
  if (tone === 'warning') return 'Cần chú ý';
  return 'Đang ổn';
};

export const BudgetsPage: React.FC = () => {
  const [budgets, setBudgets] = useState<BudgetProgress[]>([]);
  const [loading, setLoading] = useState(true);
  const [cashflowLoading, setCashflowLoading] = useState(true);
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [monthlyCashflows, setMonthlyCashflows] = useState<CashflowRecord[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedBudgetId, setSelectedBudgetId] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');
  const [monthlyLimit, setMonthlyLimit] = useState<number | ''>('');
  const [selectedMonth, setSelectedMonth] = useState(toMonthInput(new Date()));
  const [currency, setCurrency] = useState('VND');

  const monthRange = useMemo(() => getMonthRange(selectedMonth), [selectedMonth]);

  const categoryById = useMemo(
    () => new Map(categories.map(category => [category.id, category])),
    [categories]
  );

  const selectedBudget = useMemo(
    () => budgets.find(budget => budget.id === selectedBudgetId) || budgets[0],
    [budgets, selectedBudgetId]
  );

  const selectedCategoryOption = selectedBudget ? categoryById.get(selectedBudget.categoryId) : undefined;

  const linkedCashflows = useMemo(() => {
    if (!selectedBudget) return [];
    const categoryIds = new Set(selectedCategoryOption?.descendantIds || [selectedBudget.categoryId]);
    return monthlyCashflows.filter(record => categoryIds.has(record.categoryId));
  }, [monthlyCashflows, selectedBudget, selectedCategoryOption]);

  const linkedTotal = useMemo(
    () => linkedCashflows.reduce((sum, record) => sum + record.amount, 0),
    [linkedCashflows]
  );

  const summary = useMemo(() => {
    const totalLimit = budgets.reduce((sum, budget) => sum + budget.monthlyLimit, 0);
    const totalSpent = budgets.reduce((sum, budget) => sum + budget.spentAmount, 0);
    const remaining = Math.max(totalLimit - totalSpent, 0);
    const rawProgress = totalLimit > 0 ? (totalSpent / totalLimit) * 100 : 0;
    const overBudgetCount = budgets.filter(budget => budget.rawProgressPercentage >= 100).length;
    const warningCount = budgets.filter(budget => budget.rawProgressPercentage >= 80 && budget.rawProgressPercentage < 100).length;

    return { totalLimit, totalSpent, remaining, rawProgress, overBudgetCount, warningCount };
  }, [budgets]);

  const fetchData = async () => {
    try {
      setLoading(true);
      setCashflowLoading(true);
      const [budgetsData, categoriesData, cashflowsData] = await Promise.all([
        getBudgetsProgress({ year: monthRange.year, month: monthRange.month, currency }),
        cashflowsApi.getCategories(),
        cashflowsApi.getCashflows(1, 500, currency, CashflowType.Expense, monthRange.startDate, monthRange.endDate),
      ]);

      const expenseCategories = flattenCategories(categoriesData.filter(category => category.type === CashflowType.Expense));

      setBudgets(budgetsData);
      setCategories(expenseCategories);
      setMonthlyCashflows(cashflowsData);
      setSelectedBudgetId(currentId => {
        if (budgetsData.some(budget => budget.id === currentId)) return currentId;
        return budgetsData[0]?.id || '';
      });
    } catch (error) {
      console.error('Failed to fetch budget data', error);
    } finally {
      setLoading(false);
      setCashflowLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [selectedMonth, currency]);

  const openBudgetForm = (budget?: BudgetProgress) => {
    setSelectedCategory(budget?.categoryId || categories[0]?.id || '');
    setMonthlyLimit(budget?.monthlyLimit || '');
    setIsModalOpen(true);
  };

  const handleSaveBudget = async () => {
    if (!selectedCategory || !monthlyLimit) return;

    try {
      await setBudget({ categoryId: selectedCategory, monthlyLimit: Number(monthlyLimit) });
      setIsModalOpen(false);
      await fetchData();
    } catch (error) {
      console.error('Failed to save budget', error);
    }
  };

  return (
    <div className="budgets-page container">
      <section className="budgets-toolbar">
        <div>
          <span className="budgets-kicker">Budget control</span>
          <h1>Ngân sách và cashflow</h1>
        </div>
        <div className="budgets-toolbar-actions">
          <input
            type="month"
            value={selectedMonth}
            onChange={event => setSelectedMonth(event.target.value)}
            aria-label="Chon thang"
          />
          <select value={currency} onChange={event => setCurrency(event.target.value)} aria-label="Chon tien te">
            <option value="VND">VND</option>
            <option value="USD">USD</option>
          </select>
          <button onClick={() => openBudgetForm()} className="btn btn-primary">
            Them budget
          </button>
        </div>
      </section>

      <section className={`budget-alert-strip ${summary.overBudgetCount > 0 ? 'danger' : summary.warningCount > 0 ? 'warning' : 'healthy'}`}>
        <div>
          <strong>
            {summary.overBudgetCount > 0
              ? `${summary.overBudgetCount} budget đã vượt mức`
              : summary.warningCount > 0
                ? `${summary.warningCount} budget sắp chạm ngưỡng`
                : 'Tất cả budget đang ổn'}
          </strong>
          <span>
            {formatMoney(summary.totalSpent, currency)} đã chi trên tổng hạn mức {formatMoney(summary.totalLimit, currency)}
          </span>
        </div>
        <span className="budget-alert-percent">{summary.rawProgress.toFixed(1)}%</span>
      </section>

      <section className="budget-summary-grid">
        <div className="budget-summary-card">
          <span>Tổng hạn mức</span>
          <strong>{formatMoney(summary.totalLimit, currency)}</strong>
          <small>{budgets.length} category đang theo dõi</small>
        </div>
        <div className="budget-summary-card">
          <span>Đã chi</span>
          <strong>{formatMoney(summary.totalSpent, currency)}</strong>
          <small>{summary.rawProgress.toFixed(1)}% tổng budget</small>
        </div>
        <div className="budget-summary-card">
          <span>Còn lại</span>
          <strong>{formatMoney(summary.remaining, currency)}</strong>
          <small>{summary.overBudgetCount} category vượt mức</small>
        </div>
      </section>

      <section className="budget-workbench">
        <div className="budget-list-panel">
          <div className="budget-section-header">
            <div>
              <h2>Budget categories</h2>
              <span>{selectedMonth}</span>
            </div>
          </div>

          {loading ? (
            <div className="budget-empty">Đang tải dữ liệu...</div>
          ) : budgets.length === 0 ? (
            <div className="budget-empty">
              <strong>Chưa có budget nào.</strong>
              <span>Hãy thêm hạn mức cho category chi tiêu quan trọng.</span>
            </div>
          ) : (
            <div className="budget-card-list">
              {budgets.map(budget => {
                const tone = getBudgetTone(budget);
                const remaining = Math.max(budget.monthlyLimit - budget.spentAmount, 0);
                const isSelected = selectedBudget?.id === budget.id;

                return (
                  <button
                    key={budget.id}
                    type="button"
                    className={`budget-row ${tone} ${isSelected ? 'selected' : ''}`}
                    onClick={() => setSelectedBudgetId(budget.id)}
                  >
                    <span className="budget-row-mark" style={{ color: budget.categoryColor, borderColor: budget.categoryColor }}>
                      {budget.categoryIcon || budget.categoryName.slice(0, 1)}
                    </span>
                    <span className="budget-row-main">
                      <span className="budget-row-title">
                        <strong>{budget.categoryName}</strong>
                        <em>{getToneLabel(tone)}</em>
                      </span>
                      <span className="budget-row-meter">
                        <span style={{ width: `${Math.min(budget.rawProgressPercentage, 100)}%` }} />
                      </span>
                      <span className="budget-row-values">
                        <span>{formatMoney(budget.spentAmount, currency)}</span>
                        <span>{formatMoney(budget.monthlyLimit, currency)}</span>
                      </span>
                    </span>
                    <span className="budget-row-side">
                      <strong>{budget.rawProgressPercentage.toFixed(0)}%</strong>
                      <small>{remaining > 0 ? `Còn ${formatMoney(remaining, currency)}` : `Vượt ${formatMoney(budget.spentAmount - budget.monthlyLimit, currency)}`}</small>
                    </span>
                  </button>
                );
              })}
            </div>
          )}
        </div>

        <aside className="budget-detail-panel">
          {selectedBudget ? (
            <>
              <div className="budget-detail-header">
                <div>
                  <span className={`budget-status-pill ${getBudgetTone(selectedBudget)}`}>{getToneLabel(getBudgetTone(selectedBudget))}</span>
                  <h2>{selectedBudget.categoryName}</h2>
                  <p>{selectedCategoryOption?.path || selectedBudget.categoryName}</p>
                </div>
                <button className="btn btn-outline btn-sm" onClick={() => openBudgetForm(selectedBudget)}>
                  Sửa
                </button>
              </div>

              <div className="budget-detail-meter">
                <div>
                  <span>Đã chi</span>
                  <strong>{formatMoney(selectedBudget.spentAmount, currency)}</strong>
                </div>
                <div>
                  <span>Hạn mức</span>
                  <strong>{formatMoney(selectedBudget.monthlyLimit, currency)}</strong>
                </div>
                <div>
                  <span>{selectedBudget.isExceeded ? 'Vượt mức' : 'Còn lại'}</span>
                  <strong>
                    {selectedBudget.isExceeded
                      ? formatMoney(selectedBudget.spentAmount - selectedBudget.monthlyLimit, currency)
                      : formatMoney(selectedBudget.remainingAmount, currency)}
                  </strong>
                </div>
              </div>

              <div className="budget-detail-track">
                <span style={{ width: `${Math.min(selectedBudget.rawProgressPercentage, 100)}%` }} />
              </div>

              <div className={`budget-detail-alert ${getBudgetTone(selectedBudget)}`}>
                {selectedBudget.isExceeded
                  ? 'Category này đã vượt budget tháng này.'
                  : selectedBudget.rawProgressPercentage >= 80
                    ? 'Category này đang gần chạm ngưỡng budget.'
                    : 'Category này vẫn nằm trong vùng an toàn.'}
              </div>

              <div className="cashflow-link-header">
                <div>
                  <h3>Cashflow đã link</h3>
                  <span>{linkedCashflows.length} giao dịch trong tháng</span>
                </div>
                <strong>{formatMoney(linkedTotal, currency)}</strong>
              </div>

              {cashflowLoading ? (
                <div className="budget-empty compact">Đang tải cashflow...</div>
              ) : linkedCashflows.length === 0 ? (
                <div className="budget-empty compact">Chưa có giao dịch nào cho category này trong tháng.</div>
              ) : (
                <div className="linked-cashflow-list">
                  {linkedCashflows.map(record => (
                    <div key={record.id} className="linked-cashflow-item">
                      <span className="linked-cashflow-date">{formatCompactDate(record.date)}</span>
                      <span className="linked-cashflow-icon" style={{ color: record.categoryColor, borderColor: record.categoryColor }}>
                        {record.categoryIcon || record.categoryName.slice(0, 1)}
                      </span>
                      <span className="linked-cashflow-main">
                        <strong>{record.categoryName}</strong>
                        <small>{record.description || record.portfolioName}</small>
                      </span>
                      <strong className="linked-cashflow-amount">{formatMoney(record.amount, currency)}</strong>
                    </div>
                  ))}
                </div>
              )}
            </>
          ) : (
            <div className="budget-empty compact">Chọn một budget để xem cashflow liên quan.</div>
          )}
        </aside>
      </section>

      {isModalOpen && (
        <div className="budget-modal-backdrop" role="presentation" onClick={() => setIsModalOpen(false)}>
          <div className="budget-modal" role="dialog" aria-modal="true" onClick={event => event.stopPropagation()}>
            <div className="budget-modal-header">
              <div>
                <h2>Thiết lập budget</h2>
              </div>
              <button className="modal-close-btn" onClick={() => setIsModalOpen(false)} aria-label="Đóng">
                X
              </button>
            </div>

            <div className="form-group">
              <label>Category chi tiêu</label>
              <select value={selectedCategory} onChange={event => setSelectedCategory(event.target.value)}>
                <option value="">Chọn category</option>
                {categories.map(category => (
                  <option key={category.id} value={category.id}>
                    {`${'-- '.repeat(category.depth)}${category.icon ? `${category.icon} ` : ''}${category.path}`}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label>Hạn mức mỗi tháng</label>
              <input
                type="number"
                min="0"
                value={monthlyLimit}
                onChange={event => setMonthlyLimit(event.target.value ? Number(event.target.value) : '')}
                placeholder="VD: 5000000"
              />
            </div>

            <div className="budget-modal-actions">
              <button onClick={() => setIsModalOpen(false)} className="btn btn-outline">
                Hủy
              </button>
              <button onClick={handleSaveBudget} disabled={!selectedCategory || !monthlyLimit} className="btn btn-primary">
                Lưu budget
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
