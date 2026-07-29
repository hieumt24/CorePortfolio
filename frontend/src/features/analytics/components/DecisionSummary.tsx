import type { PerformanceSummary } from '../../performance/types';

interface DecisionSummaryProps {
  performance: PerformanceSummary;
  investmentPortfolioValue: number;
  currency: string;
}

const metricValue = (value: number | null, suffix = '%') =>
  value === null ? 'Chưa đủ dữ liệu' : `${value > 0 ? '+' : ''}${value.toFixed(2)}${suffix}`;

export const DecisionSummary = ({
  performance,
  investmentPortfolioValue,
  currency,
}: DecisionSummaryProps) => {
  const money = new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
    style: 'currency',
    currency,
    maximumFractionDigits: currency === 'VND' ? 0 : 2,
    notation: 'compact',
  });
  const cards = [
    {
      label: 'Giá trị danh mục đầu tư hiện tại',
      value: money.format(investmentPortfolioValue),
      meta: `NAV hiệu suất gồm tiền mặt: ${money.format(performance.endingNetAssetValue)} · Dòng tiền ngoài: ${money.format(performance.netExternalFlow)}`,
      tone: 'neutral',
    },
    {
      label: 'TWR',
      value: metricValue(performance.timeWeightedReturnPercentage.value),
      meta: performance.timeWeightedReturnPercentage.reason ?? 'Đã loại ảnh hưởng nạp/rút tiền',
      tone: (performance.timeWeightedReturnPercentage.value ?? 0) >= 0 ? 'positive' : 'negative',
    },
    {
      label: 'XIRR',
      value: metricValue(performance.moneyWeightedReturnPercentage.value),
      meta: performance.moneyWeightedReturnPercentage.reason ?? 'Lợi suất theo thời điểm dòng tiền',
      tone: (performance.moneyWeightedReturnPercentage.value ?? 0) >= 0 ? 'positive' : 'negative',
    },
    {
      label: 'Drawdown lớn nhất',
      value: metricValue(performance.maximumDrawdownPercentage.value),
      meta: 'Mức giảm từ đỉnh trong kỳ',
      tone: 'warning',
    },
  ];

  return (
    <section className="analytics-kpi-grid" aria-label="Tóm tắt quyết định">
      {cards.map((card) => (
        <article className={`analytics-kpi-card is-${card.tone}`} key={card.label}>
          <span>{card.label}</span>
          <strong>{card.value}</strong>
          <p>{card.meta}</p>
        </article>
      ))}
    </section>
  );
};
