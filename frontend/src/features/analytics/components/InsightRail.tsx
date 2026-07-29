import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { formatVietnamDateTime } from '../../../shared/utils/dateTime';
import type {
  AnalyticsInsightDto,
  AnalyticsInsightEvidenceDto,
  AnalyticsInsightsDto,
} from '../types';

interface InsightRailProps {
  insights: AnalyticsInsightsDto;
}

const categoryLabels: Record<string, string> = {
  All: 'Tất cả',
  DataQuality: 'Dữ liệu',
  Risk: 'Rủi ro',
  Allocation: 'Phân bổ',
  Cashflow: 'Dòng tiền',
  Goals: 'Mục tiêu',
  Performance: 'Hiệu suất',
  General: 'Tổng quan',
};

const severityLabels: Record<string, string> = {
  Critical: 'Ưu tiên cao',
  Warning: 'Cần rà soát',
  Info: 'Nên biết',
  Positive: 'Ổn định',
};

const confidenceLabels: Record<string, string> = {
  High: 'Tin cậy cao',
  Medium: 'Tin cậy vừa',
  Low: 'Tin cậy thấp',
};

const formatEvidenceValue = (
  evidence: AnalyticsInsightEvidenceDto,
  currency: string,
) => {
  if (evidence.unit === 'money') {
    return new Intl.NumberFormat(currency === 'VND' ? 'vi-VN' : 'en-US', {
      style: 'currency',
      currency,
      maximumFractionDigits: currency === 'VND' ? 0 : 2,
      notation: 'compact',
    }).format(evidence.value);
  }
  if (evidence.unit === 'percentagePoints') {
    return `${evidence.value > 0 ? '+' : ''}${evidence.value.toFixed(2)}đ%`;
  }
  const suffix: Record<string, string> = {
    days: ' ngày',
    assets: ' tài sản',
    records: ' bản ghi',
    budgets: ' ngân sách',
    months: ' tháng',
    goals: ' mục tiêu',
    plans: ' kế hoạch',
  };
  return `${evidence.value.toLocaleString('vi-VN')}${suffix[evidence.unit] ?? ''}`;
};

const InsightCard = ({
  item,
  currency,
}: {
  item: AnalyticsInsightDto;
  currency: string;
}) => (
  <li className={`analytics-insight-card is-${item.severity.toLowerCase()}`}>
    <div className="analytics-insight-card-head">
      <span className="analytics-insight-index" aria-hidden="true" />
      <div>
        <div className="analytics-insight-badges">
          <span className={`is-${item.severity.toLowerCase()}`}>
            {severityLabels[item.severity] ?? item.severity}
          </span>
          <span>{confidenceLabels[item.confidence] ?? item.confidence}</span>
        </div>
        <strong>{item.title}</strong>
      </div>
    </div>
    <p className="analytics-insight-observation">{item.observation}</p>
    <details>
      <summary>Vì sao tín hiệu xuất hiện?</summary>
      <div className="analytics-insight-explanation">
        <section>
          <h3>Diễn giải</h3>
          <p>{item.interpretation}</p>
        </section>
        <section>
          <h3>Vì sao đáng chú ý</h3>
          <p>{item.whyItMatters}</p>
        </section>
        {item.evidence.length > 0 && (
          <dl className="analytics-evidence-grid">
            {item.evidence.map((evidence) => (
              <div key={evidence.key}>
                <dt>{evidence.label}</dt>
                <dd>{formatEvidenceValue(evidence, currency)}</dd>
                <small>{evidence.source}</small>
              </div>
            ))}
          </dl>
        )}
        {item.limitations.length > 0 && (
          <div className="analytics-limitations">
            <h3>Giới hạn cần biết</h3>
            <ul>
              {item.limitations.map((limitation) => (
                <li key={limitation}>{limitation}</li>
              ))}
            </ul>
          </div>
        )}
      </div>
    </details>
    {item.action && <Link to={item.action.href}>{item.action.label} →</Link>}
  </li>
);

export const InsightRail = ({ insights }: InsightRailProps) => {
  const [category, setCategory] = useState('All');
  const categories = useMemo(
    () => ['All', ...Array.from(new Set(insights.items.map((item) => item.category)))],
    [insights.items],
  );
  const visibleItems = category === 'All'
    ? insights.items
    : insights.items.filter((item) => item.category === category);

  return (
    <aside className="analytics-insight-rail" aria-labelledby="analytics-attention-title">
      <div className="analytics-panel-heading">
        <div>
          <span className="analytics-eyebrow">Insight có giải thích</span>
          <h2 id="analytics-attention-title">Ưu tiên trước khi quyết định</h2>
        </div>
        <span className="analytics-rule-badge">{insights.methodologyVersion}</span>
      </div>

      <div className="analytics-insight-summary" aria-label="Tóm tắt mức độ ưu tiên">
        <span><strong>{insights.summary.criticalCount}</strong> cao</span>
        <span><strong>{insights.summary.warningCount}</strong> rà soát</span>
        <span><strong>{insights.summary.infoCount}</strong> thông tin</span>
      </div>

      <div className="analytics-insight-filters" aria-label="Lọc insight">
        {categories.map((value) => (
          <button
            key={value}
            type="button"
            className={category === value ? 'is-active' : ''}
            aria-pressed={category === value}
            onClick={() => setCategory(value)}
          >
            {categoryLabels[value] ?? value}
          </button>
        ))}
      </div>

      <ol className="analytics-insight-list">
        {visibleItems.map((item) => (
          <InsightCard key={item.code} item={item} currency={insights.scope.currency} />
        ))}
      </ol>

      <div className="analytics-methodology-note">
        <strong>Phương pháp minh bạch</strong>
        <p>{insights.methodologyDescription}</p>
        <small>Cập nhật {formatVietnamDateTime(insights.generatedAt)}</small>
      </div>
      <p className="analytics-disclaimer">{insights.disclaimer}</p>
    </aside>
  );
};
