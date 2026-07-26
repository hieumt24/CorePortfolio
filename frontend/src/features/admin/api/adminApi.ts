import { apiClient } from '../../../shared/api/baseClient';
import type {
  AdminOverview,
  AdminUser,
  AdminUserFilters,
  AuditEventPage,
  PaginatedResult,
  ProductionOperations,
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
};
