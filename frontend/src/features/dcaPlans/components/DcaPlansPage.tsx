import React, { useEffect, useMemo, useState } from 'react';
import { getPortfolios } from '../../portfolios/api/portfolioApi';
import type { PortfolioDto } from '../../portfolios/types';
import { dcaPlansApi } from '../api/dcaPlansApi';
import { DcaFrequency, type DcaMarketAsset, type DcaPlan, type SaveDcaPlanRequest } from '../types';
import './DcaPlansPage.css';

const toDateInput = (date: Date | string) => new Date(date).toISOString().slice(0, 10);

const formatCurrency = (amount: number, currency: string) =>
  new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 4,
  }).format(amount);

const frequencyLabel = (frequency: DcaFrequency) => {
  if (frequency === DcaFrequency.Weekly) return 'Hang tuan';
  if (frequency === DcaFrequency.Quarterly) return 'Hang quy';
  return 'Hang thang';
};

const buildDefaultForm = (): SaveDcaPlanRequest => ({
  portfolioId: '',
  marketAssetId: '',
  amount: 0,
  currency: 'VND',
  frequency: DcaFrequency.Monthly,
  startDate: toDateInput(new Date()),
  nextExecutionDate: toDateInput(new Date()),
  endDate: null,
  isActive: true,
  notes: '',
});

