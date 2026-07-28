import { useCallback, useEffect, useState } from 'react';
import type { FormEvent } from 'react';
import { Link, useParams } from 'react-router-dom';
import { adminApi } from '../api/adminApi';
import type {
  AdminCapabilities,
  AdminSystemConfiguration,
  AuditEvent,
  BackgroundJobStatus,
  DatabaseBackup,
  IntegrityReport,
  MarketDataHealth,
  NotificationCampaign,
  ProductionOperations,
  SecurityEvent,
  UserSession,
  AdminUserDetail,
} from '../types';
import { formatVietnamDateTime } from '../../../shared/utils/dateTime';
import './ControlPlanePages.css';

function PageState({ loading, error, retry }: { loading: boolean; error: string; retry: () => void }) {
  if (loading) return <div className="control-state glass-panel"><span className="admin-loader" />Đang tải dữ liệu...</div>;
  if (error) return <div className="control-state control-error glass-panel"><p>{error}</p><button onClick={retry}>Thử lại</button></div>;
  return null;
}

export function AuditLogPage() {
  const [items, setItems] = useState<AuditEvent[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [outcome, setOutcome] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const result = await adminApi.getAuditPage({ search, outcome, page, pageSize: 30 });
      setItems(result.items); setTotal(result.total);
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'Không tải được audit log.'); }
    finally { setLoading(false); }
  }, [search, outcome, page]);
  useEffect(() => { void load(); }, [load]);
  const exportCsv = () => {
    const rows = [['Time', 'Actor', 'Action', 'Entity', 'Outcome', 'IP', 'Correlation'],
      ...items.map(x => [x.occurredAt, x.actorUsername ?? x.actorUserId ?? '', x.action,
        `${x.entityType}:${x.entityId ?? ''}`, x.outcome, x.ipAddress ?? '', x.correlationId ?? ''])];
    const blob = new Blob([rows.map(r => r.map(v => `"${String(v).replaceAll('"', '""')}"`).join(',')).join('\n')],
      { type: 'text/csv;charset=utf-8' });
    const url = URL.createObjectURL(blob); const anchor = document.createElement('a');
    anchor.href = url; anchor.download = 'coreportfolio-audit.csv'; anchor.click(); URL.revokeObjectURL(url);
  };
  return <section className="control-page">
    <header className="control-hero"><div><span>Governance</span><h1>Audit Log</h1><p>Tra cứu mọi thay đổi nhạy cảm theo actor, IP và correlation ID.</p></div><button onClick={exportCsv}>Xuất CSV trang này</button></header>
    <div className="control-filters glass-panel">
      <input value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} placeholder="Action, entity, correlation..." />
      <select value={outcome} onChange={e => { setOutcome(e.target.value); setPage(1); }}><option value="">Mọi kết quả</option><option>Succeeded</option><option>Failed</option></select>
      <button onClick={load}>Làm mới</button>
    </div>
    <PageState loading={loading} error={error} retry={load} />
    {!loading && !error && <div className="control-table-wrap glass-panel"><table className="control-table"><thead><tr><th>Thời gian</th><th>Actor</th><th>Hành động</th><th>Đối tượng</th><th>Kết quả</th><th>IP</th></tr></thead><tbody>
      {items.map(item => <tr key={item.id}><td>{formatVietnamDateTime(item.occurredAt)}</td><td>{item.actorUsername ?? 'System'}</td><td><strong>{item.action}</strong><small>{item.correlationId}</small></td><td>{item.entityType}<small>{item.entityId}</small></td><td><span className={`status-pill ${item.outcome.toLowerCase()}`}>{item.outcome}</span></td><td>{item.ipAddress ?? '—'}</td></tr>)}
    </tbody></table>{items.length === 0 && <div className="control-empty">Không có sự kiện phù hợp.</div>}</div>}
    <div className="control-pagination"><button disabled={page === 1} onClick={() => setPage(x => x - 1)}>Trước</button><span>Trang {page} · {total} sự kiện</span><button disabled={page * 30 >= total} onClick={() => setPage(x => x + 1)}>Sau</button></div>
  </section>;
}

