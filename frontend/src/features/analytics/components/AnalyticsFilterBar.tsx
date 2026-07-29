import type { PortfolioDto } from '../../portfolios/types';
import {
  analyticsPeriods,
  type AnalyticsPeriod,
  type AnalyticsUrlState,
} from '../utils/analyticsUrlState';

interface AnalyticsFilterBarProps {
  state: AnalyticsUrlState;
  portfolios: PortfolioDto[];
  disabled?: boolean;
  onChange: (patch: Partial<AnalyticsUrlState>) => void;
}

export const AnalyticsFilterBar = ({
  state,
  portfolios,
  disabled = false,
  onChange,
}: AnalyticsFilterBarProps) => (
  <section className="analytics-filter-bar" aria-label="Phạm vi phân tích">
    <div className="analytics-filter-context">
      <span className="analytics-eyebrow">Phạm vi quyết định</span>
      <strong>{state.portfolioId ? 'Một danh mục' : 'Toàn bộ tài sản'}</strong>
    </div>

    <label>
      <span>Danh mục</span>
      <select
        value={state.portfolioId ?? ''}
        disabled={disabled}
        onChange={(event) => onChange({ portfolioId: event.target.value || undefined })}
      >
        <option value="">Tất cả danh mục</option>
        {portfolios.map((portfolio) => (
          <option key={portfolio.id} value={portfolio.id}>{portfolio.name}</option>
        ))}
      </select>
    </label>

    <fieldset>
      <legend>Kỳ phân tích</legend>
      <div className="analytics-period-switcher">
        {analyticsPeriods.map((period) => (
          <button
            key={period}
            type="button"
            disabled={disabled}
            className={state.period === period ? 'is-active' : ''}
            aria-pressed={state.period === period}
            onClick={() => onChange({ period: period as AnalyticsPeriod })}
          >
            {period}
          </button>
        ))}
      </div>
    </fieldset>

    <label>
      <span>Quy đổi</span>
      <select
        value={state.currency}
        disabled={disabled}
        onChange={(event) => onChange({
          currency: event.target.value === 'USD' ? 'USD' : 'VND',
        })}
      >
        <option value="VND">VND</option>
        <option value="USD">USD</option>
      </select>
    </label>
  </section>
);
