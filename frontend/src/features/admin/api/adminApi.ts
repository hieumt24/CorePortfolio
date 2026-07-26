import { apiClient } from '../../../shared/api/baseClient';
import type {
  AdminOverview,
  AdminUser,
  AdminUserFilters,
  AuditEventPage,
  PaginatedResult,
  ProductionOperations,
  AdminCapabilities,
  AdminUserDetail,
  UserSession,
  SecurityEvent,
  MarketDataHealth,
  NotificationCampaign,
  IntegrityReport,
  DatabaseBackup,
  AdminSystemConfiguration,
} from '../types';

export const adminApi = {
  getOverview: () => apiClient<AdminOverview>('/admin/overview'),
  getOperations: () => apiClient<ProductionOperations>('/admin/operations'),
  getAuditEvents: (pageSize = 8) =>
    apiClient<AuditEventPage>(`/admin/audit-events?page=1&pageSize=${pageSize}`),

  getUsers: (filters: AdminUserFilters) => {
    const params = new URLSearchParams();
    if (filters.search) params.set('search', filters.search);
    if (filters.role) params.set('role', filters.role);
    if (filters.isActive !== undefined) params.set('isActive', String(filters.isActive));
    if (filters.isOnline !== undefined) params.set('isOnline', String(filters.isOnline));
    params.set('page', String(filters.page ?? 1));
    params.set('pageSize', String(filters.pageSize ?? 20));
    return apiClient<PaginatedResult<AdminUser>>(`/admin/users?${params.toString()}`);
  },

  updateUserAccess: (id: string, role: AdminUser['role'], isActive: boolean) =>
    apiClient<AdminUser>(`/admin/users/${id}/access`, {
      method: 'PUT',
      body: JSON.stringify({ role, isActive }),
    }),

  getCapabilities: () =>
    apiClient<AdminCapabilities>('/admin/control-plane/capabilities'),
  getAuditPage: (filters: Record<string, string | number | undefined>) => {
    const params = new URLSearchParams();
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== '') params.set(key, String(value));
    });
    return apiClient<AuditEventPage>(`/admin/control-plane/audit-events?${params}`);
  },
  getUserDetail: (id: string) =>
    apiClient<AdminUserDetail>(`/admin/control-plane/users/${id}`),
  getUserSessions: (id: string) =>
    apiClient<UserSession[]>(`/admin/control-plane/users/${id}/sessions`),
  getSecurityTimeline: (id: string) =>
    apiClient<SecurityEvent[]>(`/admin/control-plane/users/${id}/security-timeline`),
  revokeSessions: (id: string, sessionId?: string) =>
    apiClient<{ revoked: number }>(`/admin/control-plane/users/${id}/sessions/revoke`, {
      method: 'POST',
      body: JSON.stringify({ sessionId: sessionId || null, reason: 'Thu hồi bởi quản trị viên' }),
    }),
  updateRole: (id: string, role: string) =>
    apiClient<void>(`/admin/control-plane/users/${id}/role`, {
      method: 'PUT',
      body: JSON.stringify({ role }),
    }),
  getMarketDataHealth: () =>
    apiClient<MarketDataHealth>('/admin/control-plane/market-data'),
  runJob: (name: string) =>
    apiClient<unknown>(`/admin/control-plane/jobs/${name}/run`, { method: 'POST' }),
  getNotificationCampaigns: () =>
    apiClient<NotificationCampaign[]>('/admin/control-plane/notification-campaigns'),
  broadcastNotification: (payload: {
    title: string; message: string; severity: number; role?: string; link?: string; expiresAt?: string;
  }) => apiClient<{ recipients: number }>('/admin/control-plane/notification-campaigns', {
    method: 'POST',
    body: JSON.stringify(payload),
  }),
  getIntegrityReport: () =>
    apiClient<IntegrityReport>('/admin/control-plane/data-integrity'),
  repairIntegrity: (checkKey: string, dryRun: boolean) =>
    apiClient<{ affected: number; dryRun: boolean }>('/admin/control-plane/data-integrity/repair', {
      method: 'POST',
      body: JSON.stringify({ checkKey, dryRun }),
    }),
  getConfiguration: () =>
    apiClient<AdminSystemConfiguration>('/admin/control-plane/configuration'),
  updateConfiguration: (settings: Record<string, string>) =>
    apiClient<boolean>('/admin/control-plane/configuration', {
      method: 'PUT',
      body: JSON.stringify({ settings }),
    }),
  listBackups: () => apiClient<DatabaseBackup[]>('/admin/migration/backups'),
  createBackup: () => apiClient<DatabaseBackup>('/admin/migration/backup', { method: 'POST' }),
  restoreBackup: (fileName: string) =>
    apiClient<unknown>('/admin/migration/restore', {
      method: 'POST',
      body: JSON.stringify({ fileName, confirmation: 'RESTORE' }),
    }),
};
