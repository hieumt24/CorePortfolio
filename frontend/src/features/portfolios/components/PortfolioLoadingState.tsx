import React from 'react';
import './PortfolioLoadingState.css';

const PORTFOLIO_TIPS = [
  'Đang tổng hợp giá trị và tỷ trọng tài sản của bạn.',
  'Danh mục sẽ được sắp xếp theo tỷ trọng lớn nhất.',
  'Lợi nhuận đã chốt và chưa chốt được theo dõi riêng.',
];

const getTipForPortfolio = (portfolioId?: string) => {
  if (!portfolioId) return PORTFOLIO_TIPS[0];
  const characterTotal = [...portfolioId].reduce((total, character) => total + character.charCodeAt(0), 0);
  return PORTFOLIO_TIPS[characterTotal % PORTFOLIO_TIPS.length];
};

interface PortfolioLoadingStateProps {
  portfolioId?: string;
}

export const PortfolioLoadingState: React.FC<PortfolioLoadingStateProps> = ({ portfolioId }) => (
  <section
    className="portfolio-loading"
    role="status"
    aria-live="polite"
    aria-label="Đang tải dữ liệu portfolio"
  >
    <div className="portfolio-loader-scene" aria-hidden="true">
      <span className="portfolio-loader-orbit orbit-one">₫</span>
      <span className="portfolio-loader-orbit orbit-two">$</span>
      <span className="portfolio-loader-orbit orbit-three">%</span>

      <div className="portfolio-loader-mascot">
        <div className="portfolio-loader-coin">
          <span className="portfolio-loader-shine" />
          <span className="portfolio-loader-mark">C</span>
        </div>
        <div className="portfolio-loader-chart">
          <span />
          <span />
          <span />
        </div>
      </div>

      <div className="portfolio-loader-shadow" />
    </div>

    <div className="portfolio-loading-copy">
      <strong>
        Loading portfolio
        <span className="portfolio-loading-dots" aria-hidden="true">
          <i />
          <i />
          <i />
        </span>
      </strong>
      <p>{getTipForPortfolio(portfolioId)}</p>
    </div>
  </section>
);
