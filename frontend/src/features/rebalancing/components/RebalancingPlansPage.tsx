import React, { useEffect, useState } from 'react';
import { rebalancingPlansApi } from '../api/rebalancingPlansApi';
import {
  RebalanceExecutionAction,
  RebalanceExecutionPlanStatus,
  type RebalanceExecutionPlan,
} from '../types';
import './RebalancingPlansPage.css';

const formatCurrency = (amount: number, currency: string) =>
  new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
  }).format(amount);

const actionLabel = (action: RebalanceExecutionAction) =>
  action === RebalanceExecutionAction.Buy ? 'Mua' : 'Ban';

export const RebalancingPlansPage: React.FC = () => {
  const [plans, setPlans] = useState<RebalanceExecutionPlan[]>([]);
  const [currency, setCurrency] = useState('VND');
  const [loading, setLoading] = useState(true);
  const [simulating, setSimulating] = useState(false);

  const fetchPlans = async () => {
    try {
      setLoading(true);
      setPlans(await rebalancingPlansApi.getPlans());
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchPlans();
  }, []);

  const simulatePlan = async () => {
    try {
      setSimulating(true);
      const plan = await rebalancingPlansApi.simulatePlan(currency);
      setPlans(prev => [plan, ...prev.filter(item => item.id !== plan.id)]);
    } finally {
      setSimulating(false);
    }
  };

  const applyPlan = async (id: string) => {
    await rebalancingPlansApi.applyPlan(id);
    await fetchPlans();
  };

  const latestPlan = plans[0];
  const latestPlanStats = latestPlan
    ? {
        buyCount: latestPlan.items.filter(item => item.action === RebalanceExecutionAction.Buy).length,
        sellCount: latestPlan.items.filter(item => item.action === RebalanceExecutionAction.Sell).length,
        executableTotal: latestPlan.items.reduce((sum, item) => sum + item.executableAmount, 0),
        limitedCount: latestPlan.items.filter(item => item.isCashLimited).length,
      }
    : null;

  return (
    <div className="rebalancing-page container">
      <div className="rebalancing-header rebalancing-hero">
        <div>
          <span className="page-kicker">Kế hoạch hành động</span>
          <h1>Rebalancing</h1>
          <p>Biến gợi ý tái cân bằng thành các bước mua/bán cụ thể, có tính đến lượng tiền mặt khả dụng.</p>
        </div>
        <div className="rebalancing-controls">
          <select value={currency} onChange={e => setCurrency(e.target.value)}>
            <option value="VND">VND</option>
            <option value="USD">USD</option>
          </select>
          <button className="btn btn-primary" onClick={simulatePlan} disabled={simulating}>
            {simulating ? 'Đang mô phỏng...' : 'Mô phỏng plan'}
          </button>
        </div>
      </div>

      {loading ? (
        <div className="glass-panel rebalancing-empty">Đang tải dữ liệu...</div>
      ) : !latestPlan ? (
        <div className="glass-panel rebalancing-empty">
          <strong>Chưa có execution plan.</strong>
          <span>Hãy mô phỏng plan đầu tiên để xem danh mục nên mua/bán theo thứ tự nào.</span>
        </div>
      ) : (
        <>
          <section className="rebalance-summary glass-panel">
            <div>
              <span>Plan gần nhất</span>
              <strong>{new Date(latestPlan.createdAt).toLocaleString()}</strong>
            </div>
            <div>
              <span>Trạng thái</span>
              <strong>{latestPlan.status === RebalanceExecutionPlanStatus.Applied ? 'Đã áp dụng' : 'Mô phỏng'}</strong>
            </div>
            <div>
              <span>Cash khả dụng</span>
              <strong>{formatCurrency(latestPlan.availableCash, latestPlan.currency)}</strong>
            </div>
            <div>
              <span>Có thể thực hiện</span>
              <strong>{formatCurrency(latestPlanStats?.executableTotal || 0, latestPlan.currency)}</strong>
            </div>
          </section>

          <section className="rebalance-plan-card glass-panel">
            <div className="rebalance-plan-header">
              <div>
                <h2>Các bước thực hiện</h2>
                <p>
                  {latestPlanStats?.sellCount || 0} bước bán, {latestPlanStats?.buyCount || 0} bước mua
                  {latestPlanStats?.limitedCount ? `, ${latestPlanStats.limitedCount} bước bị giới hạn bởi cash` : ''}.
                </p>
                {latestPlan.notes && <p>{latestPlan.notes}</p>}
              </div>
              {latestPlan.status === RebalanceExecutionPlanStatus.Simulated && (
                <button className="btn btn-outline" onClick={() => applyPlan(latestPlan.id)}>
                  Đánh dấu đã áp dụng
                </button>
              )}
            </div>

            {latestPlan.items.length === 0 ? (
              <div className="rebalancing-empty compact">Danh mục đang cân bằng theo mục tiêu hiện tại.</div>
            ) : (
              <div className="rebalance-steps">
                {latestPlan.items.map(item => (
                  <article key={item.id} className="rebalance-step">
                    <div className="step-priority">{item.priority}</div>
                    <div className="step-main">
                      <div className="step-title-row">
                        <h3>{actionLabel(item.action)} {item.categoryName}</h3>
                        <span className={item.action === RebalanceExecutionAction.Buy ? 'action-pill buy' : 'action-pill sell'}>
                          {actionLabel(item.action)}
                        </span>
                      </div>
                      <div className="step-values">
                        <div>
                          <span>Hiện tại</span>
                          <strong>{formatCurrency(item.currentValue, latestPlan.currency)}</strong>
                        </div>
                        <div>
                          <span>Mục tiêu</span>
                          <strong>{formatCurrency(item.targetValue, latestPlan.currency)}</strong>
                        </div>
                        <div>
                          <span>Đề xuất</span>
                          <strong>{formatCurrency(item.suggestedAmount, latestPlan.currency)}</strong>
                        </div>
                        <div>
                          <span>Có thể thực hiện</span>
                          <strong>{formatCurrency(item.executableAmount, latestPlan.currency)}</strong>
                        </div>
                      </div>
                      {item.isCashLimited && <p className="cash-limited">Bị giới hạn bởi cash khả dụng.</p>}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>

          {plans.length > 1 && (
            <section className="rebalance-history glass-panel">
              <h2>Lịch sử plan</h2>
              {plans.slice(1).map(plan => (
                <div key={plan.id} className="history-row">
                  <span>{new Date(plan.createdAt).toLocaleString()}</span>
                  <span>{plan.currency}</span>
                  <span>{plan.status === RebalanceExecutionPlanStatus.Applied ? 'Đã áp dụng' : 'Mô phỏng'}</span>
                  <span>{plan.items.length} bước</span>
                </div>
              ))}
            </section>
          )}
        </>
      )}
    </div>
  );
};