export const DcaPlansPage: React.FC = () => {
  const [plans, setPlans] = useState<DcaPlan[]>([]);
  const [portfolios, setPortfolios] = useState<PortfolioDto[]>([]);
  const [marketAssets, setMarketAssets] = useState<DcaMarketAsset[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<SaveDcaPlanRequest>(buildDefaultForm);

  const selectedAsset = useMemo(
    () => marketAssets.find(asset => asset.id === form.marketAssetId),
    [marketAssets, form.marketAssetId]
  );

  const fetchData = async () => {
    try {
      setLoading(true);
      const [planData, portfolioData, assetData] = await Promise.all([
        dcaPlansApi.getPlans(),
        getPortfolios(),
        dcaPlansApi.getMarketAssets(),
      ]);
      setPlans(planData);
      setPortfolios(portfolioData);
      setMarketAssets(assetData);
      setForm(prev => ({
        ...prev,
        portfolioId: prev.portfolioId || portfolioData[0]?.id || '',
        marketAssetId: prev.marketAssetId || assetData[0]?.id || '',
        currency: assetData.find(asset => asset.id === (prev.marketAssetId || assetData[0]?.id))?.currency || prev.currency,
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
    if (!form.portfolioId || !form.marketAssetId || form.amount <= 0) return;

    try {
      setSaving(true);
      const request = {
        ...form,
        startDate: new Date(form.startDate).toISOString(),
        nextExecutionDate: new Date(form.nextExecutionDate).toISOString(),
        endDate: form.endDate ? new Date(form.endDate).toISOString() : null,
      };

      if (editingId) {
        await dcaPlansApi.updatePlan(editingId, request);
      } else {
        await dcaPlansApi.createPlan(request);
      }

      setEditingId(null);
      setForm(prev => ({
        ...buildDefaultForm(),
        portfolioId: prev.portfolioId,
        marketAssetId: prev.marketAssetId,
        currency: prev.currency,
      }));
      await fetchData();
    } finally {
      setSaving(false);
    }
  };

  const editPlan = (plan: DcaPlan) => {
    setEditingId(plan.id);
    setForm({
      portfolioId: plan.portfolioId,
      marketAssetId: plan.marketAssetId,
      amount: plan.amount,
      currency: plan.currency,
      frequency: plan.frequency,
      startDate: toDateInput(plan.startDate),
      nextExecutionDate: toDateInput(plan.nextExecutionDate),
      endDate: plan.endDate ? toDateInput(plan.endDate) : null,
      isActive: plan.isActive,
      notes: plan.notes,
    });
  };

  const deletePlan = async (id: string) => {
    await dcaPlansApi.deletePlan(id);
    await fetchData();
  };

  return (
    <div className="dca-plans-page container">
      <div className="dca-header">
        <div>
          <h1>DCA Plans</h1>
          <p>Lap lich dau tu dinh ky, xem tien mat kha dung va uoc tinh khoi luong mua.</p>
        </div>
      </div>

      <form className="dca-form glass-panel" onSubmit={handleSubmit}>
        <div className="dca-form-grid">
          <div className="form-group">
            <label>Portfolio</label>
            <select
              value={form.portfolioId}
              onChange={e => setForm(prev => ({ ...prev, portfolioId: e.target.value }))}
            >
              {portfolios.map(portfolio => (
                <option key={portfolio.id} value={portfolio.id}>{portfolio.name}</option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>Tai san</label>
            <select
              value={form.marketAssetId}
              onChange={e => {
                const asset = marketAssets.find(item => item.id === e.target.value);
                setForm(prev => ({ ...prev, marketAssetId: e.target.value, currency: asset?.currency || prev.currency }));
              }}
            >
              {marketAssets.map(asset => (
                <option key={asset.id} value={asset.id}>
                  {asset.symbol} - {asset.name}
                </option>
              ))}
            </select>
          </div>
          <div className="form-group">
            <label>So tien moi ky</label>
            <input
              type="number"
              min="0"
              value={form.amount || ''}
              onChange={e => setForm(prev => ({ ...prev, amount: Number(e.target.value) }))}
              placeholder="5000000"
            />
          </div>
          <div className="form-group">
            <label>Tien te</label>
            <select value={form.currency} onChange={e => setForm(prev => ({ ...prev, currency: e.target.value }))}>
              <option value="VND">VND</option>
              <option value="USD">USD</option>
            </select>
          </div>
          <div className="form-group">
            <label>Tan suat</label>
            <select
              value={form.frequency}
              onChange={e => setForm(prev => ({ ...prev, frequency: Number(e.target.value) as DcaFrequency }))}
            >
              <option value={DcaFrequency.Weekly}>Hang tuan</option>
              <option value={DcaFrequency.Monthly}>Hang thang</option>
              <option value={DcaFrequency.Quarterly}>Hang quy</option>
            </select>
          </div>
          <div className="form-group">
            <label>Ngay bat dau</label>
            <input
              type="date"
              value={form.startDate}
              onChange={e => setForm(prev => ({ ...prev, startDate: e.target.value }))}
            />
          </div>
          <div className="form-group">
            <label>Ngay mua tiep theo</label>
            <input
              type="date"
              value={form.nextExecutionDate}
              onChange={e => setForm(prev => ({ ...prev, nextExecutionDate: e.target.value }))}
            />
          </div>
          <div className="form-group">
            <label>Ngay ket thuc</label>
            <input
              type="date"
              value={form.endDate || ''}
              onChange={e => setForm(prev => ({ ...prev, endDate: e.target.value || null }))}
            />
          </div>
          <div className="form-group dca-notes">
            <label>Ghi chu</label>
            <input
              value={form.notes}
              onChange={e => setForm(prev => ({ ...prev, notes: e.target.value }))}
              placeholder="VD: uu tien khi allocation thap hon muc tieu"
            />
          </div>
        </div>

        <div className="dca-form-footer">
          <div className="dca-estimate">
            {selectedAsset
              ? `Gia hien tai ${formatCurrency(selectedAsset.currentPrice, form.currency)} - uoc tinh ${(selectedAsset.currentPrice > 0 ? form.amount / selectedAsset.currentPrice : 0).toFixed(6)} don vi`
              : 'Chon tai san de xem uoc tinh'}
          </div>
          <button className="btn btn-primary" disabled={saving || portfolios.length === 0 || marketAssets.length === 0}>
            {saving ? 'Dang luu...' : editingId ? 'Cap nhat DCA' : 'Tao DCA plan'}
          </button>
        </div>
      </form>

      {loading ? (
        <div className="glass-panel dca-empty">Dang tai du lieu...</div>
      ) : plans.length === 0 ? (
        <div className="glass-panel dca-empty">Chua co DCA plan nao.</div>
      ) : (
        <div className="dca-plan-grid">
          {plans.map(plan => (
            <article key={plan.id} className={`dca-plan-card glass-panel ${plan.isActive ? '' : 'inactive'}`}>
              <div className="dca-plan-heading">
                <div>
                  <span className="dca-symbol">{plan.symbol}</span>
                  <h2>{plan.assetName}</h2>
                  <p>{plan.portfolioName} - {plan.categoryName}</p>
                </div>
                <span className={plan.hasEnoughCash ? 'cash-pill ok' : 'cash-pill warn'}>
                  {plan.hasEnoughCash ? 'Du cash' : 'Thieu cash'}
                </span>
              </div>

              <div className="dca-amount-row">
                <strong>{formatCurrency(plan.amount, plan.currency)}</strong>
                <span>{frequencyLabel(plan.frequency)}</span>
              </div>

              <div className="dca-metrics">
                <div>
                  <span>Gia hien tai</span>
                  <strong>{formatCurrency(plan.currentPrice, plan.currency)}</strong>
                </div>
                <div>
                  <span>Uoc tinh mua</span>
                  <strong>{plan.estimatedQuantity.toFixed(6)}</strong>
                </div>
                <div>
                  <span>Cash balance</span>
                  <strong>{formatCurrency(plan.cashBalance, plan.currency)}</strong>
                </div>
              </div>

              <div className="dca-calendar">
                <span>Lich sap toi</span>
                <div>
                  {plan.upcomingExecutions.map(date => (
                    <time key={date}>{toDateInput(date)}</time>
                  ))}
                </div>
              </div>

              {plan.notes && <p className="dca-note">{plan.notes}</p>}

              <div className="dca-actions">
                <button className="btn btn-outline btn-sm" onClick={() => editPlan(plan)}>Sua</button>
                <button className="btn btn-outline btn-sm danger" onClick={() => deletePlan(plan.id)}>Xoa</button>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
};
