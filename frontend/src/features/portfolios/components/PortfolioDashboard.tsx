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
    <div className="container">
      <header className="dashboard-header">
        <h1>My Portfolios</h1>
        <button 
          className="btn btn-primary glass-panel"
          onClick={() => setIsModalOpen(true)}
        >
          <span className="plus-icon">+</span> New Portfolio
        </button>
      </header>

      {loading && (
        <div className="loading-state glass-panel">
          <div className="spinner"></div>
          <p>Loading your portfolios...</p>
        </div>
      )}

      {error && (
        <div className="error-state glass-panel">
          <h3>Oops! Something went wrong</h3>
          <p>{error.message}</p>
        </div>
      )}

      {!loading && !error && portfolios.length === 0 && (
        <div className="empty-state glass-panel">
          <p>You don't have any portfolios yet. Create one to get started!</p>
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
