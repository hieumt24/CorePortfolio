import React from 'react';
import { useNavigate } from 'react-router-dom';
import type { PortfolioDto } from '../types';
import { formatVietnamDate } from '../../../shared/utils/dateTime';
import './PortfolioCard.css';

interface Props {
  portfolio: PortfolioDto;
}

export const PortfolioCard: React.FC<Props> = ({ portfolio }) => {
  const navigate = useNavigate();

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' || e.key === ' ') {
      e.preventDefault();
      navigate(`/portfolios/${portfolio.id}`);
    }
  };

  return (
    <article 
      className="portfolio-card glass-panel" 
      onClick={() => navigate(`/portfolios/${portfolio.id}`)}
      onKeyDown={handleKeyDown}
      role="button"
      tabIndex={0}
      aria-label={`View details for portfolio ${portfolio.name}`}
    >
      <div className="card-header">
        <h3 className="card-title">{portfolio.name}</h3>
      </div>
      <div className="card-body">
        <p className="card-desc">
          {portfolio.description || 'No description provided.'}
        </p>
      </div>
      <div className="card-footer">
        <span className="card-meta">
          Created {formatVietnamDate(portfolio.createdAt)}
        </span>
      </div>
    </article>
  );
};
