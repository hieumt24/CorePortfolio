import React, { useEffect, useState } from 'react';
import { usePortfolios } from '../hooks/usePortfolios';
import { PortfolioCard } from './PortfolioCard';
import { CreatePortfolioModal } from './CreatePortfolioModal';
import { useNotification } from '../../../context/NotificationContext';
import { getMarketIndices } from '../api/portfolioApi';
import type { MarketIndexQuote } from '../types';
import { formatVietnamDateTime } from '../../../shared/utils/dateTime';
import './PortfolioDashboard.css';

export const PortfolioDashboard: React.FC = () => {
  const { portfolios, loading, error, refetch } = usePortfolios();
  const { showNotification } = useNotification();
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [indices, setIndices] = useState<MarketIndexQuote[]>([]);
  const [indicesLoading, setIndicesLoading] = useState(true);
  const [indicesError, setIndicesError] = useState(false);

  const loadIndices = () => {
    setIndicesLoading(true);
    setIndicesError(false);
    getMarketIndices()
      .then(setIndices)
      .catch(() => setIndicesError(true))
      .finally(() => setIndicesLoading(false));
  };

  useEffect(() => {
    let active = true;
    getMarketIndices()
      .then(result => {
        if (active) setIndices(result);
      })
      .catch(() => {
        if (active) setIndicesError(true);
      })
      .finally(() => {
        if (active) setIndicesLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  return (
    <div className="container dashboard-layout">
      {/* Decorative blurred blobs for premium aesthetic */}
      <div className="mesh-blob blob-1"></div>
      <div className="mesh-blob blob-2"></div>

      <header className="dashboard-header">
        <div className="header-titles">
          <h1 className="gradient-text">Portfolios</h1>
          <p className="subtitle">Manage and track your financial assets</p>
        </div>
        <div className="header-actions">
          <button 
            className="btn btn-primary"
            onClick={() => setIsModalOpen(true)}
          >
            New Portfolio
          </button>
        </div>
      </header>

      <section className="market-indices" aria-label="Chỉ số thị trường Việt Nam">
        <div className="market-indices-heading">
          <div>
            <span className="section-eyebrow">THỊ TRƯỜNG VIỆT NAM</span>
            <h2>Nhịp thị trường</h2>
          </div>
          {indicesError && (
            <button type="button" className="index-retry" onClick={loadIndices}>
              Thử lại
            </button>
          )}
        </div>
        <div className="market-index-grid">
          {indicesLoading && [0, 1].map(item => (
            <div key={item} className="market-index-card glass-panel index-skeleton" />
          ))}
          {!indicesLoading && indices.map(index => {
            const direction = index.change > 0 ? 'positive' : index.change < 0 ? 'negative' : 'neutral';
            return (
              <article key={index.symbol} className={`market-index-card glass-panel ${direction}`}>
                <div className="index-card-top">
                  <div>
                    <span className="index-symbol">{index.symbol}</span>
                    <h3>{index.name}</h3>
                  </div>
                  <span className={`index-status ${index.status.toLowerCase()}`}>
                    {index.status === 'Fresh' ? 'KBS' : index.status}
                  </span>
                </div>
                {index.status === 'Error' ? (
                  <p className="index-error">{index.error ?? 'Chưa có dữ liệu.'}</p>
                ) : (
                  <>
                    <strong className="index-value">{index.value.toLocaleString('vi-VN', {
                      minimumFractionDigits: 2,
                      maximumFractionDigits: 2,
                    })}</strong>
                    <div className="index-change">
                      <span>{index.change > 0 ? '+' : ''}{index.change.toLocaleString('vi-VN', {
                        minimumFractionDigits: 2,
                        maximumFractionDigits: 2,
                      })}</span>
                      <span>{index.changePercent > 0 ? '+' : ''}{index.changePercent.toFixed(2)}%</span>
                    </div>
                    <small>Cập nhật {formatVietnamDateTime(index.asOf)}</small>
                  </>
                )}
              </article>
            );
          })}
          {!indicesLoading && indicesError && (
            <div className="market-index-card glass-panel index-error-state">
              Không thể tải chỉ số thị trường.
            </div>
          )}
        </div>
      </section>

      {loading && (
        <div className="state-panel glass-panel">
          <div className="spinner"></div>
          <p>Loading portfolios...</p>
        </div>
      )}

      {error && (
        <div className="state-panel glass-panel error-state">
          <h3>Oops! Something went wrong</h3>
          <p>{error.message}</p>
        </div>
      )}

      {!loading && !error && portfolios.length === 0 && (
        <div className="state-panel glass-panel empty-state">
          <p>You don't have any portfolios yet. Create one to get started.</p>
          <button 
            className="btn btn-outline"
            onClick={() => setIsModalOpen(true)}
            style={{ marginTop: '1rem' }}
          >
            Create Portfolio
          </button>
        </div>
      )}

      {!loading && !error && portfolios.length > 0 && (
        <div className="portfolios-grid">
          {portfolios.map(portfolio => (
            <PortfolioCard key={portfolio.id} portfolio={portfolio} />
          ))}
        </div>
      )}

      {isModalOpen && (
        <CreatePortfolioModal 
          onClose={() => setIsModalOpen(false)} 
          onSuccess={() => {
            setIsModalOpen(false);
            showNotification('Tạo Portfolio thành công!', 'success');
            refetch();
          }} 
        />
      )}
    </div>
  );
};
