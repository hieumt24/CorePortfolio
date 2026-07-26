import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { adminApi } from '../api/adminApi';
import type {
  AdminOverview as AdminOverviewModel,
  AuditEvent,
  ProductionOperations,
} from '../types';
import './AdminOperations.css';

const statCards: Array<{ key: keyof AdminOverviewModel; label: string; hint: string; tone: string }> = [
  { key: 'totalUsers', label: 'Người dùng', hint: 'Tổng tài khoản', tone: 'violet' },
  { key: 'activeUsers', label: 'Đang hoạt động', hint: 'Có thể đăng nhập', tone: 'emerald' },
  { key: 'totalPortfolios', label: 'Danh mục đầu tư', hint: 'Trên toàn hệ thống', tone: 'blue' },
  { key: 'totalTransactions', label: 'Giao dịch', hint: 'Tổng lịch sử ghi nhận', tone: 'amber' },
];

export function AdminOverview() {
  const [overview, setOverview] = useState<AdminOverviewModel | null>(null);
  const [operations, setOperations] = useState<ProductionOperations | null>(null);
  const [auditEvents, setAuditEvents] = useState<AuditEvent[]>([]);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const loadOverview = async () => {
    setLoading(true);
    setError('');
    try {
      const [overviewResult, operationsResult, auditResult] = await Promise.all([
        adminApi.getOverview(),
        adminApi.getOperations(),
        adminApi.getAuditEvents(),
      ]);
      setOverview(overviewResult);
      setOperations(operationsResult);
      setAuditEvents(auditResult.items);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : 'Không thể tải dữ liệu quản trị.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { void loadOverview(); }, []);

  if (loading) return <div className="admin-loading glass-panel"><span className="admin-loader" />Đang tổng hợp dữ liệu hệ thống...</div>;
  if (error || !overview) return <div className="admin-error glass-panel"><p>{error}</p><button onClick={loadOverview}>Thử lại</button></div>;

  return (
    <div className="admin-page-container admin-operations">
      <div className="operations-hero">
        <div>
          <span className="admin-kicker">System command center</span>
          <h1>Tổng quan quản trị</h1>
          <p>Theo dõi sức khỏe dữ liệu, người dùng và hoạt động đầu tư trên một màn hình.</p>
        </div>
        <button className="operations-refresh" onClick={loadOverview}>Làm mới</button>
      </div>

      <section className="operations-stats" aria-label="Chỉ số hệ thống">
        {statCards.map(card => (
          <article className={`operations-stat ${card.tone}`} key={card.key}>
            <span>{card.label}</span>
            <strong>{Number(overview[card.key]).toLocaleString('vi-VN')}</strong>
            <small>{card.hint}</small>
          </article>
        ))}
      </section>

      <section className="operations-grid">
        <article className="operations-panel">
          <div className="operations-panel-heading"><div><span>Access control</span><h2>Người dùng & phân quyền</h2></div><Link to="../users">Quản lý</Link></div>
          <div className="operations-metric-row"><span>Quản trị viên đang hoạt động</span><strong>{overview.adminUsers}</strong></div>
          <div className="operations-metric-row"><span>Tài khoản bị khóa</span><strong>{overview.totalUsers - overview.activeUsers}</strong></div>
        </article>

        <article className={`operations-panel ${overview.marketAssetsNeedingAttention > 0 ? 'attention' : ''}`}>
          <div className="operations-panel-heading"><div><span>Market data</span><h2>Chất lượng dữ liệu giá</h2></div><Link to="../market-assets">Kiểm tra</Link></div>
          <div className="operations-metric-row"><span>Tổng market assets</span><strong>{overview.totalMarketAssets}</strong></div>
          <div className="operations-metric-row"><span>Cần xử lý</span><strong>{overview.marketAssetsNeedingAttention}</strong></div>
        </article>

        <article className="operations-panel operations-wide">
          <div className="operations-panel-heading"><div><span>Platform activity</span><h2>Dữ liệu đang được quản lý</h2></div></div>
          <div className="operations-activity">
            <div><strong>{overview.totalAssets.toLocaleString('vi-VN')}</strong><span>Tài sản trong danh mục</span></div>
            <div><strong>{overview.totalCashflows.toLocaleString('vi-VN')}</strong><span>Dòng tiền đã ghi nhận</span></div>
            <div><strong>{overview.totalPortfolios.toLocaleString('vi-VN')}</strong><span>Danh mục đầu tư</span></div>
          </div>
        </article>

        <article className="operations-panel">
          <div className="operations-panel-heading">
            <div><span>Background jobs</span><h2>Vận hành nền</h2></div>
            <span className={`operations-health ${operations?.isMaintenanceMode ? 'failed' : 'healthy'}`}>
              {operations?.isMaintenanceMode ? 'Maintenance' : 'Ready'}
            </span>
          </div>
          <div className="job-list">
            {(operations?.jobs ?? []).map(job => (
              <div className="job-row" key={job.name}>
                <div>
                  <strong>{job.name}</strong>
                  <small>
                    {job.lastSucceededAt
                      ? `Thành công ${new Date(job.lastSucceededAt).toLocaleString('vi-VN')}`
                      : 'Chưa có lần chạy thành công'}
                  </small>
                </div>
                <span className={`job-state ${job.state.toLowerCase()}`}>{job.state}</span>
              </div>
            ))}
            {!operations?.jobs.length && <p className="operations-placeholder">Job chưa chạy từ khi API khởi động.</p>}
          </div>
        </article>

        <article className="operations-panel">
          <div className="operations-panel-heading">
            <div><span>Audit trail</span><h2>Thao tác nhạy cảm gần đây</h2></div>
          </div>
          <div className="audit-list">
            {auditEvents.map(event => (
              <div className="audit-row" key={event.id}>
                <span className="audit-mark" />
                <div>
                  <strong>{event.action}</strong>
                  <small>
                    {event.entityType}{event.entityId ? ` · ${event.entityId}` : ''}
                  </small>
                </div>
                <time dateTime={event.occurredAt}>
                  {new Date(event.occurredAt).toLocaleString('vi-VN')}
                </time>
              </div>
            ))}
            {!auditEvents.length && <p className="operations-placeholder">Chưa có audit event.</p>}
          </div>
        </article>
      </section>
      <p className="operations-updated">Cập nhật lúc {new Date(overview.generatedAt).toLocaleString('vi-VN')}</p>
    </div>
  );
}