export function OperationsPage() {
  const [data, setData] = useState<ProductionOperations | null>(null);
  const [busy, setBusy] = useState('');
  const [error, setError] = useState('');
  const load = useCallback(async () => { try { setData(await adminApi.getOperations()); setError(''); } catch (e) { setError(e instanceof Error ? e.message : 'Không tải được trạng thái job.'); } }, []);
  useEffect(() => { void load(); const timer = window.setInterval(load, 30000); return () => clearInterval(timer); }, [load]);
  const run = async (name: string) => { setBusy(name); try { await adminApi.runJob(name); await load(); } catch (e) { setError(e instanceof Error ? e.message : 'Không chạy được job.'); } finally { setBusy(''); } };
  const jobCards = (data?.jobs ?? []) as BackgroundJobStatus[];
  return <section className="control-page"><header className="control-hero"><div><span>Runtime</span><h1>Operations Center</h1><p>Theo dõi và kích hoạt các job có whitelist.</p></div><button onClick={load}>Làm mới</button></header>
    {error && <div className="control-alert">{error}</div>}
    <div className="control-grid two">
      {['daily-snapshot', 'market-price-refresh'].map(name => <article className="control-card glass-panel" key={name}><div className="control-card-head"><h3>{name === 'daily-snapshot' ? 'Daily Snapshot' : 'Market Price Refresh'}</h3><span className="status-pill ready">Manual</span></div><p>Chạy ngay và ghi lại audit event, duration và kết quả.</p><button disabled={busy !== ''} onClick={() => run(name)}>{busy === name ? 'Đang chạy...' : 'Chạy ngay'}</button></article>)}
    </div>
    <div className="control-grid three">{jobCards.map(job => <article className="control-card glass-panel" key={job.name}><div className="control-card-head"><h3>{job.name}</h3><span className={`status-pill ${job.state.toLowerCase()}`}>{job.state}</span></div><dl><div><dt>Thành công</dt><dd>{job.successCount}</dd></div><div><dt>Thất bại</dt><dd>{job.failureCount}</dd></div><div><dt>Duration</dt><dd>{job.lastDurationMilliseconds ?? 0} ms</dd></div></dl>{job.lastError && <p className="control-danger">{job.lastError}</p>}</article>)}</div>
  </section>;
}

