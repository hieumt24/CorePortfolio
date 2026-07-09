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

  return (
    <div className="rebalancing-page container">
      <div className="rebalancing-header">
        <div>
          <h1>Rebalancing Execution Plans</h1>
          <p>Bien goi y tai can bang thanh cac buoc mua/ban co tinh den cash balance.</p>
        </div>
        <div className="rebalancing-controls">
          <select value={currency} onChange={e => setCurrency(e.target.value)}>
            <option value="VND">VND</option>
            <option value="USD">USD</option>
          </select>
          <button className="btn btn-primary" onClick={simulatePlan} disabled={simulating}>
            {simulating ? 'Dang simulate...' : 'Simulate plan'}
          </button>
        </div>
      </div>

      {loading ? (
        <div className="glass-panel rebalancing-empty">Dang tai du lieu...</div>
      ) : !latestPlan ? (
        <div className="glass-panel rebalancing-empty">Chua co execution plan. Hay simulate plan dau tien.</div>
      ) : (
        <>
          <section className="rebalance-summary glass-panel">
            <div>
              <span>Plan gan nhat</span>
              <strong>{new Date(latestPlan.createdAt).toLocaleString()}</strong>
            </div>
            <div>
              <span>Trang thai</span>
              <strong>{latestPlan.status === RebalanceExecutionPlanStatus.Applied ? 'Applied' : 'Simulated'}</strong>
            </div>
            <div>
              <span>Cash kha dung</span>
              <strong>{formatCurrency(latestPlan.availableCash, latestPlan.currency)}</strong>
            </div>
            <div>
              <span>So buoc</span>
              <strong>{latestPlan.items.length}</strong>
            </div>
          </section>

          <section className="rebalance-plan-card glass-panel">
            <div className="rebalance-plan-header">
              <div>
                <h2>Execution steps</h2>
                {latestPlan.notes && <p>{latestPlan.notes}</p>}
              </div>
              {latestPlan.status === RebalanceExecutionPlanStatus.Simulated && (
                <button className="btn btn-outline" onClick={() => applyPlan(latestPlan.id)}>
                  Mark as applied
                </button>
              )}
            </div>

            {latestPlan.items.length === 0 ? (
              <div className="rebalancing-empty compact">Danh muc dang can bang theo muc tieu hien tai.</div>
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
                          <span>Hien tai</span>
                          <strong>{formatCurrency(item.currentValue, latestPlan.currency)}</strong>
                        </div>
                        <div>
                          <span>Muc tieu</span>
                          <strong>{formatCurrency(item.targetValue, latestPlan.currency)}</strong>
                        </div>
                        <div>
                          <span>De xuat</span>
                          <strong>{formatCurrency(item.suggestedAmount, latestPlan.currency)}</strong>
                        </div>
                        <div>
                          <span>Co the thuc hien</span>
                          <strong>{formatCurrency(item.executableAmount, latestPlan.currency)}</strong>
                        </div>
                      </div>
                      {item.isCashLimited && <p className="cash-limited">Bi gioi han boi cash kha dung.</p>}
                    </div>
                  </article>
                ))}
              </div>
            )}
          </section>

          {plans.length > 1 && (
            <section className="rebalance-history glass-panel">
              <h2>Plan history</h2>
              {plans.slice(1).map(plan => (
                <div key={plan.id} className="history-row">
                  <span>{new Date(plan.createdAt).toLocaleString()}</span>
                  <span>{plan.currency}</span>
                  <span>{plan.status === RebalanceExecutionPlanStatus.Applied ? 'Applied' : 'Simulated'}</span>
                  <span>{plan.items.length} buoc</span>
                </div>
              ))}
            </section>
          )}
        </>
      )}
    </div>
  );
};
