import { apiClient } from '../../../shared/api/baseClient';
import type {
  NotificationListParams,
  NotificationPreference,
  NotificationPreferenceInput,
  PaginatedNotifications,
  UnreadNotificationCount,
} from '../types';

export const notificationsApi = {
  list: (params: NotificationListParams = {}) => {
    const searchParams = new URLSearchParams();
    if (params.unreadOnly !== undefined) searchParams.set('unreadOnly', String(params.unreadOnly));
    if (params.type) searchParams.set('type', params.type);
    if (params.severity) searchParams.set('severity', params.severity);
    if (params.page) searchParams.set('page', String(params.page));
    if (params.pageSize) searchParams.set('pageSize', String(params.pageSize));
    const query = searchParams.toString();
    return apiClient<PaginatedNotifications>(`/notifications${query ? `?${query}` : ''}`);
  },
  getUnreadCount: () => apiClient<UnreadNotificationCount>('/notifications/unread-count'),
  markRead: (id: string) => apiClient<void>(`/notifications/${id}/read`, { method: 'POST' }),
  markAllRead: () => apiClient<void>('/notifications/read-all', { method: 'POST' }),
  dismiss: (id: string) => apiClient<void>(`/notifications/${id}`, { method: 'DELETE' }),
  getPreferences: () => apiClient<NotificationPreference[]>('/notifications/preferences'),
  updatePreferences: (preferences: NotificationPreferenceInput[]) =>
    apiClient<NotificationPreference[]>('/notifications/preferences', {
      method: 'PUT',
      body: JSON.stringify({ preferences }),
    }),
};