export function UserDetailPage() {
  const { id = '' } = useParams();
  const [user, setUser] = useState<AdminUserDetail | null>(null);
  const [sessions, setSessions] = useState<UserSession[]>([]);
  const [timeline, setTimeline] = useState<SecurityEvent[]>([]);
  const [capabilities, setCapabilities] = useState<AdminCapabilities | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [resetConfirmation, setResetConfirmation] = useState('');
  const [resetReason, setResetReason] = useState('');
  const [resetBusy, setResetBusy] = useState(false);
  const [resetStatus, setResetStatus] = useState('');
  const load = useCallback(async () => {
    setLoading(true); try {
      const [detail, sessionRows, events, caps] = await Promise.all([
        adminApi.getUserDetail(id), adminApi.getUserSessions(id), adminApi.getSecurityTimeline(id), adminApi.getCapabilities()]);
      setUser(detail); setSessions(sessionRows); setTimeline(events); setCapabilities(caps); setError('');
    } catch (e) { setError(e instanceof Error ? e.message : 'Không tải được user.'); } finally { setLoading(false); }
  }, [id]);
  useEffect(() => { void load(); }, [load]);
  const revoke = async (sessionId?: string) => { if (!window.confirm('Thu hồi phiên đăng nhập đã chọn?')) return; await adminApi.revokeSessions(id, sessionId); await load(); };
  const updateRole = async (role: string) => { await adminApi.updateRole(id, role); await load(); };
  const resetTwoFactor = async (event: FormEvent) => {
    event.preventDefault();
    if (!user) return;
    setResetBusy(true);
    setResetStatus('');
    try {
      await adminApi.resetUserTwoFactor(id, resetConfirmation, resetReason);
      setResetConfirmation('');
      setResetReason('');
      setResetStatus('Đã reset 2FA và thu hồi toàn bộ phiên của tài khoản.');
      await load();
    } catch (reason) {
      setResetStatus(reason instanceof Error ? reason.message : 'Không thể reset 2FA.');
    } finally {
      setResetBusy(false);
    }
  };
  if (loading || error || !user) return <PageState loading={loading} error={error || 'Không tìm thấy user.'} retry={load} />;
  return <section className="control-page"><header className="control-hero"><div><span>Identity</span><h1>{user.displayName || user.username}</h1><p>{user.email || 'Chưa có email'} · {user.lastLoginIpAddress || 'Chưa có IP'}</p></div><Link className="control-link" to="/admin/users">Quay lại</Link></header>
    <div className="control-grid four">{[['Portfolio', user.portfolioCount], ['Giao dịch', user.transactionCount], ['Dòng tiền', user.cashflowCount], ['Phiên hoạt động', user.activeSessionCount]].map(([label, value]) => <article className="metric-card glass-panel" key={label}><span>{label}</span><strong>{value}</strong></article>)}</div>
    <div className="control-grid two"><article className="control-card glass-panel"><h2>Quyền truy cập</h2><select value={user.role} onChange={e => void updateRole(e.target.value)}>{capabilities?.roles.map(role => <option key={role}>{role}</option>)}</select><p>Đổi role sẽ thu hồi tất cả token hiện tại.</p></article><article className="control-card glass-panel"><div className="control-card-head"><h2>Sessions</h2><button onClick={() => revoke()}>Thu hồi tất cả</button></div>{sessions.map(s => <div className="session-row" key={s.id}><div><strong>{s.ipAddress || 'Unknown IP'}</strong><small>{s.userAgent || 'Unknown device'} · {formatVietnamDateTime(s.lastSeenAt)}</small></div><span className={`status-pill ${s.isActive ? 'ready' : 'failed'}`}>{s.isActive ? 'Active' : 'Revoked'}</span>{s.isActive && <button onClick={() => revoke(s.id)}>Thu hồi</button>}</div>)}</article></div>
    <article className="control-card glass-panel"><h2>Security timeline</h2><div className="timeline">{timeline.map(event => <div key={event.id}><i /><div><strong>{event.action}</strong><p>{event.outcome} · {event.ipAddress || 'System'} · {formatVietnamDateTime(event.occurredAt)}</p></div></div>)}</div></article>
    <article className="control-card glass-panel">
      <div className="control-card-head"><h2>Xác minh hai bước</h2><span className={`status-pill ${user.twoFactorEnabled ? 'ready' : 'failed'}`}>{user.twoFactorEnabled ? 'Enrolled' : user.twoFactorRequired ? 'Required' : 'Disabled'}</span></div>
      <dl><div><dt>Recovery codes</dt><dd>{user.recoveryCodesRemaining}</dd></div><div><dt>Kích hoạt</dt><dd>{formatVietnamDateTime(user.twoFactorEnabledAt, '—')}</dd></div></dl>
      <p>Reset khẩn cấp chỉ dành cho SuperAdmin, yêu cầu xác nhận username và luôn thu hồi session.</p>
    </article>
    {capabilities?.permissions.includes('TwoFactor.Reset') && (
      <form className="control-card control-form glass-panel mfa-reset-panel" onSubmit={resetTwoFactor}>
        <div className="control-card-head"><div><h2>SuperAdmin recovery reset</h2><p>Dùng khi người dùng mất cả authenticator và recovery codes.</p></div><span className="status-pill failed">Break-glass</span></div>
        <div className="form-row">
          <label>Nhập chính xác username<input value={resetConfirmation} onChange={event => setResetConfirmation(event.target.value)} placeholder={user.username} required /></label>
          <label>Lý do audit<input value={resetReason} onChange={event => setResetReason(event.target.value)} minLength={10} maxLength={200} required /></label>
        </div>
        {resetStatus && <div className="control-alert" role="status">{resetStatus}</div>}
        <div className="button-row"><button className="danger-button" disabled={resetBusy || resetConfirmation !== user.username || resetReason.trim().length < 10}>{resetBusy ? 'Đang reset…' : 'Reset 2FA và thu hồi sessions'}</button></div>
      </form>
    )}
  </section>;
}

