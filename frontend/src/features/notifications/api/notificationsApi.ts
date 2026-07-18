import { apiClient } from '../../../shared/api/baseClient';

export interface NotificationItem {
  id: string;
  type: string;
  severity: string;
  title: string;
  message: string;
  link?: string;
  createdAt: string;
  readAt?: string;
}

export const notificationsApi = {
  list: (unreadOnly = false) => apiClient<NotificationItem[]>(`/notifications?unreadOnly=${unreadOnly}`),
  markRead: (id: string) => apiClient<void>(`/notifications/${id}/read`, { method: 'POST' }),
  markAllRead: () => apiClient<void>('/notifications/read-all', { method: 'POST' }),
};
