import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { usePortfolioSummary } from '../hooks/usePortfolios';
import { EditPortfolioModal } from './EditPortfolioModal';
import { CreateAssetModal } from '../../assets/components/CreateAssetModal';
import { AssetDetailsModal } from '../../assets/components/AssetDetailsModal';
import type { AssetSummaryDto } from '../../assets/types';
import { useNotification } from '../../../context/NotificationContext';
import { PieChart, Pie, Cell, Tooltip as RechartsTooltip, ResponsiveContainer } from 'recharts';
import './PortfolioDetails.css';

import { settingsApi } from '../../admin/api/settingsApi';

const COLORS = ['#8b5cf6', '#3b82f6', '#10b981', '#f59e0b', '#ef4444', '#ec4899', '#6366f1', '#14b8a6'];

export const PortfolioDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { summary, loading, error, refetch } = usePortfolioSummary(id!);
  const { showNotification } = useNotification();
  
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isAssetModalOpen, setIsAssetModalOpen] = useState(false);
  const [selectedAsset, setSelectedAsset] = useState<AssetSummaryDto | null>(null);
  const [usdToVndRate, setUsdToVndRate] = useState<number>(26309);

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
  }, [summary]);

  if (loading) return <div className="loading">Loading portfolio...</div>;
  if (error) return <div className="error">{error}</div>;
  if (!summary) return <div className="error">Portfolio not found</div>;

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
    let totalBought = 0;
    let currentValue = 0;
    assets.forEach(a => {
      totalInvested += a.totalCost || 0;
      totalBought += a.totalBought || 0;
      currentValue += a.currentValue || 0;
    });
    
    const profit = currentValue - totalInvested;
    const profitPercentage = totalBought > 0 ? (profit / totalBought) * 100 : 0;
    
    return {
      totalInvested,
      totalBought,
      currentValue,
      profit,
      profitPercentage,
      currency
    };
  };

  const calculateAssetPnL = (asset: AssetSummaryDto) => {
    const cost = asset.totalCost || 0;
    const value = asset.currentValue || 0;
    const bought = asset.totalBought || 0;
    const profit = value - cost;
    const profitPercentage = bought > 0 ? (profit / bought) * 100 : 0;
    return { profit, profitPercentage };
  };

  // Group assets by category AND currency
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

  // Calculate overall VND
  let overallInvestedVND = 0;
  let overallCurrentVND = 0;

  Object.keys(groupedAssets).forEach(catName => {
    const assets: AssetSummaryDto[] = groupedAssets[catName];
    const groupCurrency = assets[0]?.currency || 'VND';
    const totals = calculateGroupTotals(assets, groupCurrency);
    
    if (groupCurrency === 'USD') {
      overallInvestedVND += totals.totalInvested * usdToVndRate;
      overallCurrentVND += totals.currentValue * usdToVndRate;
    } else {
      overallInvestedVND += totals.totalInvested;
      overallCurrentVND += totals.currentValue;
    }
  });

  const overallProfitVND = overallCurrentVND - overallInvestedVND;

  const renderGroup = (title: string, assets: AssetSummaryDto[]) => {
    if (assets.length === 0) return null;
    const groupCurrency = assets[0]?.currency || 'VND';
    const totals = calculateGroupTotals(assets, groupCurrency);

    return (
      <div className="asset-group" key={title}>
        <div className="group-header">
          <h3 className="group-title">{title}</h3>
          <div className="group-stats">
            <div className="group-stat">
              <span className="group-stat-label">Total Invested</span>
              <span className="group-stat-value">{formatCurrency(totals.totalInvested, totals.currency)}</span>
            </div>
            <div className="group-stat">
              <span className="group-stat-label">Current Value</span>
              <span className="group-stat-value">{formatCurrency(totals.currentValue, totals.currency)}</span>
            </div>
            <div className="group-stat">
              <span className="group-stat-label">Profit/Loss</span>
              <span className={`group-stat-value ${totals.profit >= 0 ? 'positive' : 'negative'}`}>
                {totals.profit > 0 ? '+' : ''}{formatCurrency(totals.profit, totals.currency)}
                <span style={{fontSize: '0.9rem', marginLeft: '6px', fontWeight: 'normal'}}>
                  ({totals.profit > 0 ? '+' : ''}{totals.profitPercentage.toFixed(2)}%)
                </span>
              </span>
            </div>
          </div>
        </div>
        <div className="assets-grid">
          {assets.map((asset: AssetSummaryDto) => {
            const pnl = calculateAssetPnL(asset);
            return (
              <div 
                key={asset.assetId} 
                className={`asset-card glass-panel`}
                onClick={() => setSelectedAsset(asset)}
              >
                <div className="asset-header">
                  <h3>{asset.symbol}</h3>
                  <span className="asset-type">{title}</span>
                </div>
                <p className="asset-name">{asset.name}</p>
                <div className="asset-stats">
                  <div className="stat">
                    <span>Quantity</span>
                    <strong>{asset.totalQuantity?.toLocaleString(undefined, { maximumFractionDigits: 8 }) || '0'}</strong>
                  </div>
                  <div className="stat" style={{alignItems: 'flex-end'}}>
                    <span>Value</span>
                    <strong>{formatCurrency(asset.currentValue, asset.currency)}</strong>
                  </div>
                </div>
                <div className="asset-pnl">
                  <span className="asset-pnl-label">Profit / Loss</span>
                  <div className="asset-pnl-values">
                    <span className={`pnl-amount ${pnl.profit >= 0 ? 'positive' : 'negative'}`}>
                      {pnl.profit > 0 ? '+' : ''}{formatCurrency(pnl.profit, asset.currency)}
                    </span>
                    <span className={`pnl-percent ${pnl.profit >= 0 ? 'positive' : 'negative'}`}>
                      {pnl.profit > 0 ? '+' : ''}{pnl.profitPercentage.toFixed(2)}%
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    );
  };

  return (
    <div className="container">
      <header className="dashboard-header">
        <div className="header-left">
          <button className="btn btn-secondary glass-panel" onClick={() => navigate('/portfolios')}>
            ← Back
          </button>
          <div className="title-group">
            <h1>{summary.name}</h1>
            <div className="exchange-rate-note">
              Exchange Rate: 1 USD = {usdToVndRate.toLocaleString()} VND
            </div>
          </div>
        </div>
        <div className="header-right">
          <button 
            className="btn btn-outline glass-panel"
            onClick={() => setIsEditModalOpen(true)}
          >
            Edit Portfolio
          </button>
          <button 
            className="btn btn-primary glass-panel"
            onClick={() => setIsAssetModalOpen(true)}
          >
            <span className="plus-icon">+</span> Add Asset
          </button>
        </div>
      </header>

      <section className="overview-cards">
        <div className="stat-card glass-panel">
          <span className="stat-label">Total Invested (VND)</span>
          <span className="stat-value">{formatCurrency(overallInvestedVND, 'VND')}</span>
        </div>
        <div className="stat-card glass-panel">
          <span className="stat-label">Current Value (VND)</span>
          <span className="stat-value">{formatCurrency(overallCurrentVND, 'VND')}</span>
        </div>
        <div className="stat-card glass-panel">
          <span className="stat-label">Total Profit/Loss</span>
          <span className={`stat-value ${overallProfitVND >= 0 ? 'positive' : 'negative'}`}>
            {overallProfitVND > 0 ? '+' : ''}{formatCurrency(overallProfitVND, 'VND')}
          </span>
        </div>
      </section>

      {summary.assets && summary.assets.length > 0 && (
        <section className="portfolio-report-section">
          <div className="glass-panel" style={{ padding: '1.5rem', marginBottom: '2rem', display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <h3 style={{ margin: '0 0 1rem 0', color: 'rgba(255,255,255,0.9)' }}>Asset Allocation</h3>
            <div style={{ width: '100%', height: 250 }}>
              <ResponsiveContainer>
                <PieChart>
                  <Pie
                    data={Object.keys(groupedAssets).map(catName => {
                      const assets = groupedAssets[catName];
                      const groupCurrency = assets[0]?.currency || 'VND';
                      const totals = calculateGroupTotals(assets, groupCurrency);
                      let valueVND = totals.currentValue;
                      if (groupCurrency === 'USD') valueVND *= usdToVndRate;
                      return { name: catName, value: valueVND };
                    }).filter(d => d.value > 0)}
                    cx="50%"
                    cy="50%"
                    innerRadius={50}
                    outerRadius={80}
                    paddingAngle={5}
                    dataKey="value"
                    label={({name, percent}) => `${name} ${((percent || 0) * 100).toFixed(1)}%`}
                    stroke="none"
                  >
                    {Object.keys(groupedAssets).map((_, index) => (
                      <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                    ))}
                  </Pie>
                  <RechartsTooltip 
                    formatter={(value: any) => formatCurrency(Number(value) || 0, 'VND')}
                    contentStyle={{ backgroundColor: 'rgba(30, 41, 59, 0.8)', border: '1px solid rgba(255,255,255,0.1)', borderRadius: '8px', backdropFilter: 'blur(8px)' }}
                    itemStyle={{ color: '#fff' }}
                  />
                </PieChart>
              </ResponsiveContainer>
            </div>
          </div>
        </section>
      )}

      <section className="assets-section">
        <h2>Assets</h2>
        {!summary.assets || summary.assets.length === 0 ? (
          <div className="empty-state glass-panel">
            <p>No assets in this portfolio yet.</p>
            <button className="btn btn-primary" onClick={() => setIsAssetModalOpen(true)}>
              Add your first asset
            </button>
          </div>
        ) : (
          <div className="asset-groups">
            {Object.keys(groupedAssets).map(catName => renderGroup(catName, groupedAssets[catName]))}
          </div>
        )}
      </section>

      {isEditModalOpen && (
        <EditPortfolioModal 
          portfolio={{
            portfolioId: id!,
            name: summary.name
          }}
          onClose={() => setIsEditModalOpen(false)}
          onSuccess={() => {
            setIsEditModalOpen(false);
            showNotification('Cập nhật Portfolio thành công!', 'success');
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
            showNotification('Thêm Asset thành công!', 'success');
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
    </div>
  );
};
