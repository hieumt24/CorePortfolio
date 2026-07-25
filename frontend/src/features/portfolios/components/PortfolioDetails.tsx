import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { usePortfolioSummary } from '../hooks/usePortfolios';
import { EditPortfolioModal } from './EditPortfolioModal';
import { CreateAssetModal } from '../../assets/components/CreateAssetModal';
import { AssetDetailsModal } from '../../assets/components/AssetDetailsModal';
import { FundPortfolioModal } from './FundPortfolioModal';
import { GlobalCreateTransactionModal } from '../../transactions/components/GlobalCreateTransactionModal';
import { PortfolioTransactionHistory } from './PortfolioTransactionHistory';
import { DashboardSkeleton } from '../../../shared/components/Skeleton';
import type { AssetSummaryDto } from '../../assets/types';
import { useNotification } from '../../../context/NotificationContext';
import { PieChart, Pie, Cell, Tooltip as RechartsTooltip, ResponsiveContainer } from 'recharts';
import './PortfolioDetails.css';

import { settingsApi } from '../../admin/api/settingsApi';

// Vibrant chart colors
const COLORS = ['#6366f1', '#8b5cf6', '#06b6d4', '#10b981', '#f59e0b', '#ec4899', '#3b82f6', '#84cc16'];

export const PortfolioDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { summary, loading, error, refetch } = usePortfolioSummary(id!);
  const { showNotification } = useNotification();
  
  const [activeTab, setActiveTab] = useState<'overview' | 'transactions'>('overview');
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isAssetModalOpen, setIsAssetModalOpen] = useState(false);
  const [isFundModalOpen, setIsFundModalOpen] = useState(false);
  const [isTxModalOpen, setIsTxModalOpen] = useState(false);
  const [selectedAsset, setSelectedAsset] = useState<AssetSummaryDto | null>(null);
  const [usdToVndRate, setUsdToVndRate] = useState<number>(0);

  React.useEffect(() => {
    const loadSettings = async () => {
      const rateStr = await settingsApi.getSetting('USD_TO_VND');
      if (rateStr) {
        const rate = parseFloat(rateStr);
        if (!isNaN(rate)) {
          setUsdToVndRate(rate);
        }
      }
    };
    loadSettings();
  }, []);

  React.useEffect(() => {
    if (selectedAsset && summary) {
      const updatedAsset = summary.assets.find((a: any) => a.assetId === selectedAsset.assetId);
      if (updatedAsset && JSON.stringify(updatedAsset) !== JSON.stringify(selectedAsset)) {
        setSelectedAsset(updatedAsset);
      }
    }
  }, [summary, selectedAsset]);

  if (loading || (usdToVndRate === 0 && !error)) return <DashboardSkeleton />;
  if (error) return <div className="state-panel glass-panel error-state">{error}</div>;
  if (!summary) return <div className="state-panel glass-panel error-state">Portfolio not found</div>;

  const formatCurrency = (value: number | undefined | null, currency: string | null | undefined) => {
    if (value === undefined || value === null) return '0';
    let validCurrency = currency && currency.trim() !== '' ? currency : 'VND';
    const isVND = validCurrency === 'VND';
    
    try {
      return new Intl.NumberFormat(isVND ? 'vi-VN' : 'en-US', {
        style: 'currency',
        currency: validCurrency,
        minimumFractionDigits: isVND ? 0 : 2,
        maximumFractionDigits: isVND ? 0 : 2,
      }).format(value);
    } catch (e) {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2,
      }).format(value);
    }
  };

  const calculateGroupTotals = (assets: AssetSummaryDto[], currency: string) => {
    let totalInvested = 0;
    let currentValue = 0;
    let totalRealizedPnl = 0;
    let totalUnrealizedPnl = 0;
    let totalBought = 0;

    assets.forEach(a => {
      totalInvested += a.totalCost || 0;
      currentValue += a.currentValue || 0;
      totalRealizedPnl += a.realizedPnl || 0;
      totalUnrealizedPnl += a.unrealizedPnl || 0;
      totalBought += a.totalBought || 0;
    });
    const totalPnl = totalRealizedPnl + totalUnrealizedPnl;
    
    return {
      totalInvested,
      currentValue,
      realizedPnl: totalRealizedPnl,
      unrealizedPnl: totalUnrealizedPnl,
      totalPnl,
      pnlPercentage: totalBought > 0 ? (totalPnl / totalBought) * 100 : null,
      currency
    };
  };

  const groupedAssets = summary.assets.reduce((acc: any, asset: AssetSummaryDto) => {
    const cat = asset.categoryName || 'Uncategorized';
    const currency = asset.currency || 'VND';
    const key = `${cat} - ${currency}`;
    if (!acc[key]) {
      acc[key] = [];
    }
    acc[key].push(asset);
    return acc;
  }, {});

  const renderGroup = (title: string, assets: AssetSummaryDto[]) => {
    if (assets.length === 0) return null;
    const groupCurrency = assets[0]?.currency || 'VND';
    const totals = calculateGroupTotals(assets, groupCurrency);

    return (
      <div className="asset-group" key={title}>
        <div className="group-header">
          <h3 className="group-title">{title}</h3>
          <div className="group-summary glass-panel">
            <div className="summary-item">
              <span className="summary-label">Value</span>
              <span className="summary-val">{formatCurrency(totals.currentValue, totals.currency)}</span>
            </div>
            <div className="summary-item">
              <span className="summary-label">Realized</span>
              <span className={`summary-val ${totals.realizedPnl >= 0 ? 'text-success' : 'text-danger'}`}>
                {totals.realizedPnl > 0 ? '+' : ''}{formatCurrency(totals.realizedPnl, totals.currency)}
              </span>
            </div>
            <div className="summary-item">
              <span className="summary-label">Total PnL</span>
              <span className={`summary-val ${totals.totalPnl >= 0 ? 'text-success' : 'text-danger'}`}>
                {totals.totalPnl > 0 ? '+' : ''}{formatCurrency(totals.totalPnl, totals.currency)}
                {' · '}{totals.pnlPercentage === null ? '—' : `${totals.pnlPercentage > 0 ? '+' : ''}${totals.pnlPercentage.toFixed(2)}%`}
              </span>
            </div>
          </div>
          </div>
          <div className="assets-grid">
            {assets.map((asset: AssetSummaryDto) => {
            const totalPnl = (asset.realizedPnl || 0) + (asset.unrealizedPnl || 0);
            const pnlPercentage = asset.totalBought > 0 ? (totalPnl / asset.totalBought) * 100 : null;
            const isClosedPosition = Math.abs(asset.totalQuantity || 0) < 0.00000001;
            return (
              <button 
                key={asset.assetId} 
                className="asset-card glass-panel"
                onClick={() => setSelectedAsset(asset)}
              >
                <div className="asset-header">
                  <div>
                    <h4 className="asset-symbol">{asset.symbol}</h4>
                    <p className="asset-name">{asset.name}</p>
                  </div>
                </div>
                <div className="asset-metrics">
                  <div className="metric">
                    <span className="metric-label">Qty</span>
                    <span className="metric-val">{asset.totalQuantity?.toLocaleString(undefined, { maximumFractionDigits: 8 }) || '0'}</span>
                  </div>
                  <div className="metric right-align">
                    <span className="metric-label">Value</span>
                    <span className="metric-val">{formatCurrency(asset.currentValue, asset.currency)}</span>
                  </div>
                </div>
                <div className="asset-metrics" style={{ borderTop: 'none', paddingTop: 0 }}>
                  <div className="metric">
                    <span className="metric-label">
                      Total PnL{isClosedPosition ? ' · Closed' : ''}
                    </span>
                    <div style={{ display: 'flex', alignItems: 'center' }}>
                      <span className={`metric-val ${totalPnl >= 0 ? 'text-success' : 'text-danger'}`}>
                        {totalPnl > 0 ? '+' : ''}{formatCurrency(totalPnl, asset.currency)}
                      </span>
                      <span className={`badge ${totalPnl >= 0 ? 'success' : 'danger'}`} style={{ marginLeft: '0.5rem' }}>
                        {pnlPercentage === null
                          ? '—'
                          : `${pnlPercentage > 0 ? '+' : ''}${pnlPercentage.toFixed(2)}%`}
                      </span>
                    </div>
                  </div>
                </div>
              </button>
            );
          })}
        </div>
      </div>
    );
  };

  return (
    <div className="container details-layout">
      {/* Decorative blurred blobs */}
      <div className="mesh-blob blob-1"></div>
      <div className="mesh-blob blob-2" style={{ top: '30%', right: '10%', bottom: 'auto', left: 'auto', background: 'var(--accent-color)' }}></div>

      <header className="details-header">
        <div className="header-left">
          <button className="back-link" onClick={() => navigate('/portfolios')}>
            ← Back to Portfolios
          </button>
          <div>
            <h1 className="gradient-text">{summary.name}</h1>
            <div className="exchange-rate">
              <span className="badge">Exchange Rate: 1 USD = {usdToVndRate.toLocaleString()} VND</span>
            </div>
          </div>
        </div>
        <div className="header-actions">
          <button className="btn btn-outline" onClick={() => setIsFundModalOpen(true)}>Manage Funds</button>
          <button className="btn btn-outline" onClick={() => setIsEditModalOpen(true)}>Edit</button>
          {activeTab === 'overview' ? (
            <button className="btn btn-primary" onClick={() => setIsAssetModalOpen(true)}>Add Asset</button>
          ) : (
            <button className="btn btn-primary" onClick={() => setIsTxModalOpen(true)}>Record Transaction</button>
          )}
        </div>
      </header>

      {/* Tabs System */}
      <div className="portfolio-tabs">
        <button 
          className={`tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
          onClick={() => setActiveTab('overview')}
        >
          Overview
        </button>
        <button 
          className={`tab-btn ${activeTab === 'transactions' ? 'active' : ''}`}
          onClick={() => setActiveTab('transactions')}
        >
          Transactions
        </button>
      </div>

      {activeTab === 'overview' ? (
        <div className="tab-content overview-tab animate-fade-in">
          {/* Premium Glass Stat Cards */}
          <section className="stat-cards-row">
            <div className="glass-stat-card glass-panel">
              <div className="stat-icon" style={{ background: 'linear-gradient(135deg, #6366f1, #3b82f6)' }}>💰</div>
              <div className="stat-content">
                <span className="stat-label">Total Invested (VND)</span>
                <span className="stat-value">{formatCurrency(summary.totalInvested, 'VND')}</span>
              </div>
            </div>
            <div className="glass-stat-card glass-panel">
              <div className="stat-icon" style={{ background: 'linear-gradient(135deg, #8b5cf6, #d946ef)' }}>📈</div>
              <div className="stat-content">
                <span className="stat-label">Current Value (VND)</span>
                <span className="stat-value">{formatCurrency(summary.currentTotalValue, 'VND')}</span>
              </div>
            </div>
            <div className="glass-stat-card glass-panel">
              <div className="stat-icon" style={summary.unrealizedPnl >= 0 ? { background: 'linear-gradient(135deg, #10b981, #059669)' } : { background: 'linear-gradient(135deg, #ef4444, #b91c1c)' }}>
                {summary.unrealizedPnl >= 0 ? '↗' : '↘'}
              </div>
              <div className="stat-content">
                <span className="stat-label">Unrealized PnL</span>
                <span className={`stat-value ${summary.unrealizedPnl >= 0 ? 'text-success' : 'text-danger'}`}>
                  {summary.unrealizedPnl > 0 ? '+' : ''}{formatCurrency(summary.unrealizedPnl, 'VND')}
                </span>
              </div>
            </div>
            <div className="glass-stat-card glass-panel">
              <div className="stat-icon" style={summary.realizedPnl >= 0 ? { background: 'linear-gradient(135deg, #06b6d4, #0284c7)' } : { background: 'linear-gradient(135deg, #f97316, #c2410c)' }}>
                {summary.realizedPnl >= 0 ? '✓' : '−'}
              </div>
              <div className="stat-content">
                <span className="stat-label">Realized PnL</span>
                <span className={`stat-value ${summary.realizedPnl >= 0 ? 'text-success' : 'text-danger'}`}>
                  {summary.realizedPnl > 0 ? '+' : ''}{formatCurrency(summary.realizedPnl, 'VND')}
                </span>
              </div>
            </div>
            <div className="glass-stat-card glass-panel">
              <div className="stat-icon" style={(summary.realizedPnl + summary.unrealizedPnl) >= 0 ? { background: 'linear-gradient(135deg, #14b8a6, #0f766e)' } : { background: 'linear-gradient(135deg, #ef4444, #991b1b)' }}>
                Σ
              </div>
              <div className="stat-content">
                <span className="stat-label">Total PnL</span>
                <span className={`stat-value ${(summary.realizedPnl + summary.unrealizedPnl) >= 0 ? 'text-success' : 'text-danger'}`}>
                  {(summary.realizedPnl + summary.unrealizedPnl) > 0 ? '+' : ''}{formatCurrency(summary.realizedPnl + summary.unrealizedPnl, 'VND')}
                </span>
              </div>
            </div>
          </section>

          <div className="details-content">
            <div className="asset-section">
              <h2>Investments</h2>
              {!summary.assets || summary.assets.length === 0 ? (
                <div className="state-panel glass-panel">
                  <p style={{ color: 'var(--text-secondary)' }}>No assets in this portfolio yet.</p>
                  <button className="btn btn-primary" onClick={() => setIsAssetModalOpen(true)}>
                    Add your first asset
                  </button>
                </div>
              ) : (
                <div className="asset-groups-container">
                  {Object.keys(groupedAssets).map(catName => renderGroup(catName, groupedAssets[catName]))}
                </div>
              )}

              <h2 style={{ marginTop: '2rem' }}>Cash Balances</h2>
              {!summary.cashBalances || summary.cashBalances.length === 0 ? (
                <div className="state-panel glass-panel" style={{ minHeight: '100px' }}>
                  <p style={{ color: 'var(--text-secondary)' }}>No cash balances found.</p>
                </div>
              ) : (
                <div className="cash-grid">
                  {summary.cashBalances.map((cash: any) => (
                    <div key={cash.cashAccountId} className="cash-card glass-panel">
                      <div className="cash-icon">{cash.currency === 'USD' ? '$' : '₫'}</div>
                      <div className="cash-info">
                        <h3 className="cash-currency">{cash.currency}</h3>
                        <span className="cash-val">{formatCurrency(cash.balance, cash.currency)}</span>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <aside>
              <div className="chart-panel glass-panel">
                <h3>Allocation</h3>
                <div style={{ width: '100%', height: 300, marginTop: '1rem' }}>
                  <ResponsiveContainer>
                    <PieChart>
                      <Pie
                        data={[
                          ...Object.keys(groupedAssets).map(catName => {
                            const assets = groupedAssets[catName];
                            const groupCurrency = assets[0]?.currency || 'VND';
                            const totals = calculateGroupTotals(assets, groupCurrency);
                            let valueVND = totals.currentValue;
                            if (groupCurrency === 'USD') valueVND *= usdToVndRate;
                            return { name: catName, value: valueVND };
                          }),
                          ...(summary.cashBalances || []).map((cash: any) => {
                            let valueVND = cash.balance;
                            if (cash.currency === 'USD') valueVND *= usdToVndRate;
                            return { name: `Cash (${cash.currency})`, value: valueVND };
                          })
                        ].filter(d => d.value > 0)}
                        cx="50%"
                        cy="50%"
                        innerRadius={70}
                        outerRadius={100}
                        paddingAngle={5}
                        dataKey="value"
                        stroke="rgba(255,255,255,0.1)"
                        strokeWidth={2}
                      >
                        {[...Object.keys(groupedAssets), ...(summary.cashBalances || [])].map((_, index) => (
                          <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} style={{ filter: 'drop-shadow(0px 4px 8px rgba(0,0,0,0.4))' }} />
                        ))}
                      </Pie>
                      <RechartsTooltip 
                        formatter={(value: any) => formatCurrency(Number(value) || 0, 'VND')}
                        contentStyle={{ 
                          backgroundColor: 'rgba(15, 23, 42, 0.85)', 
                          backdropFilter: 'blur(10px)',
                          border: '1px solid rgba(255,255,255,0.1)', 
                          borderRadius: '12px',
                          boxShadow: '0 8px 32px rgba(0,0,0,0.5)'
                        }}
                        itemStyle={{ color: '#fff' }}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                </div>
              </div>
            </aside>
          </div>
        </div>
      ) : (
        <div className="tab-content transactions-tab animate-fade-in">
          <PortfolioTransactionHistory portfolioId={id!} />
        </div>
      )}

      {isEditModalOpen && (
        <EditPortfolioModal 
          portfolio={{ portfolioId: id!, name: summary.name }}
          onClose={() => setIsEditModalOpen(false)}
          onSuccess={() => {
            setIsEditModalOpen(false);
            showNotification('Portfolio updated!', 'success');
            refetch();
          }}
        />
      )}
      {isAssetModalOpen && (
        <CreateAssetModal 
          portfolioId={id!} 
          existingAssetIds={summary.assets.map((a: any) => a.marketAssetId)}
          onClose={() => setIsAssetModalOpen(false)} 
          onSuccess={() => {
            setIsAssetModalOpen(false);
            showNotification('Asset added!', 'success');
            refetch();
          }} 
        />
      )}
      {isTxModalOpen && (
        <GlobalCreateTransactionModal
          initialPortfolioId={id!}
          onClose={() => setIsTxModalOpen(false)}
          onSuccess={() => {
            setIsTxModalOpen(false);
            showNotification('Transaction recorded!', 'success');
            // If they are on the overview tab, refetch summary. If on transactions, it automatically refetches via its own effect or we could trigger a global refresh. We'll just refetch summary so balances are correct.
            refetch();
          }}
        />
      )}
      {selectedAsset && (
        <AssetDetailsModal
          asset={selectedAsset}
          portfolioId={id!}
          onClose={() => setSelectedAsset(null)}
          onDataChanged={() => refetch()}
        />
      )}
      {isFundModalOpen && (
        <FundPortfolioModal
          portfolioId={id!}
          onClose={() => setIsFundModalOpen(false)}
          onSuccess={() => {
            setIsFundModalOpen(false);
            showNotification('Funds updated!', 'success');
            refetch();
          }}
        />
      )}
    </div>
  );
};
