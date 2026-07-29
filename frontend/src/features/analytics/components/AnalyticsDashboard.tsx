import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { analyticsApi } from '../api/analyticsApi';
import type { 
  CashflowMonthlyAnalyticsDto, 
  AssetAllocationDto, 
  PerformanceAnalyticsDto, 
  DividendMonthlyAnalyticsDto,
  PerformanceDataQualityDto,
  RebalanceAssessmentDto
} from '../types';
import { 
  PieChart, Pie, Cell, Tooltip as RechartsTooltip, ResponsiveContainer, Legend,
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  LineChart, Line
} from 'recharts';
import { TargetAllocationModal } from './TargetAllocationModal';
import { CashflowHeatmap } from './CashflowHeatmap';
import { DashboardSkeleton } from '../../../shared/components/Skeleton';
import '../../cashflows/components/CashflowDashboard.css'; // Re-use styling
import './AnalyticsDashboard.css';
import { useNotification } from '../../../context/NotificationContext';

type AnalyticsSection = 'cashflow' | 'allocation' | 'performance' | 'dividend' | 'rebalancing' | 'quality';
type AnalyticsErrors = Partial<Record<AnalyticsSection, string>>;

const getErrorMessage = (reason: unknown, fallback: string) =>
  reason instanceof Error && reason.message ? reason.message : fallback;

