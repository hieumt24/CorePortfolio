import React, { useEffect, useMemo, useState } from 'react';
import { cashflowsApi } from '../../cashflows/api/cashflowsApi';
import { CashflowType, type CashflowCategory } from '../../cashflows/types/cashflows';
import { cashAccountsApi, type CashAccountDto } from '../../portfolios/api/cashAccountsApi';
import { getPortfolios } from '../../portfolios/api/portfolioApi';
import type { PortfolioDto } from '../../portfolios/types';
import { savingGoalsApi } from '../api/savingGoalsApi';
import type { SaveSavingGoalRequest, SavingGoal } from '../types';
import './SavingGoalsPage.css';

const toDateInput = (date: Date | string) => new Date(date).toISOString().slice(0, 10);

const defaultDeadline = () => {
  const date = new Date();
  date.setMonth(date.getMonth() + 6);
  return toDateInput(date);
};

const formatCurrency = (amount: number, currency: string) =>
  new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  }).format(amount);

export const SavingGoalsPage: React.FC = () => {
  const [goals, setGoals] = useState<SavingGoal[]>([]);
  const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
  const [cashAccounts, setCashAccounts] = useState<CashAccountDto[]>([]);
  const [categories, setCategories] = useState<CashflowCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [form, setForm] = useState<SaveSavingGoalRequest>({
    portfolioId: '',
    cashAccountId: null,
    cashflowCategoryId: '',
    name: '',
    description: '',
    targetAmount: 0,
    currency: 'VND',
    deadline: defaultDeadline(),
    isCompleted: false,
  });

  const selectedCashAccounts = useMemo(
    () => cashAccounts.filter(a => a.portfolioId === form.portfolioId && a.currency === form.currency),
    [cashAccounts, form.portfolioId, form.currency]
  );

  const goalSummary = useMemo(() => {
    const activeGoals = goals.filter(goal => !goal.isCompleted);
    return {
      activeCount: activeGoals.length,
      completedCount: goals.length - activeGoals.length,
      totalTarget: activeGoals.reduce((sum, goal) => sum + goal.targetAmount, 0),
      totalSaved: activeGoals.reduce((sum, goal) => sum + goal.currentAmount, 0),
      monthlyRequired: activeGoals.reduce((sum, goal) => sum + goal.monthlyRequiredSaving, 0),
      currency: activeGoals[0]?.currency || form.currency,
    };
  }, [goals, form.currency]);

  const fetchData = async () => {
    try {
      setLoading(true);
      const [goalData, portfolioData, accountData, categoryData] = await Promise.all([
        savingGoalsApi.getGoals(),
        getPortfolios(),
        cashAccountsApi.getAccounts(),
        cashflowsApi.getCategories(),
      ]);

      const savingCategories = categoryData.filter(c => c.type === CashflowType.Saving);
      setGoals(goalData);
      setPortfolios(portfolioData);
      setCashAccounts(accountData);
      setCategories(savingCategories);
      setForm(prev => ({
        ...prev,
        portfolioId: prev.portfolioId || portfolioData[0]?.id || '',
        cashflowCategoryId: prev.cashflowCategoryId || savingCategories[0]?.id || '',
      }));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.portfolioId || !form.cashflowCategoryId || !form.name || form.targetAmount <= 0) return;

    try {
      setSaving(true);
      await savingGoalsApi.createGoal({
        ...form,
        cashAccountId: form.cashAccountId || null,
        deadline: new Date(form.deadline).toISOString(),
      });
      setForm(prev => ({
        ...prev,
        name: '',
        description: '',
        targetAmount: 0,
        deadline: defaultDeadline(),
        isCompleted: false,
      }));
      await fetchData();
    } finally {
      setSaving(false);
    }
  };

  const toggleCompleted = async (goal: SavingGoal) => {
    await savingGoalsApi.updateGoal(goal.id, {
      portfolioId: goal.portfolioId,
      cashAccountId: goal.cashAccountId,
      cashflowCategoryId: goal.cashflowCategoryId,
      name: goal.name,
      description: goal.description,
      targetAmount: goal.targetAmount,
      currency: goal.currency,
      deadline: goal.deadline,
      isCompleted: !goal.isCompleted,
    });
    await fetchData();
  };

  const deleteGoal = async (id: string) => {
    await savingGoalsApi.deleteGoal(id);
    await fetchData();
  };

  return (
    <div className="saving-goals-page container">
      <div className="saving-goals-header saving-goals-hero">
        <div>
          <span className="page-kicker">Kế hoạch tiết kiệm</span>
          <h1>Saving Goals</h1>
          <p>Theo dõi quỹ khẩn cấp, mua nhà, du lịch và các mục tiêu dài hạn bằng số dư tiền mặt thực tế.</p>
        </div>
        <div className="hero-balance-card">
          <span>Cần tiết kiệm mỗi tháng</span>
          <strong>{formatCurrency(goalSummary.monthlyRequired, goalSummary.currency)}</strong>
        </div>
      </div>

      <section className="saving-summary-grid">
        <div className="saving-summary-card glass-panel">
          <span>Đang theo đuổi</span>
          <strong>{goalSummary.activeCount}</strong>
          <small>{goalSummary.completedCount} mục tiêu đã xong</small>
        </div>
        <div className="saving-summary-card glass-panel">
          <span>Đã tích lũy</span>
          <strong>{formatCurrency(goalSummary.totalSaved, goalSummary.currency)}</strong>
          <small>Trên mục tiêu đang mở</small>
        </div>
        <div className="saving-summary-card glass-panel">
          <span>Tổng mục tiêu</span>
          <strong>{formatCurrency(goalSummary.totalTarget, goalSummary.currency)}</strong>
          <small>Không tính mục tiêu đã hoàn thành</small>
        </div>
      </section>

      <form className="saving-goal-form glass-panel" onSubmit={handleSubmit}>
        <div className="form-section-heading">
          <div>
            <h2>Tạo mục tiêu mới</h2>
            <p>Chọn portfolio, nhóm Saving và tài khoản tiền mặt để app tự tính tiến độ.</p>
          </div>
        </div>
        <div className="form-grid">
          <div className="form-group">
            <label>Tên mục tiêu</label>
            <input
              value={form.name}
              onChange={e => setForm(prev => ({ ...prev, name: e.target.value }))}
              placeholder="Quỹ khẩn cấp"
            />
          </div>
          <div className="form-group">
            <label>Portfolio</label>
            <select
              value={form.portfolioId}
              onChange={e => setForm(prev => ({ ...prev, portfolioId: e.target.value, cashAccountId: null }))}
            >
              {portfolios.map(portfolio => (
                <option key={portfolio.id} value={portfolio.id}>{portfolio.name}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>Saving category</label>
            <select
              value={form.cashflowCategoryId}
              onChange={e => setForm(prev => ({ ...prev, cashflowCategoryId: e.target.value }))}
            >
              {categories.map(category => (
                <option key={category.id} value={category.id}>{category.name}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>Tài khoản tiền mặt</label>
            <select
              value={form.cashAccountId || ''}
              onChange={e => setForm(prev => ({ ...prev, cashAccountId: e.target.value || null }))}
            >
              <option value="">Tất cả tài khoản {form.currency}</option>
              {selectedCashAccounts.map(account => (
                <option key={account.id} value={account.id}>
                  {account.currency} - {formatCurrency(account.balance, account.currency)}
                </option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>Số tiền mục tiêu</label>
            <input
              type="number"
              min="0"
              value={form.targetAmount || ''}
              onChange={e => setForm(prev => ({ ...prev, targetAmount: Number(e.target.value) }))}
              placeholder="50000000"
            />
          </div>
          <div className="form-group">
            <label>Tiền tệ</label>
            <select
              value={form.currency}
              onChange={e => setForm(prev => ({ ...prev, currency: e.target.value, cashAccountId: null }))}
            >
              <option value="VND">VND</option>
              <option value="USD">USD</option>
            </select>
          </div>
          <div className="form-group">
            <label>Deadline</label>
            <input
              type="date"
              value={form.deadline}
              onChange={e => setForm(prev => ({ ...prev, deadline: e.target.value }))}
            />
          </div>
          <div className="form-group form-group-wide">
            <label>Mô tả</label>
            <input
              value={form.description}
              onChange={e => setForm(prev => ({ ...prev, description: e.target.value }))}
              placeholder="Ví dụ: 6 tháng chi phí sinh hoạt"
            />
          </div>
        </div>
        <button className="btn btn-primary" disabled={saving || portfolios.length === 0 || categories.length === 0}>
          {saving ? 'Đang lưu...' : 'Tạo saving goal'}
        </button>
      </form>

      {loading ? (
        <div className="glass-panel saving-goals-empty">Đang tải dữ liệu...</div>
      ) : goals.length === 0 ? (
        <div className="glass-panel saving-goals-empty">
          <strong>Chưa có mục tiêu tiết kiệm nào.</strong>
          <span>Tạo mục tiêu đầu tiên như quỹ khẩn cấp hoặc chuyến du lịch để bắt đầu theo dõi tiến độ.</span>
        </div>
      ) : (
        <div className="saving-goals-grid">
          {goals.map(goal => (
            <article key={goal.id} className={`saving-goal-card glass-panel ${goal.isCompleted ? 'completed' : ''}`}>
              <div className="saving-goal-topline">
                <span className="goal-category">{goal.categoryName}</span>
                <span className={goal.isCompleted ? 'goal-status done' : 'goal-status'}>
                  {goal.isCompleted ? 'Hoàn thành' : `${goal.daysRemaining} ngày`}
                </span>
              </div>
              <h2>{goal.name}</h2>
              <p>{goal.description || goal.portfolioName}</p>

              <div className="goal-progress-row">
                <span>{formatCurrency(goal.currentAmount, goal.currency)}</span>
                <span>{formatCurrency(goal.targetAmount, goal.currency)}</span>
              </div>
              <div className="goal-progress-track">
                <div style={{ width: `${Math.min(goal.progressPercentage, 100)}%` }} />
              </div>
              <div className="goal-metrics">
                <div>
                  <span>Còn lại</span>
                  <strong>{formatCurrency(goal.remainingAmount, goal.currency)}</strong>
                </div>
                <div>
                  <span>Cần tiết kiệm/tháng</span>
                  <strong>{formatCurrency(goal.monthlyRequiredSaving, goal.currency)}</strong>
                </div>
                <div>
                  <span>Cash balance</span>
                  <strong>{formatCurrency(goal.cashAccountBalance, goal.currency)}</strong>
                </div>
                <div>
                  <span>Saving cashflow</span>
                  <strong>{formatCurrency(goal.savingCashflowAmount, goal.currency)}</strong>
                </div>
              </div>
              <div className="goal-actions">
                <button className="btn btn-outline btn-sm" onClick={() => toggleCompleted(goal)}>
                  {goal.isCompleted ? 'Mở lại' : 'Đánh dấu xong'}
                </button>
                <button className="btn btn-outline btn-sm danger" onClick={() => deleteGoal(goal.id)}>
                  Xóa
                </button>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
};
