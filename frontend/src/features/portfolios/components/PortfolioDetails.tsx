import React, { useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { settingsApi } from '../../admin/api/settingsApi';
import { CreateAssetModal } from '../../assets/components/CreateAssetModal';
import { AssetDetailsModal } from '../../assets/components/AssetDetailsModal';
import { GlobalCreateTransactionModal } from '../../transactions/components/GlobalCreateTransactionModal';
import { useNotification } from '../../../context/NotificationContext';
import { usePortfolioSummary } from '../hooks/usePortfolios';
import type { AssetSummaryDto } from '../types';
import { EditPortfolioModal } from './EditPortfolioModal';
import { FundPortfolioModal } from './FundPortfolioModal';
import { HoldingsList } from './HoldingsList';
import { PortfolioCategoryReport } from './PortfolioCategoryReport';
import { PortfolioLoadingState } from './PortfolioLoadingState';
import { PortfolioTransactionHistory } from './PortfolioTransactionHistory';
import './PortfolioDetails.css';

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
  const [usdToVndRate, setUsdToVndRate] = useState(0);

  React.useEffect(() => {
    const loadSettings = async () => {
      const rateValue = await settingsApi.getSetting('USD_TO_VND');
      const rate = Number(rateValue);
      if (Number.isFinite(rate) && rate > 0) setUsdToVndRate(rate);
    };
    loadSettings();
  }, []);

  React.useEffect(() => {
    if (!selectedAsset || !summary) return;
    const updatedAsset = summary.assets.find(asset => asset.assetId === selectedAsset.assetId);
    if (updatedAsset && JSON.stringify(updatedAsset) !== JSON.stringify(selectedAsset)) {
      setSelectedAsset(updatedAsset);
    }
  }, [selectedAsset, summary]);

  const formatCurrency = (value: number | undefined | null, currency: string | null | undefined) => {
    if (value === undefined || value === null) return '0';
    const validCurrency = currency?.trim() || 'VND';
    const isVnd = validCurrency === 'VND';
    try {
      return new Intl.NumberFormat(isVnd ? 'vi-VN' : 'en-US', {
        style: 'currency',
        currency: validCurrency,
        minimumFractionDigits: isVnd ? 0 : 2,
        maximumFractionDigits: isVnd ? 0 : 2,
      }).format(value);
    } catch {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2,
      }).format(value);
    }
  };

  const portfolioInsights = useMemo(() => {
    if (!summary) return { cashValueVnd: 0, largestHolding: null, topHoldings: [] };
    const holdings = summary.assets
      .map(asset => {
        const valueVnd = asset.currency === 'USD'
          ? asset.currentValue * usdToVndRate
          : asset.currentValue;
        return {
          asset,
          valueVnd,
          weight: summary.currentTotalValue > 0 ? (valueVnd / summary.currentTotalValue) * 100 : 0,
        };
      })
      .filter(item => item.valueVnd > 0)
      .sort((a, b) => b.valueVnd - a.valueVnd);
    const cashValueVnd = summary.cashBalances.reduce(
      (total, cash) => total + (cash.currency === 'USD' ? cash.balance * usdToVndRate : cash.balance),
      0,
    );
    return {
      cashValueVnd,
      largestHolding: holdings[0] ?? null,
      topHoldings: holdings.slice(0, 3),
    };
  }, [summary, usdToVndRate]);

  if (loading || (usdToVndRate === 0 && !error)) {
    return (
      <div className="container details-layout">
        <PortfolioLoadingState portfolioId={id} />
      </div>
    );
  }
  if (error) return <div className="state-panel glass-panel error-state">{error}</div>;
  if (!summary) return <div className="state-panel glass-panel error-state">Portfolio not found</div>;

  const totalPnl = summary.realizedPnl + summary.unrealizedPnl;
  const totalPnlPercentage = summary.totalInvested > 0 ? (totalPnl / summary.totalInvested) * 100 : null;
  const openHoldings = summary.assets.filter(asset => Math.abs(asset.totalQuantity) >= 0.00000001).length;

  return (
    <div className="container details-layout">
      <header className="details-header">
        <div className="portfolio-heading">
          <button className="back-link" onClick={() => navigate('/portfolios')}>
            ← All portfolios
          </button>
          <div>
            <p className="portfolio-eyebrow">My portfolio</p>
            <h1>{summary.name}</h1>
            <p className="portfolio-as-of">
              Updated {new Date(summary.asOf).toLocaleString()} · 1 USD = {usdToVndRate.toLocaleString('vi-VN')} VND
            </p>
          </div>
        </div>
        <div className="header-actions">
          <button className="btn btn-outline" onClick={() => setIsFundModalOpen(true)}>Manage funds</button>
          <button className="btn btn-outline" onClick={() => setIsEditModalOpen(true)}>Edit</button>
          <button className="btn btn-outline" onClick={() => setIsAssetModalOpen(true)}>Add asset</button>
          <button className="btn btn-primary" onClick={() => setIsTxModalOpen(true)}>Record transaction</button>
        </div>
      </header>

      <div className="portfolio-tabs" role="tablist" aria-label="Portfolio sections">
        <button
          role="tab"
          aria-selected={activeTab === 'overview'}
          className={`tab-btn ${activeTab === 'overview' ? 'active' : ''}`}
          onClick={() => setActiveTab('overview')}
        >
          Overview
        </button>
        <button
          role="tab"
          aria-selected={activeTab === 'transactions'}
          className={`tab-btn ${activeTab === 'transactions' ? 'active' : ''}`}
          onClick={() => setActiveTab('transactions')}
        >
          Transactions
        </button>
      </div>

      {activeTab === 'overview' ? (
        <div className="overview-workbench">
          <section className="portfolio-summary-strip glass-panel" aria-label="Portfolio summary">
            <div className="summary-primary">
              <span>Holdings value</span>
              <strong>{formatCurrency(summary.currentTotalValue, 'VND')}</strong>
            </div>
            <div>
              <span>Invested</span>
              <strong>{formatCurrency(summary.totalInvested, 'VND')}</strong>
            </div>
            <div>
              <span>Total PnL</span>
              <strong className={totalPnl >= 0 ? 'text-success' : 'text-danger'}>
                {totalPnl > 0 ? '+' : ''}{formatCurrency(totalPnl, 'VND')}
              </strong>
              <small className={totalPnl >= 0 ? 'text-success' : 'text-danger'}>
                {totalPnlPercentage === null ? '—' : `${totalPnlPercentage > 0 ? '+' : ''}${totalPnlPercentage.toFixed(2)}%`}
              </small>
            </div>
            <div>
              <span>Cash balance</span>
              <strong>{formatCurrency(portfolioInsights.cashValueVnd, 'VND')}</strong>
            </div>
            <div>
              <span>Open holdings</span>
              <strong>{openHoldings}</strong>
            </div>
          </section>

          {portfolioInsights.largestHolding && (
            <section className="allocation-insight glass-panel" aria-labelledby="allocation-heading">
              <div className="allocation-copy">
                <span>Largest holding</span>
                <h2 id="allocation-heading">
                  {portfolioInsights.largestHolding.asset.symbol}
                  <strong>{portfolioInsights.largestHolding.weight.toFixed(1)}%</strong>
                </h2>
                <p>{portfolioInsights.largestHolding.asset.name} has the highest weight in this portfolio.</p>
              </div>
              <div className="top-holdings" aria-label="Top three holdings">
                {portfolioInsights.topHoldings.map(item => (
                  <div key={item.asset.assetId}>
                    <span><strong>{item.asset.symbol}</strong><small>{item.weight.toFixed(1)}%</small></span>
                    <span className="allocation-track" aria-hidden="true">
                      <span style={{ transform: `scaleX(${Math.min(item.weight, 100) / 100})` }} />
                    </span>
                  </div>
                ))}
              </div>
            </section>
          )}

          <PortfolioCategoryReport
            assets={summary.assets}
            totalHoldingsVnd={summary.currentTotalValue}
            usdToVndRate={usdToVndRate}
            formatCurrency={formatCurrency}
          />

          {summary.assets.length === 0 ? (
            <div className="state-panel glass-panel">
              <p>No assets in this portfolio yet.</p>
              <button className="btn btn-primary" onClick={() => setIsAssetModalOpen(true)}>Add your first asset</button>
            </div>
          ) : (
            <HoldingsList
              assets={summary.assets}
              portfolioValueVnd={summary.currentTotalValue}
              usdToVndRate={usdToVndRate}
              formatCurrency={formatCurrency}
              onSelectAsset={setSelectedAsset}
            />
          )}

          <section className="cash-panel glass-panel" aria-labelledby="cash-heading">
            <div className="section-heading">
              <div>
                <h2 id="cash-heading">Cash accounts</h2>
                <p>Available cash tracked separately from holdings.</p>
              </div>
              <button className="btn btn-outline" onClick={() => setIsFundModalOpen(true)}>Manage funds</button>
            </div>
            {summary.cashBalances.length === 0 ? (
              <p className="cash-empty">No cash balances found.</p>
            ) : (
              <div className="cash-list">
                {summary.cashBalances.map(cash => {
                  const balanceVnd = cash.currency === 'USD' ? cash.balance * usdToVndRate : cash.balance;
                  const netValue = summary.currentTotalValue + portfolioInsights.cashValueVnd;
                  const weight = netValue > 0 ? (balanceVnd / netValue) * 100 : 0;
                  return (
                    <div className="cash-row" key={cash.cashAccountId}>
                      <span className="cash-currency">{cash.currency}</span>
                      <strong>{formatCurrency(cash.balance, cash.currency)}</strong>
                      <small>{weight.toFixed(1)}% of holdings + cash</small>
                    </div>
                  );
                })}
              </div>
            )}
          </section>
        </div>
      ) : (
        <div className="transactions-tab">
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
            refetch();
          }}
        />
      )}
      {selectedAsset && (
        <AssetDetailsModal
          asset={selectedAsset}
          portfolioId={id!}
          onClose={() => setSelectedAsset(null)}
          onDataChanged={refetch}
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
