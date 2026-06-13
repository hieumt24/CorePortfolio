import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { usePortfolioSummary } from '../hooks/usePortfolios';
import { EditPortfolioModal } from './EditPortfolioModal';
import { CreateAssetModal } from '../../assets/components/CreateAssetModal';
import { AssetDetailsModal } from '../../assets/components/AssetDetailsModal';
import { AssetType } from '../../assets/types';
import type { AssetSummaryDto } from '../types';
import './PortfolioDetails.css';

export const PortfolioDetails: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { summary, loading, error, refetch } = usePortfolioSummary(id!);
  
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [isAssetModalOpen, setIsAssetModalOpen] = useState(false);
  const [selectedAsset, setSelectedAsset] = useState<AssetSummaryDto | null>(null);

  if (loading) return <div className="loading">Loading portfolio...</div>;
  if (error) return <div className="error">{error}</div>;
  if (!summary) return <div className="error">Portfolio not found</div>;

  const formatCurrency = (value: number | undefined | null, currency: string = 'USD') => {
    if (value === undefined || value === null) return '0.00';
    const validCurrency = currency && currency.trim() !== '' ? currency : 'USD';
    try {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: validCurrency,
        minimumFractionDigits: 2,
      }).format(value);
    } catch (e) {
      return new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'USD',
        minimumFractionDigits: 2,
      }).format(value);
    }
  };

  const cryptoAssets = summary.assets.filter((a: any) => a.type === AssetType.Crypto);
  const stockAssets = summary.assets.filter((a: any) => a.type === AssetType.Stock);
  const fundAssets = summary.assets.filter((a: any) => a.type === AssetType.MutualFund);

  return (
    <div className="container">
      <header className="dashboard-header">
        <div className="header-left">
          <button className="btn btn-secondary glass-panel" onClick={() => navigate('/portfolios')}>
            ← Back
          </button>
          <div className="title-group">
            <h1>{summary.name}</h1>
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
          <span className="stat-label">Total Invested</span>
          <span className="stat-value">{formatCurrency(summary.totalInvested)}</span>
        </div>
        <div className="stat-card glass-panel">
          <span className="stat-label">Current Value</span>
          <span className="stat-value">{formatCurrency(summary.currentTotalValue)}</span>
        </div>
        <div className="stat-card glass-panel">
          <span className="stat-label">Total Profit/Loss</span>
          <span className={`stat-value ${(summary.currentTotalValue || 0) >= (summary.totalInvested || 0) ? 'positive' : 'negative'}`}>
            {formatCurrency((summary.currentTotalValue || 0) - (summary.totalInvested || 0))}
          </span>
        </div>
      </section>

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
            {cryptoAssets.length > 0 && (
              <div className="asset-group">
                <h3 className="group-title">Crypto</h3>
                <div className="assets-grid">
                  {cryptoAssets.map((asset: AssetSummaryDto) => (
                    <div 
                      key={asset.assetId} 
                      className="asset-card glass-panel crypto"
                      onClick={() => setSelectedAsset(asset)}
                    >
                      <div className="asset-header">
                        <h3>{asset.symbol}</h3>
                        <span className="asset-type">Crypto</span>
                      </div>
                      <p className="asset-name">{asset.name}</p>
                      <div className="asset-stats">
                        <div className="stat">
                          <span>Quantity</span>
                          <strong>{asset.totalQuantity?.toLocaleString() || '0'}</strong>
                        </div>
                        <div className="stat">
                          <span>Value</span>
                          <strong>{formatCurrency(asset.currentValue, asset.currency)}</strong>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
            
            {stockAssets.length > 0 && (
              <div className="asset-group">
                <h3 className="group-title">Stocks</h3>
                <div className="assets-grid">
                  {stockAssets.map((asset: AssetSummaryDto) => (
                    <div 
                      key={asset.assetId} 
                      className="asset-card glass-panel stock"
                      onClick={() => setSelectedAsset(asset)}
                    >
                      <div className="asset-header">
                        <h3>{asset.symbol}</h3>
                        <span className="asset-type">Stock</span>
                      </div>
                      <p className="asset-name">{asset.name}</p>
                      <div className="asset-stats">
                        <div className="stat">
                          <span>Quantity</span>
                          <strong>{asset.totalQuantity?.toLocaleString() || '0'}</strong>
                        </div>
                        <div className="stat">
                          <span>Value</span>
                          <strong>${asset.currentValue?.toLocaleString(undefined, { minimumFractionDigits: 2 }) || '0.00'}</strong>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {fundAssets.length > 0 && (
              <div className="asset-group">
                <h3 className="group-title">Mutual Funds</h3>
                <div className="assets-grid">
                  {fundAssets.map((asset: AssetSummaryDto) => (
                    <div 
                      key={asset.assetId} 
                      className="asset-card glass-panel fund"
                      onClick={() => setSelectedAsset(asset)}
                    >
                      <div className="asset-header">
                        <h3>{asset.symbol}</h3>
                        <span className="asset-type">Fund</span>
                      </div>
                      <p className="asset-name">{asset.name}</p>
                      <div className="asset-stats">
                        <div className="stat">
                          <span>Quantity</span>
                          <strong>{asset.totalQuantity?.toLocaleString() || '0'}</strong>
                        </div>
                        <div className="stat">
                          <span>Value</span>
                          <strong>${asset.currentValue?.toLocaleString(undefined, { minimumFractionDigits: 2 }) || '0.00'}</strong>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            )}
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
