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
  if (frequency === DcaFrequency.Weekly) return 'Hàng tuần';
  if (frequency === DcaFrequency.Quarterly) return 'Hàng quý';
  return 'Hàng tháng';
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

  const planSummary = useMemo(() => {
    const activePlans = plans.filter(plan => plan.isActive);
    const nextPlan = activePlans
      .slice()
      .sort((a, b) => new Date(a.nextExecutionDate).getTime() - new Date(b.nextExecutionDate).getTime())[0];

    return {
      activeCount: activePlans.length,
      totalPerCycle: activePlans.reduce((sum, plan) => sum + plan.amount, 0),
      currency: activePlans[0]?.currency || form.currency,
      nextExecutionDate: nextPlan?.nextExecutionDate,
    };
  }, [plans, form.currency]);

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
      <div className="dca-header dca-hero">
        <div>
          <span className="page-kicker">Kỷ luật đầu tư</span>
          <h1>DCA Plans</h1>
          <p>Lập lịch đầu tư định kỳ, kiểm tra tiền mặt khả dụng và ước tính khối lượng mua trước mỗi kỳ.</p>
        </div>
        <div className="dca-hero-stats">
          <div>
            <span>Plan đang chạy</span>
            <strong>{planSummary.activeCount}</strong>
          </div>
          <div>
            <span>Tổng tiền mỗi kỳ</span>
            <strong>{formatCurrency(planSummary.totalPerCycle, planSummary.currency)}</strong>
          </div>
          <div>
            <span>Lần mua gần nhất</span>
            <strong>{planSummary.nextExecutionDate ? toDateInput(planSummary.nextExecutionDate) : 'Chưa có'}</strong>
          </div>
        </div>
      </div>

      <form className="dca-form glass-panel" onSubmit={handleSubmit}>
        <div className="dca-form-title">
          <div>
            <h2>{editingId ? 'Cập nhật DCA plan' : 'Tạo DCA plan mới'}</h2>
            <p>Chọn tài sản, tần suất và ngày mua tiếp theo. App sẽ kiểm tra số dư cash cho từng plan.</p>
          </div>
          {editingId && (
            <button
              className="btn btn-outline btn-sm"
              type="button"
              onClick={() => {
                setEditingId(null);
                setForm(prev => ({
                  ...buildDefaultForm(),
                  portfolioId: prev.portfolioId,
                  marketAssetId: prev.marketAssetId,
                  currency: prev.currency,
                }));
              }}
            >
              Hủy sửa
            </button>
          )}
        </div>
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
            <label>Tài sản</label>
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
            <label>Số tiền mỗi kỳ</label>
            <input
              type="number"
              min="0"
              value={form.amount || ''}
              onChange={e => setForm(prev => ({ ...prev, amount: Number(e.target.value) }))}
              placeholder="5000000"
            />
          </div>
          <div className="form-group">
            <label>Tiền tệ</label>
            <select value={form.currency} onChange={e => setForm(prev => ({ ...prev, currency: e.target.value }))}>
              <option value="VND">VND</option>
              <option value="USD">USD</option>
            </select>
          </div>
          <div className="form-group">
            <label>Tần suất</label>
            <select
              value={form.frequency}
              onChange={e => setForm(prev => ({ ...prev, frequency: Number(e.target.value) as DcaFrequency }))}
            >
              <option value={DcaFrequency.Weekly}>Hàng tuần</option>
              <option value={DcaFrequency.Monthly}>Hàng tháng</option>
              <option value={DcaFrequency.Quarterly}>Hàng quý</option>
            </select>
          </div>
          <div className="form-group">
            <label>Ngày bắt đầu</label>
            <input
              type="date"
              value={form.startDate}
              onChange={e => setForm(prev => ({ ...prev, startDate: e.target.value }))}
            />
          </div>
          <div className="form-group">
            <label>Ngày mua tiếp theo</label>
            <input
              type="date"
              value={form.nextExecutionDate}
              onChange={e => setForm(prev => ({ ...prev, nextExecutionDate: e.target.value }))}
            />
          </div>
          <div className="form-group">
            <label>Ngày kết thúc</label>
            <input
              type="date"
              value={form.endDate || ''}
              onChange={e => setForm(prev => ({ ...prev, endDate: e.target.value || null }))}
            />
          </div>
          <div className="form-group dca-notes">
            <label>Ghi chú</label>
            <input
              value={form.notes}
              onChange={e => setForm(prev => ({ ...prev, notes: e.target.value }))}
              placeholder="VD: ưu tiên khi allocation thấp hơn mục tiêu"
            />
          </div>
        </div>

        <div className="dca-form-footer">
          <div className="dca-estimate">
            {selectedAsset
              ? `Giá hiện tại ${formatCurrency(selectedAsset.currentPrice, form.currency)} - ước tính ${(selectedAsset.currentPrice > 0 ? form.amount / selectedAsset.currentPrice : 0).toFixed(6)} đơn vị`
              : 'Chọn tài sản để xem ước tính'}
          </div>
          <button className="btn btn-primary" disabled={saving || portfolios.length === 0 || marketAssets.length === 0}>
            {saving ? 'Đang lưu...' : editingId ? 'Cập nhật DCA' : 'Tạo DCA plan'}
          </button>
        </div>
      </form>

      {loading ? (
        <div className="glass-panel dca-empty">Đang tải dữ liệu...</div>
      ) : plans.length === 0 ? (
        <div className="glass-panel dca-empty">
          <strong>Chưa có DCA plan nào.</strong>
          <span>Tạo plan đầu tiên để biến chiến lược đầu tư định kỳ thành lịch hành động rõ ràng.</span>
        </div>
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
                  <span>Giá hiện tại</span>
                  <strong>{formatCurrency(plan.currentPrice, plan.currency)}</strong>
                </div>
                <div>
                  <span>Ước tính mua</span>
                  <strong>{plan.estimatedQuantity.toFixed(6)}</strong>
                </div>
                <div>
                  <span>Cash balance</span>
                  <strong>{formatCurrency(plan.cashBalance, plan.currency)}</strong>
                </div>
              </div>

              <div className="dca-calendar">
                <span>Lịch sắp tới</span>
                <div>
                  {plan.upcomingExecutions.map(date => (
                    <time key={date}>{toDateInput(date)}</time>
                  ))}
                </div>
              </div>

              {plan.notes && <p className="dca-note">{plan.notes}</p>}

              <div className="dca-actions">
                <button className="btn btn-outline btn-sm" onClick={() => editPlan(plan)}>Sửa</button>
                <button className="btn btn-outline btn-sm danger" onClick={() => deletePlan(plan.id)}>Xóa</button>
              </div>
            </article>
          ))}
        </div>
      )}
    </div>
  );
};
