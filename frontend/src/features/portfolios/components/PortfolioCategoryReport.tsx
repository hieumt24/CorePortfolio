import React from 'react';
import type { AssetSummaryDto } from '../types';
import { calculateCategoryAllocations } from '../utils/categoryAllocation';

interface PortfolioCategoryReportProps {
  assets: AssetSummaryDto[];
  totalHoldingsVnd: number;
  usdToVndRate: number;
  formatCurrency: (value: number, currency: string) => string;
}

export const PortfolioCategoryReport: React.FC<PortfolioCategoryReportProps> = ({
  assets,
  totalHoldingsVnd,
  usdToVndRate,
  formatCurrency,
}) => {
  const allocations = calculateCategoryAllocations(assets, totalHoldingsVnd, usdToVndRate);

  return (
    <section className="category-report glass-panel" aria-labelledby="category-report-heading">
      <div className="category-report-heading">
        <div>
          <h2 id="category-report-heading">Asset allocation</h2>
          <p>Percentage of holdings value by investment group.</p>
        </div>
        <strong>{formatCurrency(totalHoldingsVnd, 'VND')}</strong>
      </div>

      <div className="category-report-list">
        {allocations.map(item => (
          <article className={`category-report-row category-${item.key}`} key={item.key}>
            <div className="category-report-label">
              <span className="category-report-dot" aria-hidden="true" />
              <div>
                <strong>{item.label}</strong>
                <small>{item.assetCount} {item.assetCount === 1 ? 'asset' : 'assets'}</small>
              </div>
            </div>
            <div className="category-report-value">
              <strong>{item.percentage.toFixed(1)}%</strong>
              <small>{formatCurrency(item.valueVnd, 'VND')}</small>
            </div>
            <div
              className="category-report-track"
              role="progressbar"
              aria-label={`${item.label} allocation`}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={Number(item.percentage.toFixed(1))}
            >
              <span style={{ transform: `scaleX(${Math.min(item.percentage, 100) / 100})` }} />
            </div>
          </article>
        ))}
      </div>
    </section>
  );
};