export const AnalyticsDashboard: React.FC = () => {
  const { showNotification } = useNotification();
  const [currency, setCurrency] = useState('VND');
  const [cashflowData, setCashflowData] = useState<CashflowMonthlyAnalyticsDto[]>([]);
  const [allocationData, setAllocationData] = useState<AssetAllocationDto[]>([]);
  const [performanceData, setPerformanceData] = useState<PerformanceAnalyticsDto | null>(null);
  const [dividendData, setDividendData] = useState<DividendMonthlyAnalyticsDto[]>([]);
  const [rebalanceAssessment, setRebalanceAssessment] = useState<RebalanceAssessmentDto | null>(null);
  const [dataQuality, setDataQuality] = useState<PerformanceDataQualityDto | null>(null);
  const [errors, setErrors] = useState<AnalyticsErrors>({});
  const [loading, setLoading] = useState(true);
  const [reloadKey, setReloadKey] = useState(0);
  const [isSnapshotLoading, setIsSnapshotLoading] = useState(false);
  const [isTargetModalOpen, setIsTargetModalOpen] = useState(false);

  useEffect(() => {
    let active = true;

    const fetchData = async () => {
      setLoading(true);
      setErrors({});

      const results = await Promise.allSettled([
        analyticsApi.getCashflowAnalytics(6, currency),
        analyticsApi.getAssetAllocation(currency),
        analyticsApi.getPerformanceAnalytics(currency),
        analyticsApi.getDividendAnalytics(12, currency),
        analyticsApi.getRebalanceSuggestions(currency),
        analyticsApi.getPerformanceDataQuality()
      ] as const);
      if (!active) return;

      const nextErrors: AnalyticsErrors = {};
      const [cashflow, allocation, performance, dividend, rebalancing, quality] = results;

      if (cashflow.status === 'fulfilled') setCashflowData(cashflow.value);
      else nextErrors.cashflow = getErrorMessage(cashflow.reason, 'Không thể tải phân tích dòng tiền.');

      if (allocation.status === 'fulfilled') setAllocationData(allocation.value);
      else nextErrors.allocation = getErrorMessage(allocation.reason, 'Không thể tải phân bổ tài sản.');

      if (performance.status === 'fulfilled') setPerformanceData(performance.value);
      else nextErrors.performance = getErrorMessage(performance.reason, 'Không thể tải lịch sử hiệu suất.');

      if (dividend.status === 'fulfilled') setDividendData(dividend.value);
      else nextErrors.dividend = getErrorMessage(dividend.reason, 'Không thể tải thu nhập đầu tư.');

      if (rebalancing.status === 'fulfilled') setRebalanceAssessment(rebalancing.value);
      else nextErrors.rebalancing = getErrorMessage(rebalancing.reason, 'Không thể đánh giá tái cân bằng.');

      if (quality.status === 'fulfilled') setDataQuality(quality.value);
      else nextErrors.quality = getErrorMessage(quality.reason, 'Không thể kiểm tra chất lượng dữ liệu.');

      setErrors(nextErrors);
      setLoading(false);
    };

    void fetchData();
    return () => {
      active = false;
    };
  }, [currency, reloadKey]);

  const reloadAllocations = async () => {
    try {
      const [alloc, suggestions] = await Promise.all([
        analyticsApi.getAssetAllocation(currency),
        analyticsApi.getRebalanceSuggestions(currency)
      ]);
      setAllocationData(alloc);
      setRebalanceAssessment(suggestions);
      setErrors(current => ({ ...current, allocation: undefined, rebalancing: undefined }));
    } catch (error) {
      const message = getErrorMessage(error, 'Không thể cập nhật phân bổ và đánh giá tái cân bằng.');
      setErrors(current => ({ ...current, allocation: message, rebalancing: message }));
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
      style: 'currency',
      currency: currency,
    }).format(amount);
  };

  const renderTooltipFormatter = (value: any) => {
    return formatCurrency(Number(value));
  };

  const renderSectionError = (section: AnalyticsSection) => {
    const message = errors[section];
    if (!message) return null;

    return (
      <div className="analytics-section-error" role="alert">
        <div>
          <strong>Không tải được dữ liệu</strong>
          <p>{message}</p>
        </div>
        <button type="button" onClick={() => setReloadKey(value => value + 1)}>
          Thử lại
        </button>
      </div>
    );
  };

  const handleTakeSnapshot = async () => {
    try {
      setIsSnapshotLoading(true);
      await analyticsApi.triggerSnapshot();
      showNotification('Đã cập nhật giá trị danh mục thành công!', 'success');
      // Reload performance data
      const perf = await analyticsApi.getPerformanceAnalytics(currency);
      setPerformanceData(perf);
      const quality = await analyticsApi.getPerformanceDataQuality();
      setDataQuality(quality);
      setErrors(current => ({ ...current, performance: undefined, quality: undefined }));
    } catch (error) {
      showNotification('Có lỗi xảy ra khi cập nhật giá trị danh mục.', 'error');
    } finally {
      setIsSnapshotLoading(false);
    }
  };

  if (loading) {
    return <DashboardSkeleton />;
  }

  return (
    <div className="cashflow-dashboard" style={{ paddingBottom: '3rem' }}>
      <div className="dashboard-header">
        <div className="header-title">
          <h1>📊 Phân tích tài chính</h1>
          <p className="subtitle">Theo dõi hiệu suất, dòng tiền và mức độ phù hợp với mục tiêu của bạn</p>
        </div>
        <div className="header-actions">
          <Link
            className="btn-secondary"
            style={{ padding: '0.55rem 0.9rem', fontSize: '0.875rem', textDecoration: 'none' }}
            to="/analytics/performance"
          >
            Performance Center
          </Link>
          <select 
            className="modern-select"
            value={currency} 
            onChange={(e) => setCurrency(e.target.value)}
          >
            <option value="VND">VND</option>
            <option value="USD">USD</option>
          </select>
        </div>
      </div>

      {errors.quality ? (
        <section className="analytics-quality-banner quality-error" aria-label="Chất lượng dữ liệu">
          {renderSectionError('quality')}
        </section>
      ) : dataQuality ? (
        <section
          className={`analytics-quality-banner quality-${dataQuality.qualityStatus.toLowerCase()}`}
          aria-label="Chất lượng dữ liệu"
        >
          <div>
            <strong>
              {dataQuality.qualityStatus === 'Complete'
                ? 'Dữ liệu đủ để tham khảo'
                : dataQuality.qualityStatus === 'Unavailable'
                  ? 'Chưa đủ dữ liệu để phân tích'
                  : 'Dữ liệu cần được kiểm tra'}
            </strong>
            <p>
              {dataQuality.missingSnapshotDays} ngày thiếu snapshot · {dataQuality.staleAssetCount} tài sản có giá cũ ·{' '}
              {dataQuality.unclassifiedCashFlowCount} dòng tiền chưa phân loại
            </p>
          </div>
          <span>
            {dataQuality.asOf
              ? `Cập nhật ${new Date(dataQuality.asOf).toLocaleString('vi-VN')}`
              : 'Chưa có snapshot'}
          </span>
        </section>
      ) : null}

      {/* Row 1: Allocation & Cashflow */}
      <div className="dashboard-grid" style={{ gridTemplateColumns: '1fr 1fr', gap: '1.5rem', marginBottom: '1.5rem' }}>
        <div className="chart-card glass-panel" style={{ height: '400px', display: 'flex', flexDirection: 'column' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h2>Phân bổ tài sản</h2>
            <button className="btn-secondary" style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem' }} onClick={() => setIsTargetModalOpen(true)}>
              Cài đặt mục tiêu
            </button>
          </div>
          <div style={{ flex: 1, minHeight: 0, display: 'flex', gap: '1rem', marginTop: '1rem' }}>
            {errors.allocation ? renderSectionError('allocation') : allocationData.length === 0 ? (
              <div className="empty-state">Chưa có tài sản nào.</div>
            ) : (
              <>
                <div style={{ flex: '1', position: 'relative' }}>
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={allocationData}
                        cx="50%"
                        cy="50%"
                        innerRadius={50}
                        outerRadius={80}
                        paddingAngle={5}
                        dataKey="totalValue"
                        nameKey="categoryName"
                      >
                        {allocationData.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={entry.color || '#8884d8'} />
                        ))}
                      </Pie>
                      <RechartsTooltip formatter={renderTooltipFormatter} />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
                <div style={{ flex: '1.2', overflowY: 'auto' }}>
                  <table style={{ width: '100%', fontSize: '0.875rem', textAlign: 'left', borderCollapse: 'collapse' }}>
                    <thead>
                      <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
                        <th style={{ padding: '0.5rem' }}>Danh mục</th>
                        <th style={{ padding: '0.5rem', textAlign: 'right' }}>Thực tế</th>
                        <th style={{ padding: '0.5rem', textAlign: 'right' }}>Mục tiêu</th>
                        <th style={{ padding: '0.5rem', textAlign: 'right' }}>Độ lệch</th>
                      </tr>
                    </thead>
                    <tbody>
                      {allocationData.map(a => (
                        <tr key={a.categoryName} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                          <td style={{ padding: '0.5rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                            <span style={{ display: 'inline-block', width: '10px', height: '10px', borderRadius: '50%', backgroundColor: a.color }}></span>
                            {a.categoryName}
                          </td>
                          <td style={{ padding: '0.5rem', textAlign: 'right' }}>{a.percentage.toFixed(1)}%</td>
                          <td style={{ padding: '0.5rem', textAlign: 'right' }}>{a.targetPercentage.toFixed(1)}%</td>
                          <td style={{ padding: '0.5rem', textAlign: 'right', color: a.deviation > 5 ? '#ef4444' : (a.deviation < -5 ? '#3b82f6' : '#10b981') }}>
                            {a.deviation > 0 ? '+' : ''}{a.deviation.toFixed(1)}%
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            )}
          </div>
        </div>

        <div className="chart-card glass-panel" style={{ height: '400px', display: 'flex', flexDirection: 'column' }}>
          <h2>Thu / Chi theo tháng</h2>
          <div style={{ flex: 1, minHeight: 0 }}>
            {errors.cashflow ? renderSectionError('cashflow') : cashflowData.length === 0 ? (
              <div className="empty-state">Chưa có giao dịch thu chi.</div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={cashflowData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
                  <XAxis dataKey="month" stroke="#cbd5e1" />
                  <YAxis stroke="#cbd5e1" tickFormatter={(v) => v >= 1000000 ? (v/1000000).toFixed(1) + 'M' : v} />
                  <RechartsTooltip formatter={renderTooltipFormatter} contentStyle={{ backgroundColor: '#1e293b', border: 'none', borderRadius: '8px', color: '#fff' }} />
                  <Legend />
                  <Bar dataKey="income" name="Thu Nhập" fill="#10b981" radius={[4, 4, 0, 0]} />
                  <Bar dataKey="expense" name="Chi Tiêu" fill="#ef4444" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>
      </div>

      {/* Heatmap Row */}
      <div className="dashboard-grid" style={{ gridTemplateColumns: '1fr', gap: '1.5rem', marginBottom: '1.5rem' }}>
        <div className="chart-card glass-panel">
          <h2>Mức độ hoạt động giao dịch (365 ngày qua)</h2>
          <div style={{ marginTop: '1rem' }}>
            <CashflowHeatmap />
          </div>
        </div>
      </div>


      {/* Row 2: Value History & Dividends */}
      <div className="dashboard-grid" style={{ gridTemplateColumns: '1fr 1fr', gap: '1.5rem', marginBottom: '1.5rem' }}>
        <div className="chart-card glass-panel" style={{ height: '400px', display: 'flex', flexDirection: 'column' }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h2>Giá trị danh mục đang theo dõi</h2>
            <button 
              className="btn-secondary" 
              style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }} 
              onClick={handleTakeSnapshot}
              disabled={isSnapshotLoading}
            >
              {isSnapshotLoading ? <div className="spinner" style={{ width: '14px', height: '14px', borderWidth: '2px' }}></div> : null}
              {isSnapshotLoading ? 'Đang cập nhật...' : 'Cập nhật giá trị mới nhất'}
            </button>
          </div>
          <div style={{ flex: 1, minHeight: 0, marginTop: '1rem' }}>
            {errors.performance ? renderSectionError('performance') : !performanceData || performanceData.totalValueHistory.length === 0 ? (
              <div className="empty-state">Chưa có dữ liệu lịch sử.</div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={performanceData.totalValueHistory} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
                  <XAxis dataKey="date" stroke="#cbd5e1" />
                  <YAxis stroke="#cbd5e1" tickFormatter={(v) => v >= 1000000 ? (v/1000000).toFixed(1) + 'M' : v} />
                  <RechartsTooltip formatter={renderTooltipFormatter} contentStyle={{ backgroundColor: '#1e293b', border: 'none', borderRadius: '8px', color: '#fff' }} />
                  <Line type="monotone" dataKey="totalValue" name="Tổng giá trị" stroke="#3b82f6" strokeWidth={3} dot={{ r: 4, fill: '#3b82f6' }} activeDot={{ r: 8 }} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        <div className="chart-card glass-panel" style={{ height: '400px', display: 'flex', flexDirection: 'column' }}>
          <h2>Cổ tức & Lãi tiết kiệm</h2>
          <div style={{ flex: 1, minHeight: 0 }}>
            {errors.dividend ? renderSectionError('dividend') : dividendData.length === 0 ? (
              <div className="empty-state">Chưa có dữ liệu cổ tức.</div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={dividendData} margin={{ top: 20, right: 30, left: 20, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.1)" />
                  <XAxis dataKey="month" stroke="#cbd5e1" />
                  <YAxis stroke="#cbd5e1" tickFormatter={(v) => v >= 1000000 ? (v/1000000).toFixed(1) + 'M' : v} />
                  <RechartsTooltip formatter={renderTooltipFormatter} contentStyle={{ backgroundColor: '#1e293b', border: 'none', borderRadius: '8px', color: '#fff' }} />
                  <Bar dataKey="amount" name="Cổ tức" fill="#f59e0b" radius={[4, 4, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>
      </div>

      {/* Row 3: Performance Winners & Losers */}
      <div className="dashboard-grid" style={{ gridTemplateColumns: '1fr 1fr', gap: '1.5rem' }}>
        <div className="chart-card glass-panel" style={{ display: 'flex', flexDirection: 'column' }}>
          <h2>Tài sản có lợi suất cao nhất</h2>
          <p className="analytics-panel-note">Xếp theo tỷ suất lợi nhuận, không phải mức đóng góp vào toàn danh mục.</p>
          <div className="transactions-list" style={{ marginTop: '1rem' }}>
            {errors.performance ? renderSectionError('performance') : (!performanceData || performanceData.topPerformers.length === 0) && <p className="empty-state">Không có dữ liệu.</p>}
            {!errors.performance && performanceData?.topPerformers.map(a => (
              <div key={a.symbol} className="transaction-item" style={{ padding: '0.75rem', marginBottom: '0.5rem', borderRadius: '8px', background: 'rgba(255,255,255,0.05)' }}>
                <div className="transaction-details">
                  <h4>{a.symbol}</h4>
                  <p className="description">{a.name}</p>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div className={`transaction-amount ${a.returnValue >= 0 ? 'positive' : 'negative'}`}>
                    {a.returnValue > 0 ? '+' : ''}{formatCurrency(a.returnValue)}
                  </div>
                  <div style={{ color: a.returnPercentage >= 0 ? '#10b981' : '#ef4444', fontSize: '0.875rem' }}>
                    {a.returnPercentage > 0 ? '+' : ''}{a.returnPercentage.toFixed(2)}%
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="chart-card glass-panel" style={{ display: 'flex', flexDirection: 'column' }}>
          <h2>Tài sản có lợi suất thấp nhất</h2>
          <p className="analytics-panel-note">Xếp theo tỷ suất lợi nhuận, không phải mức đóng góp vào toàn danh mục.</p>
          <div className="transactions-list" style={{ marginTop: '1rem' }}>
            {errors.performance ? renderSectionError('performance') : (!performanceData || performanceData.worstPerformers.length === 0) && <p className="empty-state">Không có dữ liệu.</p>}
            {!errors.performance && performanceData?.worstPerformers.map(a => (
              <div key={a.symbol} className="transaction-item" style={{ padding: '0.75rem', marginBottom: '0.5rem', borderRadius: '8px', background: 'rgba(255,255,255,0.05)' }}>
                <div className="transaction-details">
                  <h4>{a.symbol}</h4>
                  <p className="description">{a.name}</p>
                </div>
                <div style={{ textAlign: 'right' }}>
                  <div className={`transaction-amount ${a.returnValue >= 0 ? 'positive' : 'negative'}`}>
                    {a.returnValue > 0 ? '+' : ''}{formatCurrency(a.returnValue)}
                  </div>
                  <div style={{ color: a.returnPercentage >= 0 ? '#10b981' : '#ef4444', fontSize: '0.875rem' }}>
                    {a.returnPercentage > 0 ? '+' : ''}{a.returnPercentage.toFixed(2)}%
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Row 4: Rebalancing Suggestions */}
      <div className="dashboard-grid" style={{ gridTemplateColumns: '1fr', gap: '1.5rem', marginTop: '1.5rem' }}>
        <div className="chart-card glass-panel" style={{ display: 'flex', flexDirection: 'column' }}>
          <h2>Đánh giá sai lệch phân bổ</h2>
          <p className="analytics-panel-note">
            Chỉ mang tính tham khảo. Hãy cân nhắc dòng tiền mới, phí và thuế trước khi điều chỉnh tài sản.
          </p>
          <div className="transactions-list" style={{ marginTop: '1rem' }}>
            {errors.rebalancing ? renderSectionError('rebalancing') : !rebalanceAssessment?.isActionable ? (
              <div className="analytics-assessment-note">
                <strong>Chưa thể đưa ra phương án tham khảo</strong>
                <p>{rebalanceAssessment?.reason ?? 'Chưa có đủ dữ liệu đánh giá.'}</p>
                {rebalanceAssessment?.targetPlanStatus !== 'Complete' && (
                  <button className="btn-secondary" onClick={() => setIsTargetModalOpen(true)} type="button">
                    Hoàn thiện phân bổ mục tiêu
                  </button>
                )}
              </div>
            ) : rebalanceAssessment.suggestions.length === 0 ? (
              <p className="empty-state">{rebalanceAssessment.reason}</p>
            ) : (
              <table style={{ width: '100%', fontSize: '0.875rem', textAlign: 'left', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
                    <th style={{ padding: '0.75rem' }}>Phương án tham khảo</th>
                    <th style={{ padding: '0.75rem' }}>Danh mục</th>
                    <th style={{ padding: '0.75rem', textAlign: 'right' }}>Giá trị Hiện tại</th>
                    <th style={{ padding: '0.75rem', textAlign: 'right' }}>Giá trị Mục tiêu</th>
                    <th style={{ padding: '0.75rem', textAlign: 'right' }}>Số tiền (Chênh lệch)</th>
                  </tr>
                </thead>
                <tbody>
                  {rebalanceAssessment.suggestions.map((s, index) => (
                    <tr key={index} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                      <td style={{ padding: '0.75rem' }}>
                        <span style={{ 
                          padding: '0.25rem 0.5rem', 
                          borderRadius: '4px',
                          backgroundColor: s.action === 'Increase' ? 'rgba(16, 185, 129, 0.2)' : 'rgba(245, 158, 11, 0.2)',
                          color: s.action === 'Increase' ? '#10b981' : '#f59e0b',
                          fontWeight: 'bold'
                        }}>
                          {s.action === 'Increase' ? 'CÂN NHẮC BỔ SUNG' : 'CÂN NHẮC GIẢM'}
                        </span>
                      </td>
                      <td style={{ padding: '0.75rem', fontWeight: 'bold' }}>{s.categoryName}</td>
                      <td style={{ padding: '0.75rem', textAlign: 'right' }}>{formatCurrency(s.currentValue)}</td>
                      <td style={{ padding: '0.75rem', textAlign: 'right' }}>{formatCurrency(s.targetValue)}</td>
                      <td style={{ padding: '0.75rem', textAlign: 'right', color: s.action === 'Increase' ? '#10b981' : '#f59e0b', fontWeight: 'bold' }}>
                        {formatCurrency(s.differenceValue)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            {!errors.rebalancing && rebalanceAssessment && (
              <p className="analytics-tolerance-note">
                Biên dung sai: {rebalanceAssessment.tolerancePercentagePoints.toLocaleString('vi-VN')} điểm phần trăm.
              </p>
            )}
          </div>
        </div>
      </div>

      <TargetAllocationModal 
        isOpen={isTargetModalOpen} 
        onClose={() => setIsTargetModalOpen(false)} 
        onSaved={reloadAllocations}
      />
    </div>
  );
};
