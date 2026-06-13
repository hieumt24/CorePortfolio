import React from 'react';
import { useNavigate } from 'react-router-dom';
import type { PortfolioDto } from '../types';
import './PortfolioCard.css';

interface Props {
  portfolio: PortfolioDto;
}

export const PortfolioCard: React.FC<Props> = ({ portfolio }) => {
  const navigate = useNavigate();

  return (
    <div 
      className="portfolio-card glass-panel" 
      onClick={() => navigate(`/portfolios/${portfolio.id}`)}
      style={{ cursor: 'pointer' }}
    >
      <div className="portfolio-card-header">
        <h3 className="portfolio-name">{portfolio.name}</h3>
      </div>
      <div className="portfolio-card-body">
        <p className="portfolio-description">
          {portfolio.description || 'No description provided.'}
        </p>
      </div>
      <div className="portfolio-card-footer">
        <span className="portfolio-date">
          Created: {new Date(portfolio.createdAt).toLocaleDateString()}
        </span>
      </div>
    </div>
  );
};
