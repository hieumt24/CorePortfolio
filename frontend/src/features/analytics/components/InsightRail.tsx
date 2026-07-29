import { Link } from 'react-router-dom';
import type { AnalyticsAttentionDto } from '../types';

interface InsightRailProps {
  attention: AnalyticsAttentionDto[];
}

export const InsightRail = ({ attention }: InsightRailProps) => (
  <aside className="analytics-insight-rail" aria-labelledby="analytics-attention-title">
    <div className="analytics-panel-heading">
      <div>
        <span className="analytics-eyebrow">Ưu tiên rà soát</span>
        <h2 id="analytics-attention-title">Ba tín hiệu trước khi quyết định</h2>
      </div>
      <span className="analytics-rule-badge">Quy tắc xác định</span>
    </div>
    <ol>
      {attention.map((item) => (
        <li key={item.code} className={`is-${item.severity.toLowerCase()}`}>
          <span className="analytics-insight-index" aria-hidden="true" />
          <div>
            <strong>{item.title}</strong>
            <p>{item.detail}</p>
            {item.deepLink && <Link to={item.deepLink}>Mở dữ liệu liên quan →</Link>}
          </div>
        </li>
      ))}
    </ol>
    <p className="analytics-disclaimer">
      Các tín hiệu chỉ dựa trên dữ liệu đã ghi nhận và ngưỡng cố định, không phải khuyến nghị đầu tư.
    </p>
  </aside>
);
