import { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { getPortfolioSummary, getPortfolios } from '../../portfolios/api/portfolioApi';
import type { PortfolioSummaryDto } from '../../portfolios/types';
import { cashflowsApi } from '../../cashflows/api/cashflowsApi';
import { getBudgetsProgress, type BudgetProgress } from '../../budgets/api/budgetApi';
import { savingGoalsApi } from '../../savingGoals/api/savingGoalsApi';
import type { SavingGoal } from '../../savingGoals/types';
import { dcaPlansApi } from '../../dcaPlans/api/dcaPlansApi';
import type { DcaPlan } from '../../dcaPlans/types';
import { rebalancingPlansApi } from '../../rebalancing/api/rebalancingPlansApi';
import { RebalanceExecutionPlanStatus, type RebalanceExecutionPlan } from '../../rebalancing/types';
import './OverviewDashboard.css';

const formatVnd = (amount: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(amount);

const getCurrentMonth = () => {
  const now = new Date();
  return { year: now.getFullYear(), month: now.getMonth() + 1 };
};

const isWithinDays = (dateValue: string, days: number) => {
  const date = new Date(dateValue);
  if (Number.isNaN(date.getTime())) return false;
  const now = new Date();
  const diff = date.getTime() - now.getTime();
  return diff >= 0 && diff <= days * 24 * 60 * 60 * 1000;
};

type DashboardData = {
  summaries: PortfolioSummaryDto[];
  cashflow: Awaited<ReturnType<typeof cashflowsApi.getSummary>> | null;
  budgets: BudgetProgress[];
  goals: SavingGoal[];
  dcaPlans: DcaPlan[];
  rebalancePlans: RebalanceExecutionPlan[];
};

export function OverviewDashboard() {
  const [data, setData] = useState<DashboardData>({
    summaries: [],
    cashflow: null,
    budgets: [],
    goals: [],
    dcaPlans: [],
    rebalancePlans: [],
  });
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const { year, month } = getCurrentMonth();
        const portfolios = await getPortfolios();
        const summaryResults = await Promise.allSettled(portfolios.map(portfolio => getPortfolioSummary(portfolio.id)));
        const summaries = summaryResults
          .filter((result): result is PromiseFulfilledResult<PortfolioSummaryDto> => result.status === 'fulfilled')
          .map(result => result.value);

        const [cashflowResult, budgetsResult, goalsResult, dcaResult, rebalanceResult] = await Promise.allSettled([
          cashflowsApi.getSummary('VND'),
          getBudgetsProgress({ year, month, currency: 'VND' }),
          savingGoalsApi.getGoals(),
          dcaPlansApi.getPlans(),
          rebalancingPlansApi.getPlans(),
        ]);

        setData({
          summaries,
          cashflow: cashflowResult.status === 'fulfilled' ? cashflowResult.value : null,
          budgets: budgetsResult.status === 'fulfilled' ? budgetsResult.value : [],
          goals: goalsResult.status === 'fulfilled' ? goalsResult.value : [],
          dcaPlans: dcaResult.status === 'fulfilled' ? dcaResult.value : [],
          rebalancePlans: rebalanceResult.status === 'fulfilled' ? rebalanceResult.value : [],
        });
      } catch (error) {
        console.error('Failed to load dashboard', error);
      } finally {
        setLoading(false);
      }
    };

    load();
  }, []);

  const metrics = useMemo(() => {
    const invested = data.summaries.reduce((sum, item) => sum + item.totalInvested, 0);
    const portfolioValue = data.summaries.reduce((sum, item) => sum + item.currentTotalValue, 0);
    const cashBalance = data.summaries.reduce((sum, item) => {
      return sum + item.cashBalances.filter(balance => balance.currency === 'VND').reduce((cash, balance) => cash + balance.balance, 0);
    }, 0);
    const unrealizedPnl = data.summaries.reduce((sum, item) => sum + item.unrealizedPnl, 0);
    const netWorth = portfolioValue + cashBalance;
    const budgetLimit = data.budgets.reduce((sum, budget) => sum + budget.monthlyLimit, 0);
    const budgetSpent = data.budgets.reduce((sum, budget) => sum + budget.spentAmount, 0);
    const budgetProgress = budgetLimit > 0 ? (budgetSpent / budgetLimit) * 100 : 0;

    return { invested, portfolioValue, cashBalance, unrealizedPnl, netWorth, budgetLimit, budgetSpent, budgetProgress };
  }, [data]);

  const risks = useMemo(() => {
    const exceededBudgets = data.budgets.filter(budget => budget.isExceeded);
    const warningBudgets = data.budgets.filter(budget => !budget.isExceeded && budget.rawProgressPercentage >= 80);
    const urgentGoals = data.goals.filter(goal => !goal.isCompleted && goal.daysRemaining <= 30 && goal.progressPercentage < 100);
    const dueDca = data.dcaPlans.filter(plan => plan.isActive && isWithinDays(plan.nextExecutionDate, 7));
    const cashLimitedDca = data.dcaPlans.filter(plan => plan.isActive && !plan.hasEnoughCash);
    const simulatedRebalance = data.rebalancePlans.filter(plan => plan.status === RebalanceExecutionPlanStatus.Simulated);
    return { exceededBudgets, warningBudgets, urgentGoals, dueDca, cashLimitedDca, simulatedRebalance };
  }, [data]);

  const actionItems = [
    ...risks.exceededBudgets.slice(0, 3).map(item => ({
      title: `${item.categoryName} vượt budget`,
      detail: `${item.rawProgressPercentage.toFixed(0)}% của hạn mức tháng`,
      tone: 'danger',
      to: '/budgets',
    })),
    ...risks.cashLimitedDca.slice(0, 2).map(item => ({
      title: `${item.symbol} thiếu cash cho DCA`,
      detail: `${formatVnd(item.amount)} cần cho lần mua tiếp theo`,
      tone: 'warning',
      to: '/dca-plans',
    })),
    ...risks.urgentGoals.slice(0, 2).map(item => ({
      title: `${item.name} gần deadline`,
      detail: `${item.daysRemaining} ngày còn lại, đạt ${item.progressPercentage.toFixed(0)}%`,
      tone: 'warning',
      to: '/saving-goals',
    })),
    ...risks.simulatedRebalance.slice(0, 2).map(item => ({
      title: 'Có kế hoạch rebalancing chưa applied',
      detail: `${item.items.length} hành động, cash ${formatVnd(item.availableCash)}`,
      tone: 'info',
      to: '/rebalancing',
    })),
  ];

  return (
    <div className="overview-dashboard container">
      <section className="overview-header">
        <div>
          <span className="overview-kicker">Overview</span>
          <h1>Dashboard tổng quan</h1>
          <p>Tài sản, dòng tiền, budget, mục tiêu và lịch đầu tư trong một màn hình.</p>
        </div>
        <div className="overview-header-actions">
          <Link to="/cashflow" className="btn btn-outline">Thêm cashflow</Link>
          <Link to="/portfolios" className="btn btn-primary">Xem portfolio</Link>
        </div>
      </section>

      <section className="overview-metrics">
        <div className="overview-metric primary">
          <span>Net worth</span>
          <strong>{loading ? '...' : formatVnd(metrics.netWorth)}</strong>
          <small>Portfolio value + cash VND</small>
        </div>
        <div className="overview-metric">
          <span>Invested value</span>
          <strong>{loading ? '...' : formatVnd(metrics.portfolioValue)}</strong>
          <small>{formatVnd(metrics.unrealizedPnl)} unrealized PnL</small>
        </div>
        <div className="overview-metric">
          <span>Cash balance</span>
          <strong>{loading ? '...' : formatVnd(metrics.cashBalance)}</strong>
          <small>{data.summaries.length} portfolios</small>
        </div>
        <div className="overview-metric">
          <span>Monthly budget</span>
          <strong>{loading ? '...' : `${metrics.budgetProgress.toFixed(1)}%`}</strong>
          <small>{formatVnd(metrics.budgetSpent)} / {formatVnd(metrics.budgetLimit)}</small>
        </div>
      </section>

      <section className="overview-grid">
        <div className="overview-panel action-panel">
          <div className="panel-title-row">
            <h2>Action needed</h2>
            <span>{actionItems.length}</span>
          </div>
          {actionItems.length === 0 ? (
            <div className="overview-empty">Không có cảnh báo quan trọng.</div>
          ) : (
            <div className="action-list">
              {actionItems.map((item, index) => (
                <Link key={`${item.title}-${index}`} to={item.to} className={`action-item ${item.tone}`}>
                  <span />
                  <div>
                    <strong>{item.title}</strong>
                    <small>{item.detail}</small>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </div>

        <div className="overview-panel">
          <div className="panel-title-row">
            <h2>Cashflow tháng này</h2>
            <Link to="/cashflow">Chi tiết</Link>
          </div>
          <div className="cashflow-split">
            <div>
              <span>Income</span>
              <strong>{formatVnd(data.cashflow?.totalIncome || 0)}</strong>
            </div>
            <div>
              <span>Expense</span>
              <strong>{formatVnd(data.cashflow?.totalExpense || 0)}</strong>
            </div>
            <div>
              <span>Net flow</span>
              <strong>{formatVnd(data.cashflow?.netFlow || 0)}</strong>
            </div>
          </div>
        </div>

        <div className="overview-panel">
          <div className="panel-title-row">
            <h2>Saving goals</h2>
            <Link to="/saving-goals">Mở goals</Link>
          </div>
          <div className="mini-list">
            {data.goals.slice(0, 4).map(goal => (
              <div key={goal.id} className="mini-row">
                <div>
                  <strong>{goal.name}</strong>
                  <small>{goal.progressPercentage.toFixed(0)}% - cần {formatVnd(goal.monthlyRequiredSaving)}/tháng</small>
                </div>
                <span>{goal.daysRemaining}d</span>
              </div>
            ))}
            {data.goals.length === 0 && <div className="overview-empty">Chưa có saving goal.</div>}
          </div>
        </div>

        <div className="overview-panel">
          <div className="panel-title-row">
            <h2>DCA sắp tới</h2>
            <Link to="/dca-plans">Mở DCA</Link>
          </div>
          <div className="mini-list">
            {data.dcaPlans.filter(plan => plan.isActive).slice(0, 4).map(plan => (
              <div key={plan.id} className={`mini-row ${plan.hasEnoughCash ? '' : 'warning'}`}>
                <div>
                  <strong>{plan.symbol}</strong>
                  <small>{formatVnd(plan.amount)} - {new Date(plan.nextExecutionDate).toLocaleDateString('vi-VN')}</small>
                </div>
                <span>{plan.hasEnoughCash ? 'Ready' : 'Cash'}</span>
              </div>
            ))}
            {data.dcaPlans.filter(plan => plan.isActive).length === 0 && <div className="overview-empty">Chưa có DCA active.</div>}
          </div>
        </div>
      </section>
    </div>
  );
}