export function MarketDataControlPage() {
  const [data, setData] = useState<MarketDataHealth | null>(null); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const load = useCallback(async () => { try { setData(await adminApi.getMarketDataHealth()); setError(''); } catch (e) { setError(e instanceof Error ? e.message : 'Không tải được market health.'); } }, []);
  useEffect(() => { void load(); }, [load]);
  const refresh = async () => { setBusy(true); try { await adminApi.runJob('market-price-refresh'); await load(); } finally { setBusy(false); } };
  return <section className="control-page"><header className="control-hero"><div><span>Market Data</span><h1>Provider Control Center</h1><p>Quan sát freshness và xử lý hàng đợi giá lỗi.</p></div><button disabled={busy} onClick={refresh}>{busy ? 'Đang refresh...' : 'Refresh toàn bộ'}</button></header>{error && <div className="control-alert">{error}</div>}
    <div className="control-grid three">{data?.providers.map(p => <article className="control-card glass-panel" key={p.provider}><div className="control-card-head"><h3>{p.provider}</h3><span>{p.total} assets</span></div><dl><div><dt>Fresh</dt><dd>{p.fresh}</dd></div><div><dt>Stale</dt><dd>{p.stale}</dd></div><div><dt>Error</dt><dd>{p.errors}</dd></div></dl><small>{formatVietnamDateTime(p.lastUpdated)}</small></article>)}</div>
    <div className="control-table-wrap glass-panel"><table className="control-table"><thead><tr><th>Asset</th><th>Provider</th><th>Trạng thái</th><th>Lỗi gần nhất</th><th>Cập nhật</th></tr></thead><tbody>{data?.attention.map(x => <tr key={x.id}><td><strong>{x.symbol}</strong><small>{x.name}</small></td><td>{x.priceSource}</td><td><span className={`status-pill ${x.priceStatus.toLowerCase()}`}>{x.priceStatus}</span></td><td>{x.lastPriceError || '—'}</td><td>{formatVietnamDateTime(x.lastUpdated)}</td></tr>)}</tbody></table>{data?.attention.length === 0 && <div className="control-empty">Tất cả Market Asset đang khỏe.</div>}</div>
  </section>;
}

