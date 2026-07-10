import React, { useEffect, useMemo, useState } from 'react';
import { cashflowsApi } from '../../cashflows/api/cashflowsApi';
import { CashflowType, type CashflowCategory } from '../../cashflows/types/cashflows';
import { getBudgetsProgress, setBudget, type BudgetProgress } from '../api/budgetApi';
import './BudgetsPage.css';

type CategoryOption = {
  id: string;
  name: string;
  icon: string;
  color: string;
  path: string;
  depth: number;
};

const formatVnd = (amount: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(amount);

const toMonthInput = (date: Date) =>
  `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;

const parseMonthInput = (value: string) => {
  const [year, month] = value.split('-').map(Number);
  return { year, month };
};

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
      },
      ...flattenCategories(category.subCategories || [], path, depth + 1),
    ];
  });

const getBudgetTone = (percentage: number) => {
  if (percentage >= 100) return 'danger';
  if (percentage >= 80) return 'warning';
  return 'healthy';
};

export const BudgetsPage: React.FC = () => {
  const [budgets, setBudgets] = useState<BudgetProgress[]>([]);
  const [loading, setLoading] = useState(true);
  const [categories, setCategories] = useState<CategoryOption[]>([]);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedCategory, setSelectedCategory] = useState('');
  const [monthlyLimit, setMonthlyLimit] = useState<number | ''>('');
  const [selectedMonth, setSelectedMonth] = useState(toMonthInput(new Date()));

  const summary = useMemo(() => {
    const totalLimit = budgets.reduce((sum, budget) => sum + budget.monthlyLimit, 0);
    const totalSpent = budgets.reduce((sum, budget) => sum + budget.spentAmount, 0);
    const remaining = Math.max(totalLimit - totalSpent, 0);
    const progress = totalLimit > 0 ? Math.min((totalSpent / totalLimit) * 100, 100) : 0;
    const overBudgetCount = budgets.filter(budget => budget.progressPercentage >= 100).length;

    return { totalLimit, totalSpent, remaining, progress, overBudgetCount };
  }, [budgets]);

  const fetchData = async () => {
    try {
      setLoading(true);
      const { year, month } = parseMonthInput(selectedMonth);
      const [budgetsData, categoriesData] = await Promise.all([
        getBudgetsProgress({ year, month, currency: 'VND' }),
        cashflowsApi.getCategories(),
      ]);

      setBudgets(budgetsData);
      setCategories(flattenCategories(categoriesData.filter(category => category.type === CashflowType.Expense)));
    } catch (error) {
      console.error('Failed to fetch budget data', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, [selectedMonth]);

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
      <section className="budgets-hero">
        <div>
          <span className="budgets-kicker">Kiểm soát chi tiêu</span>
          <h1>Ngân sách hằng tháng</h1>
          <p>Thiết lập hạn mức theo danh mục cha/con và theo dõi mức sử dụng trong tháng hiện tại.</p>
        </div>
        <button onClick={() => openBudgetForm()} className="btn btn-primary">
          Thêm ngân sách
        </button>
      </section>

      <section className="budget-month-bar glass-panel">
        <div>
          <span>Tháng đang theo dõi</span>
          <strong>{selectedMonth}</strong>
        </div>
        <input
          type="month"
          value={selectedMonth}
          onChange={event => setSelectedMonth(event.target.value)}
        />
      </section>

      <section className="budget-summary-grid">
        <div className="budget-summary-card glass-panel">
          <span>Tổng ngân sách</span>
          <strong>{formatVnd(summary.totalLimit)}</strong>
          <small>{budgets.length} danh mục đang theo dõi</small>
        </div>
        <div className="budget-summary-card glass-panel">
          <span>Đã chi</span>
          <strong>{formatVnd(summary.totalSpent)}</strong>
          <small>{summary.progress.toFixed(1)}% tổng hạn mức</small>
        </div>
        <div className="budget-summary-card glass-panel">
          <span>Còn lại</span>
          <strong>{formatVnd(summary.remaining)}</strong>
          <small>{summary.overBudgetCount} danh mục vượt hạn mức</small>
        </div>
      </section>

      <section className="budget-overview glass-panel">
        <div className="budget-overview-copy">
          <h2>Tổng quan tháng này</h2>
          <p>Màu trạng thái đổi theo tiến độ: an toàn dưới 80%, cần chú ý từ 80%, và vượt mức khi đạt 100%.</p>
        </div>
        <div className="budget-overview-meter">
          <div className="meter-label">
            <span>{formatVnd(summary.totalSpent)}</span>
            <span>{formatVnd(summary.totalLimit)}</span>
          </div>
          <div className="budget-track">
            <div className={`budget-fill ${getBudgetTone(summary.progress)}`} style={{ width: `${summary.progress}%` }} />
          </div>
        </div>
      </section>

      {loading ? (
        <div className="budget-empty glass-panel">Đang tải dữ liệu...</div>
      ) : budgets.length === 0 ? (
        <div className="budget-empty glass-panel">
          <strong>Chưa có ngân sách nào.</strong>
          <span>Thêm hạn mức cho các danh mục chi tiêu quan trọng để bắt đầu theo dõi.</span>
        </div>
      ) : (
        <section className="budget-card-grid">
          {budgets.map(budget => {
            const tone = getBudgetTone(budget.progressPercentage);
            const remaining = Math.max(budget.monthlyLimit - budget.spentAmount, 0);

            return (
              <article key={budget.id} className={`budget-card glass-panel ${tone}`}>
                <div className="budget-card-header">
                  <div className="budget-category-mark" style={{ borderColor: budget.categoryColor, color: budget.categoryColor }}>
                    {budget.categoryIcon || budget.categoryName.slice(0, 1)}
                  </div>
                  <div>
                    <h2>{budget.categoryName}</h2>
                    <span>{tone === 'danger' ? 'Vượt hạn mức' : tone === 'warning' ? 'Cần chú ý' : 'Đang ổn'}</span>
                  </div>
                  <button className="btn btn-outline btn-sm" onClick={() => openBudgetForm(budget)}>
                    Sửa
                  </button>
                </div>

                <div className="budget-values">
                  <div>
                    <span>Đã chi</span>
                    <strong>{formatVnd(budget.spentAmount)}</strong>
                  </div>
                  <div>
                    <span>Còn lại</span>
                    <strong>{formatVnd(remaining)}</strong>
                  </div>
                </div>

                <div className="meter-label">
                  <span>{budget.progressPercentage.toFixed(1)}%</span>
                  <span>{formatVnd(budget.monthlyLimit)}</span>
                </div>
                <div className="budget-track">
                  <div className={`budget-fill ${tone}`} style={{ width: `${Math.min(budget.progressPercentage, 100)}%` }} />
                </div>
              </article>
            );
          })}
        </section>
      )}

      {isModalOpen && (
        <div className="budget-modal-backdrop" role="presentation" onClick={() => setIsModalOpen(false)}>
          <div className="budget-modal glass-panel" role="dialog" aria-modal="true" onClick={event => event.stopPropagation()}>
            <div className="budget-modal-header">
              <div>
                <h2>Thiết lập ngân sách</h2>
                <p>Chọn danh mục chi tiêu, có thể là category cha hoặc category con.</p>
              </div>
              <button className="modal-close-btn" onClick={() => setIsModalOpen(false)} aria-label="Đóng">
                X
              </button>
            </div>

            <div className="form-group">
              <label>Danh mục chi tiêu</label>
              <select value={selectedCategory} onChange={e => setSelectedCategory(e.target.value)}>
                <option value="">Chọn danh mục</option>
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
                value={monthlyLimit}
                onChange={e => setMonthlyLimit(e.target.value ? Number(e.target.value) : '')}
                placeholder="VD: 5000000"
              />
            </div>

            <div className="budget-modal-actions">
              <button onClick={() => setIsModalOpen(false)} className="btn btn-outline">
                Hủy
              </button>
              <button onClick={handleSaveBudget} disabled={!selectedCategory || !monthlyLimit} className="btn btn-primary">
                Lưu ngân sách
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
