import React, { useState, useEffect } from 'react';
import { analyticsApi } from '../api/analyticsApi';
import type { 
  CashflowMonthlyAnalyticsDto, 
  AssetAllocationDto, 
  PerformanceAnalyticsDto, 
  DividendMonthlyAnalyticsDto,
  RebalanceSuggestionDto
} from '../types';
import { 
  PieChart, Pie, Cell, Tooltip as RechartsTooltip, ResponsiveContainer, Legend,
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
  LineChart, Line
} from 'recharts';
import { TargetAllocationModal } from './TargetAllocationModal';
import { CashflowHeatmap } from './CashflowHeatmap';
import '../../cashflows/components/CashflowDashboard.css'; // Re-use styling

export const AnalyticsDashboard: React.FC = () => {
  const [currency, setCurrency] = useState('VND');
  const [cashflowData, setCashflowData] = useState<CashflowMonthlyAnalyticsDto[]>([]);
  const [allocationData, setAllocationData] = useState<AssetAllocationDto[]>([]);
  const [performanceData, setPerformanceData] = useState<PerformanceAnalyticsDto | null>(null);
  const [dividendData, setDividendData] = useState<DividendMonthlyAnalyticsDto[]>([]);
  const [rebalanceSuggestions, setRebalanceSuggestions] = useState<RebalanceSuggestionDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [isTargetModalOpen, setIsTargetModalOpen] = useState(false);

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      
      const safeFetch = async <T,>(promise: Promise<T>, fallback: T): Promise<T> => {
        try {
          return await promise;
        } catch (error) {
          console.error('API Error in AnalyticsDashboard:', error);
          return fallback;
        }
      };

      try {
        const [cf, alloc, perf, div, suggestions] = await Promise.all([
          safeFetch(analyticsApi.getCashflowAnalytics(6, currency), []),
          safeFetch(analyticsApi.getAssetAllocation(currency), []),
          safeFetch(analyticsApi.getPerformanceAnalytics(currency), null),
          safeFetch(analyticsApi.getDividendAnalytics(12, currency), []),
          safeFetch(analyticsApi.getRebalanceSuggestions(currency), [])
        ]);
        setCashflowData(cf);
        setAllocationData(alloc);
        setPerformanceData(perf);
        setDividendData(div);
        setRebalanceSuggestions(suggestions);
      } catch (error) {
        console.error('Failed to load analytics', error);
      } finally {
        setLoading(false);
      }
    };
    fetchData();
  }, [currency]);

  const reloadAllocations = async () => {
    try {
      const [alloc, suggestions] = await Promise.all([
        analyticsApi.getAssetAllocation(currency),
        analyticsApi.getRebalanceSuggestions(currency)
      ]);
      setAllocationData(alloc);
      setRebalanceSuggestions(suggestions);
    } catch (e) {}
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

  const handleTakeSnapshot = async () => {
    try {
      await analyticsApi.triggerSnapshot();
      alert('Đã cập nhật giá trị danh mục thành công!');
      // Reload performance data
      const perf = await analyticsApi.getPerformanceAnalytics(currency);
      setPerformanceData(perf);
    } catch (error) {
      alert('Có lỗi xảy ra khi cập nhật giá trị danh mục.');
    }
  };

  if (loading) {
    return (
      <div className="cashflow-dashboard">
        <div className="loading-state">
          <div className="spinner"></div>
          <p>Đang phân tích dữ liệu danh mục...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="cashflow-dashboard" style={{ paddingBottom: '3rem' }}>
      <div className="dashboard-header">
        <div className="header-title">
          <h1>📊 Báo cáo & Phân tích</h1>
          <p className="subtitle">Theo dõi hiệu suất và sức khỏe danh mục của bạn</p>
        </div>
        <div className="header-actions">
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
            {allocationData.length === 0 ? (
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
            {cashflowData.length === 0 ? (
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
            <h2>Tổng tài sản theo thời gian</h2>
            <button className="btn-secondary" style={{ padding: '0.25rem 0.75rem', fontSize: '0.875rem' }} onClick={handleTakeSnapshot}>
              Cập nhật giá trị mới nhất
            </button>
          </div>
          <div style={{ flex: 1, minHeight: 0, marginTop: '1rem' }}>
            {!performanceData || performanceData.totalValueHistory.length === 0 ? (
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
            {dividendData.length === 0 ? (
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
          <h2>🔥 Top Lợi Nhuận</h2>
          <div className="transactions-list" style={{ marginTop: '1rem' }}>
            {(!performanceData || performanceData.topPerformers.length === 0) && <p className="empty-state">Không có dữ liệu.</p>}
            {performanceData?.topPerformers.map(a => (
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
          <h2>📉 Top Thua Lỗ</h2>
          <div className="transactions-list" style={{ marginTop: '1rem' }}>
            {(!performanceData || performanceData.worstPerformers.length === 0) && <p className="empty-state">Không có dữ liệu.</p>}
            {performanceData?.worstPerformers.map(a => (
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
          <h2>⚖️ Gợi ý Tái cân bằng (Rebalancing)</h2>
          <div className="transactions-list" style={{ marginTop: '1rem' }}>
            {rebalanceSuggestions.length === 0 ? (
              <p className="empty-state">Tỷ trọng danh mục của bạn đang cân bằng, không có gợi ý nào vào lúc này.</p>
            ) : (
              <table style={{ width: '100%', fontSize: '0.875rem', textAlign: 'left', borderCollapse: 'collapse' }}>
                <thead>
                  <tr style={{ borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
                    <th style={{ padding: '0.75rem' }}>Hành động</th>
                    <th style={{ padding: '0.75rem' }}>Danh mục</th>
                    <th style={{ padding: '0.75rem', textAlign: 'right' }}>Giá trị Hiện tại</th>
                    <th style={{ padding: '0.75rem', textAlign: 'right' }}>Giá trị Mục tiêu</th>
                    <th style={{ padding: '0.75rem', textAlign: 'right' }}>Số tiền (Chênh lệch)</th>
                  </tr>
                </thead>
                <tbody>
                  {rebalanceSuggestions.map((s, index) => (
                    <tr key={index} style={{ borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
                      <td style={{ padding: '0.75rem' }}>
                        <span style={{ 
                          padding: '0.25rem 0.5rem', 
                          borderRadius: '4px',
                          backgroundColor: s.action === 'Buy' ? 'rgba(16, 185, 129, 0.2)' : 'rgba(239, 68, 68, 0.2)',
                          color: s.action === 'Buy' ? '#10b981' : '#ef4444',
                          fontWeight: 'bold'
                        }}>
                          {s.action === 'Buy' ? 'NÊN MUA' : 'NÊN BÁN'}
                        </span>
                      </td>
                      <td style={{ padding: '0.75rem', fontWeight: 'bold' }}>{s.categoryName}</td>
                      <td style={{ padding: '0.75rem', textAlign: 'right' }}>{formatCurrency(s.currentValue)}</td>
                      <td style={{ padding: '0.75rem', textAlign: 'right' }}>{formatCurrency(s.targetValue)}</td>
                      <td style={{ padding: '0.75rem', textAlign: 'right', color: s.action === 'Buy' ? '#10b981' : '#ef4444', fontWeight: 'bold' }}>
                        {formatCurrency(s.differenceValue)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
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