export function NotificationManagementPage() {
  const [campaigns, setCampaigns] = useState<NotificationCampaign[]>([]); const [error, setError] = useState(''); const [busy, setBusy] = useState(false);
  const [form, setForm] = useState({ title: '', message: '', severity: '0', role: '', link: '' });
  const load = useCallback(async () => { try { setCampaigns(await adminApi.getNotificationCampaigns()); } catch (e) { setError(e instanceof Error ? e.message : 'Không tải được chiến dịch.'); } }, []);
  useEffect(() => { void load(); }, [load]);
  const submit = async (event: FormEvent) => { event.preventDefault(); setBusy(true); try { const result = await adminApi.broadcastNotification({ ...form, severity: Number(form.severity), role: form.role || undefined, link: form.link || undefined }); setForm({ title: '', message: '', severity: '0', role: '', link: '' }); setError(`Đã gửi tới ${result.recipients} người dùng.`); await load(); } catch (e) { setError(e instanceof Error ? e.message : 'Không gửi được thông báo.'); } finally { setBusy(false); } };
  return <section className="control-page"><header className="control-hero"><div><span>Communication</span><h1>Notification Management</h1><p>Broadcast có phân nhóm, theo dõi recipient và read rate.</p></div></header>{error && <div className="control-alert">{error}</div>}
    <div className="control-grid two"><form className="control-card control-form glass-panel" onSubmit={submit}><h2>Tạo thông báo</h2><input required placeholder="Tiêu đề" value={form.title} onChange={e => setForm({ ...form, title: e.target.value })}/><textarea required placeholder="Nội dung" value={form.message} onChange={e => setForm({ ...form, message: e.target.value })}/><div className="form-row"><select value={form.severity} onChange={e => setForm({ ...form, severity: e.target.value })}><option value="0">Info</option><option value="1">Warning</option><option value="2">Critical</option></select><select value={form.role} onChange={e => setForm({ ...form, role: e.target.value })}><option value="">Tất cả user</option><option>User</option><option>Admin</option><option>Operations</option></select></div><input placeholder="Deep link (không bắt buộc)" value={form.link} onChange={e => setForm({ ...form, link: e.target.value })}/><button disabled={busy}>{busy ? 'Đang gửi...' : 'Gửi broadcast'}</button></form>
      <article className="control-card glass-panel"><h2>Chiến dịch gần đây</h2>{campaigns.map(c => <div className="campaign-row" key={c.id}><div><strong>{c.title}</strong><small>{formatVietnamDateTime(c.createdAt)}</small></div><span>{c.readCount}/{c.recipientCount} đã đọc</span></div>)}{campaigns.length === 0 && <div className="control-empty">Chưa có broadcast.</div>}</article></div>
  </section>;
}

const permissionGroups: Record<string, string[]> = {
  SuperAdmin: ['Toàn bộ quyền'], Admin: ['Toàn bộ quyền'],
  Operations: ['Audit', 'Jobs', 'Market Data', 'Notification', 'Integrity read', 'Backup create'],
  Support: ['Audit', 'Users read', 'Revoke session', 'Notification'],
  MarketDataManager: ['Audit', 'Operations read', 'Market Data'],
  Auditor: ['Audit', 'Operations read', 'Users read', 'Integrity read', 'Backup read'],
  User: ['Không có quyền Admin'],
};
export function RolesPermissionsPage() {
  const [caps, setCaps] = useState<AdminCapabilities | null>(null);
  useEffect(() => { void adminApi.getCapabilities().then(setCaps); }, []);
  return <section className="control-page"><header className="control-hero"><div><span>Authorization</span><h1>Role & Permission</h1><p>Ma trận quyền cố định, least-privilege và dễ audit.</p></div></header>
    <div className="control-grid three">{Object.entries(permissionGroups).map(([role, permissions]) => <article className={`control-card glass-panel ${caps?.role === role ? 'highlight' : ''}`} key={role}><div className="control-card-head"><h3>{role}</h3>{caps?.role === role && <span className="status-pill ready">Vai trò của bạn</span>}</div><ul>{permissions.map(item => <li key={item}>{item}</li>)}</ul></article>)}</div>
  </section>;
}

export function DataIntegrityPage() {
  const [report, setReport] = useState<IntegrityReport | null>(null); const [message, setMessage] = useState('');
  const load = useCallback(async () => setReport(await adminApi.getIntegrityReport()), []);
  useEffect(() => { void load(); }, [load]);
  const repair = async (dryRun: boolean) => { const result = await adminApi.repairIntegrity('expired-sessions', dryRun); setMessage(`${dryRun ? 'Dry run' : 'Đã xử lý'}: ${result.affected} session.`); await load(); };
  return <section className="control-page"><header className="control-hero"><div><span>Data Governance</span><h1>Data Integrity Center</h1><p>Kiểm tra sức khỏe dữ liệu; chỉ repair thao tác đã whitelist và idempotent.</p></div><button onClick={load}>Quét lại</button></header>{message && <div className="control-alert">{message}</div>}
    <div className="control-grid three">{report?.checks.map(check => <article className="control-card glass-panel" key={check.key}><div className="control-card-head"><h3>{check.label}</h3><span className={`status-pill ${check.status === 'Healthy' ? 'ready' : 'failed'}`}>{check.status}</span></div><strong className="integrity-count">{check.count}</strong><p>Mức độ: {check.severity}</p>{check.key === 'expired-sessions' && check.count > 0 && <div className="button-row"><button onClick={() => repair(true)}>Dry run</button><button className="danger-button" onClick={() => repair(false)}>Repair</button></div>}</article>)}</div>
  </section>;
}

