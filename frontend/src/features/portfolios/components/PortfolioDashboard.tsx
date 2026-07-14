import React, { useState } from 'react';
import { usePortfolios } from '../hooks/usePortfolios';
import { PortfolioCard } from './PortfolioCard';
import { CreatePortfolioModal } from './CreatePortfolioModal';
import { useNotification } from '../../../context/NotificationContext';
import './PortfolioDashboard.css';

export const PortfolioDashboard: React.FC = () => {
  const { portfolios, loading, error, refetch } = usePortfolios();
  const { showNotification } = useNotification();
  const [isModalOpen, setIsModalOpen] = useState(false);

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
