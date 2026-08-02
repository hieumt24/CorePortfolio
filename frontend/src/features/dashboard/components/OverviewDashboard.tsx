import { useCallback, useEffect, useMemo, useState } from 'react';
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
import { formatVietnamDateTime } from '../../../shared/utils/dateTime';
import { financialHealthApi, type FinancialHealth } from '../api/financialHealthApi';
import './OverviewDashboard.css';

const formatVnd = (amount: number) =>
  new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(amount);

const formatPercent = (value: number) => `${value > 0 ? '+' : ''}${value.toFixed(1)}%`;

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
  health: FinancialHealth | null;
};

const emptyDashboardData: DashboardData = {
  summaries: [],
  cashflow: null,
  budgets: [],
  goals: [],
  dcaPlans: [],
  rebalancePlans: [],
  health: null,
};

type ActionTone = 'danger' | 'warning' | 'info';

type ActionItem = {
  title: string;
  detail: string;
  tone: ActionTone;
  label: string;
  to: string;
};

export function OverviewDashboard() {
  const [data, setData] = useState<DashboardData>(emptyDashboardData);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadDashboard = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const { year, month } = getCurrentMonth();
      const portfolios = await getPortfolios();
      const [serviceResults, summaryResults] = await Promise.all([
        Promise.allSettled([
          financialHealthApi.get('VND'),
          cashflowsApi.getSummary('VND'),
          getBudgetsProgress({ year, month, currency: 'VND' }),
          savingGoalsApi.getGoals(),
          dcaPlansApi.getPlans(),
          rebalancingPlansApi.getPlans(),
        ] as const),
        Promise.allSettled(portfolios.map(portfolio => getPortfolioSummary(portfolio.id))),
      ]);
      const [healthResult, cashflowResult, budgetsResult, goalsResult, dcaResult, rebalanceResult] = serviceResults;

      setData({
        summaries: summaryResults
          .filter((result): result is PromiseFulfilledResult<PortfolioSummaryDto> => result.status === 'fulfilled')
          .map(result => result.value),
        health: healthResult.status === 'fulfilled' ? healthResult.value : null,
        cashflow: cashflowResult.status === 'fulfilled' ? cashflowResult.value : null,
        budgets: budgetsResult.status === 'fulfilled' ? budgetsResult.value : [],
        goals: goalsResult.status === 'fulfilled' ? goalsResult.value : [],
        dcaPlans: dcaResult.status === 'fulfilled' ? dcaResult.value : [],
        rebalancePlans: rebalanceResult.status === 'fulfilled' ? rebalanceResult.value : [],
      });
    } catch (loadError) {
      console.error('Failed to load dashboard', loadError);
      setError('Không thể tải dashboard lúc này. Dữ liệu của bạn không bị thay đổi.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadDashboard();
  }, [loadDashboard]);

  const metrics = useMemo(() => {
    const invested = data.health?.investedValue ?? data.summaries.reduce((sum, item) => sum + item.totalInvested, 0);
    const unrealizedPnl = data.health?.unrealizedPnl ?? data.summaries.reduce((sum, item) => sum + item.unrealizedPnl, 0);
    const realizedPnl = data.summaries.reduce((sum, item) => sum + item.realizedPnl, 0);
    const portfolioValue = data.health
      ? data.health.investedValue + data.health.unrealizedPnl
      : data.summaries.reduce((sum, item) => sum + item.currentTotalValue, 0);
    const cashBalance = data.health?.cashBalance ?? data.summaries.reduce((sum, item) => (
      sum + item.cashBalances
        .filter(balance => balance.currency === 'VND')
        .reduce((cash, balance) => cash + balance.balance, 0)
    ), 0);
    const budgetLimit = data.health?.budgetLimit ?? data.budgets.reduce((sum, budget) => sum + budget.monthlyLimit, 0);
    const budgetSpent = data.health?.budgetSpent ?? data.budgets.reduce((sum, budget) => sum + budget.spentAmount, 0);
    const budgetProgress = budgetLimit > 0 ? (budgetSpent / budgetLimit) * 100 : 0;
    const monthlyIncome = data.health?.monthlyIncome ?? data.cashflow?.totalIncome ?? 0;
    const monthlyExpense = data.health?.monthlyExpense ?? data.cashflow?.totalExpense ?? 0;
    const monthlyNetFlow = data.health?.monthlyNetFlow ?? data.cashflow?.netFlow ?? 0;

    return {
      invested,
      portfolioValue,
      cashBalance,
      unrealizedPnl,
      realizedPnl,
      totalPnl: realizedPnl + unrealizedPnl,
      netWorth: portfolioValue + cashBalance,
      budgetLimit,
      budgetSpent,
      budgetProgress,
      monthlyIncome,
      monthlyExpense,
      monthlyNetFlow,
    };
  }, [data]);

  const risks = useMemo(() => ({
    exceededBudgets: data.budgets.filter(budget => budget.isExceeded),
    warningBudgets: data.budgets.filter(budget => !budget.isExceeded && budget.rawProgressPercentage >= 80),
    urgentGoals: data.goals.filter(goal => !goal.isCompleted && goal.daysRemaining <= 30 && goal.progressPercentage < 100),
    dueDca: data.dcaPlans.filter(plan => plan.isActive && isWithinDays(plan.nextExecutionDate, 7)),
    cashLimitedDca: data.dcaPlans.filter(plan => plan.isActive && !plan.hasEnoughCash),
    simulatedRebalance: data.rebalancePlans.filter(plan => plan.status === RebalanceExecutionPlanStatus.Simulated),
  }), [data]);

  const actionItems = useMemo<ActionItem[]>(() => [
    ...risks.exceededBudgets.slice(0, 3).map(item => ({
      title: `${item.categoryName} vượt ngân sách`,
      detail: `${item.rawProgressPercentage.toFixed(0)}% hạn mức tháng đã sử dụng`,
      tone: 'danger' as const,
      label: 'Ngân sách',
      to: '/budgets',
    })),
    ...risks.cashLimitedDca.slice(0, 2).map(item => ({
      title: `${item.symbol} chưa đủ tiền cho DCA`,
      detail: `Cần ${formatVnd(item.amount)} cho lần mua tiếp theo`,
      tone: 'warning' as const,
      label: 'DCA',
      to: '/dca-plans',
    })),
    ...risks.urgentGoals.slice(0, 2).map(item => ({
      title: `${item.name} sắp đến hạn`,
      detail: `Còn ${item.daysRemaining} ngày · đã đạt ${item.progressPercentage.toFixed(0)}%`,
      tone: 'warning' as const,
      label: 'Mục tiêu',
      to: '/saving-goals',
    })),
    ...risks.simulatedRebalance.slice(0, 2).map(item => ({
      title: 'Kế hoạch cân bằng lại đang chờ',
      detail: `${item.items.length} hành động · tiền khả dụng ${formatVnd(item.availableCash)}`,
      tone: 'info' as const,
      label: 'Cân bằng',
      to: '/rebalancing',
    })),
  ], [risks]);

  const activeDcaPlans = useMemo(
    () => data.dcaPlans.filter(plan => plan.isActive).slice(0, 4),
    [data.dcaPlans],
  );
  const openGoals = useMemo(
    () => data.goals.filter(goal => !goal.isCompleted).slice(0, 3),
    [data.goals],
  );
  const portfolioRows = useMemo(
    () => [...data.summaries].sort((a, b) => b.currentTotalValue - a.currentTotalValue).slice(0, 5),
    [data.summaries],
  );

  if (loading) {
    return (
      <main className="overview-dashboard container" aria-busy="true" aria-label="Đang tải dashboard">
        <div className="dashboard-skeleton skeleton-heading" />
        <div className="dashboard-skeleton skeleton-hero" />
        <div className="dashboard-skeleton skeleton-strip" />
        <div className="dashboard-skeleton skeleton-body" />
      </main>
    );
  }

  if (error) {
    return (
      <main className="overview-dashboard container">
        <section className="dashboard-error" role="alert">
          <span aria-hidden="true">!</span>
          <h1>Dashboard chưa sẵn sàng</h1>
          <p>{error}</p>
          <button className="dashboard-primary-action" type="button" onClick={() => void loadDashboard()}>
            Thử tải lại
          </button>
        </section>
      </main>
    );
  }

  const pnlPercentage = metrics.invested > 0 ? (metrics.totalPnl / metrics.invested) * 100 : null;
  const portfolioShare = metrics.netWorth > 0 ? (metrics.portfolioValue / metrics.netWorth) * 100 : 0;
  const boundedBudgetProgress = Math.min(Math.max(metrics.budgetProgress, 0), 100);
  const asOf = data.health?.asOf ?? data.summaries[0]?.asOf;

  return (
    <main className="overview-dashboard container">
      <header className="overview-masthead">
        <div>
          <p className="dashboard-overline">Tổng quan tài chính</p>
          <h1>Một màn hình. Mọi quyết định.</h1>
          <p className="overview-masthead-copy">
            Theo dõi tài sản, dòng tiền và các việc cần xử lý mà không rời khỏi dashboard.
          </p>
        </div>
        <div className="overview-masthead-actions">
          <Link to="/cashflow" className="dashboard-secondary-action">Ghi dòng tiền</Link>
          <Link to="/transactions" className="dashboard-primary-action">Thêm giao dịch</Link>
        </div>
      </header>

      <section className="net-worth-brief" aria-labelledby="net-worth-heading">
        <div className="net-worth-main">
          <div className="net-worth-label-row">
            <span id="net-worth-heading">Tài sản ròng</span>
            {asOf && <time dateTime={asOf}>Cập nhật {formatVietnamDateTime(asOf)}</time>}
          </div>
          <strong aria-live="polite">{formatVnd(metrics.netWorth)}</strong>
          <div className="net-worth-context">
            <span className={metrics.totalPnl >= 0 ? 'positive' : 'negative'}>
              {metrics.totalPnl >= 0 ? 'Lãi' : 'Lỗ'} {formatVnd(Math.abs(metrics.totalPnl))}
            </span>
            <span>{pnlPercentage === null ? 'Chưa có giá vốn' : `${formatPercent(pnlPercentage)} trên giá vốn`}</span>
          </div>
          <div className="asset-mix" aria-label={`Tỷ trọng đầu tư ${portfolioShare.toFixed(0)} phần trăm`}>
            <span style={{ transform: `scaleX(${Math.min(portfolioShare, 100) / 100})` }} />
          </div>
          <div className="asset-mix-labels">
            <span>Đầu tư {portfolioShare.toFixed(0)}%</span>
            <span>Tiền mặt {(100 - Math.min(portfolioShare, 100)).toFixed(0)}%</span>
          </div>
        </div>

        <dl className="net-worth-ledger">
          <div>
            <dt>Giá trị đầu tư</dt>
            <dd>{formatVnd(metrics.portfolioValue)}</dd>
            <small>Giá vốn {formatVnd(metrics.invested)}</small>
          </div>
          <div>
            <dt>Tiền mặt</dt>
            <dd>{formatVnd(metrics.cashBalance)}</dd>
            <small>{data.summaries.length} portfolio đang theo dõi</small>
          </div>
          <div>
            <dt>PnL chưa chốt</dt>
            <dd className={metrics.unrealizedPnl >= 0 ? 'positive' : 'negative'}>
              {metrics.unrealizedPnl > 0 ? '+' : ''}{formatVnd(metrics.unrealizedPnl)}
            </dd>
            <small>Đã chốt {formatVnd(metrics.realizedPnl)}</small>
          </div>
        </dl>
      </section>

      <section className="monthly-pulse" aria-labelledby="monthly-pulse-heading">
        <div className="monthly-pulse-heading">
          <h2 id="monthly-pulse-heading">Nhịp tiền tháng này</h2>
          <Link to="/cashflow" className="dashboard-text-link">Mở dòng tiền <span aria-hidden="true">→</span></Link>
        </div>
        <dl className="cashflow-ledger">
          <div>
            <dt>Thu nhập</dt>
            <dd>{formatVnd(metrics.monthlyIncome)}</dd>
          </div>
          <div>
            <dt>Chi tiêu</dt>
            <dd>{formatVnd(metrics.monthlyExpense)}</dd>
          </div>
          <div className="cashflow-net">
            <dt>Dòng tiền ròng</dt>
            <dd className={metrics.monthlyNetFlow >= 0 ? 'positive' : 'negative'}>
              {metrics.monthlyNetFlow > 0 ? '+' : ''}{formatVnd(metrics.monthlyNetFlow)}
            </dd>
          </div>
        </dl>
        <div className="budget-line">
          <div className="budget-line-copy">
            <span>Ngân sách đã dùng</span>
            <strong>{metrics.budgetProgress.toFixed(1)}%</strong>
            <small>{formatVnd(metrics.budgetSpent)} / {formatVnd(metrics.budgetLimit)}</small>
          </div>
          <div
            className={`budget-track ${metrics.budgetProgress >= 100 ? 'is-danger' : metrics.budgetProgress >= 80 ? 'is-warning' : ''}`}
            role="progressbar"
            aria-label="Tiến độ ngân sách tháng"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuenow={Math.round(metrics.budgetProgress)}
          >
            <span style={{ transform: `scaleX(${boundedBudgetProgress / 100})` }} />
          </div>
        </div>
      </section>

      <div className="decision-layout">
        <section className="decision-queue" aria-labelledby="decision-heading">
          <div className="section-heading-row">
            <h2 id="decision-heading">Cần bạn quyết định</h2>
            <span className="section-count">{actionItems.length}</span>
          </div>

          {actionItems.length === 0 ? (
            <div className="dashboard-empty">
              <span aria-hidden="true">OK</span>
              <div>
                <strong>Không có cảnh báo quan trọng</strong>
                <p>Ngân sách, DCA, mục tiêu và kế hoạch cân bằng hiện không cần xử lý.</p>
              </div>
              <Link to="/analytics" className="dashboard-text-link">Xem phân tích <span aria-hidden="true">→</span></Link>
            </div>
          ) : (
            <div className="decision-list">
              {actionItems.map((item, index) => (
                <Link key={`${item.title}-${index}`} to={item.to} className={`decision-row ${item.tone}`}>
                  <span className="decision-index">{String(index + 1).padStart(2, '0')}</span>
                  <span className="decision-copy">
                    <small>{item.label}</small>
                    <strong>{item.title}</strong>
                    <span>{item.detail}</span>
                  </span>
                  <span className="decision-arrow" aria-hidden="true">→</span>
                </Link>
              ))}
            </div>
          )}
        </section>

        <aside className="dashboard-side-index" aria-label="Lịch đầu tư và mục tiêu">
          <section className="side-index-section" aria-labelledby="dca-heading">
            <div className="side-index-heading">
              <h2 id="dca-heading">DCA sắp tới</h2>
              <Link to="/dca-plans">Tất cả</Link>
            </div>
            {activeDcaPlans.length === 0 ? (
              <div className="compact-empty">
                <span>Chưa có lịch DCA đang chạy.</span>
                <Link to="/dca-plans">Tạo lịch DCA</Link>
              </div>
            ) : (
              <div className="schedule-list">
                {activeDcaPlans.map(plan => (
                  <Link key={plan.id} to="/dca-plans" className="schedule-row">
                    <time dateTime={plan.nextExecutionDate}>
                      {new Date(plan.nextExecutionDate).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })}
                    </time>
                    <span>
                      <strong>{plan.symbol}</strong>
                      <small>{formatVnd(plan.amount)}</small>
                    </span>
                    <em className={plan.hasEnoughCash ? 'ready' : 'needs-cash'}>
                      {plan.hasEnoughCash ? 'Sẵn sàng' : 'Thiếu tiền'}
                    </em>
                  </Link>
                ))}
              </div>
            )}
          </section>

          <section className="side-index-section" aria-labelledby="goals-heading">
            <div className="side-index-heading">
              <h2 id="goals-heading">Mục tiêu tiết kiệm</h2>
              <Link to="/saving-goals">Tất cả</Link>
            </div>
            {openGoals.length === 0 ? (
              <div className="compact-empty">
                <span>Chưa có mục tiêu đang theo đuổi.</span>
                <Link to="/saving-goals">Tạo mục tiêu</Link>
              </div>
            ) : (
              <div className="goal-list">
                {openGoals.map(goal => (
                  <Link key={goal.id} to="/saving-goals" className="goal-row">
                    <span className="goal-copy">
                      <strong>{goal.name}</strong>
                      <small>Cần {formatVnd(goal.monthlyRequiredSaving)}/tháng</small>
                    </span>
                    <span className="goal-progress-copy">{goal.progressPercentage.toFixed(0)}%</span>
                    <span className="goal-track" aria-hidden="true">
                      <span style={{ transform: `scaleX(${Math.min(Math.max(goal.progressPercentage, 0), 100) / 100})` }} />
                    </span>
                  </Link>
                ))}
              </div>
            )}
          </section>
        </aside>
      </div>

      <section className="portfolio-index" aria-labelledby="portfolio-index-heading">
        <div className="section-heading-row">
          <h2 id="portfolio-index-heading">Portfolio đang theo dõi</h2>
          <Link to="/portfolios" className="dashboard-text-link">Quản lý portfolio <span aria-hidden="true">→</span></Link>
        </div>

        {portfolioRows.length === 0 ? (
          <div className="dashboard-empty">
            <span aria-hidden="true">01</span>
            <div>
              <strong>Chưa có portfolio</strong>
              <p>Tạo portfolio đầu tiên để bắt đầu theo dõi tài sản và PnL.</p>
            </div>
            <Link to="/portfolios" className="dashboard-text-link">Tạo portfolio <span aria-hidden="true">→</span></Link>
          </div>
        ) : (
          <div className="portfolio-list">
            {portfolioRows.map((portfolio, index) => {
              const pnl = portfolio.realizedPnl + portfolio.unrealizedPnl;
              return (
                <Link key={portfolio.portfolioId} to={`/portfolios/${portfolio.portfolioId}`} className="portfolio-row">
                  <span className="portfolio-rank">{String(index + 1).padStart(2, '0')}</span>
                  <span className="portfolio-name">
                    <strong>{portfolio.name}</strong>
                    <small>{portfolio.assets.length} tài sản</small>
                  </span>
                  <span className="portfolio-value">
                    <small>Giá trị hiện tại</small>
                    <strong>{formatVnd(portfolio.currentTotalValue)}</strong>
                  </span>
                  <span className={`portfolio-pnl ${pnl >= 0 ? 'positive' : 'negative'}`}>
                    <small>Tổng PnL</small>
                    <strong>{pnl > 0 ? '+' : ''}{formatVnd(pnl)}</strong>
                  </span>
                  <span className="portfolio-arrow" aria-hidden="true">→</span>
                </Link>
              );
            })}
          </div>
        )}
      </section>
    </main>
  );
}