export function BackupConfigurationPage() {
  const [backups, setBackups] = useState<DatabaseBackup[]>([]); const [config, setConfig] = useState<AdminSystemConfiguration | null>(null);
  const [busy, setBusy] = useState(''); const [message, setMessage] = useState('');
  const load = useCallback(async () => { const [rows, settings] = await Promise.all([adminApi.listBackups(), adminApi.getConfiguration()]); setBackups(rows); setConfig(settings); }, []);
  useEffect(() => { void load(); }, [load]);
  const create = async () => { setBusy('create'); try { await adminApi.createBackup(); setMessage('Backup đã được tạo và kiểm tra checksum.'); await load(); } finally { setBusy(''); } };
  const restore = async (fileName: string) => { if (!window.confirm(`Restore ${fileName}? Hệ thống sẽ tạo safety backup trước.`)) return; setBusy(fileName); try { await adminApi.restoreBackup(fileName); setMessage('Restore hoàn tất.'); } finally { setBusy(''); } };
  const save = async () => { if (!config) return; await adminApi.updateConfiguration(config.settings); setMessage('Đã lưu chính sách hệ thống.'); };
  return <section className="control-page"><header className="control-hero"><div><span>Recovery</span><h1>Backup & System Configuration</h1><p>Backup có checksum, safety restore và cấu hình không chứa secret.</p></div><button disabled={busy !== ''} onClick={create}>{busy === 'create' ? 'Đang tạo...' : 'Tạo backup'}</button></header>{message && <div className="control-alert">{message}</div>}
    <div className="control-grid two"><article className="control-card glass-panel"><h2>Runtime</h2><dl><div><dt>Database</dt><dd>{config?.runtime.databaseProvider}</dd></div><div><dt>Persistent backup path</dt><dd>{config?.runtime.backupDirectoryConfigured ? 'Configured' : 'Default path'}</dd></div><div><dt>Retention</dt><dd>{config?.runtime.retentionCount}</dd></div></dl></article><article className="control-card control-form glass-panel"><h2>Policy</h2><label><input type="checkbox" checked={config?.settings.BACKUP_SCHEDULE_ENABLED === 'true'} onChange={e => config && setConfig({ ...config, settings: { ...config.settings, BACKUP_SCHEDULE_ENABLED: String(e.target.checked) } })}/> Bật lịch backup</label><input placeholder="UTC schedule, ví dụ 02:00" value={config?.settings.BACKUP_SCHEDULE_UTC ?? ''} onChange={e => config && setConfig({ ...config, settings: { ...config.settings, BACKUP_SCHEDULE_UTC: e.target.value } })}/><button onClick={save}>Lưu policy</button></article></div>
    <div className="control-table-wrap glass-panel"><table className="control-table"><thead><tr><th>File</th><th>Thời gian</th><th>Dung lượng</th><th>Checksum</th><th /></tr></thead><tbody>{backups.map(item => <tr key={item.fileName}><td><strong>{item.fileName}</strong></td><td>{formatVietnamDateTime(item.createdAt)}</td><td>{(item.sizeBytes / 1024 / 1024).toFixed(2)} MB</td><td><code>{item.sha256.slice(0, 16)}…</code></td><td><button className="danger-button" disabled={busy !== ''} onClick={() => restore(item.fileName)}>Restore</button></td></tr>)}</tbody></table>{backups.length === 0 && <div className="control-empty">Chưa có backup.</div>}</div>
  </section>;
}
