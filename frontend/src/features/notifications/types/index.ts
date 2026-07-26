export type NotificationType =
  | 'System'
  | 'Budget'
  | 'SavingGoal'
  | 'Dca'
  | 'Rebalancing'
  | 'MarketPrice'
  | 'RecurringCashflow';

export type NotificationSeverity = 'Info' | 'Warning' | 'Critical';

export interface NotificationItem {
  id: string;
  type: NotificationType;
  severity: NotificationSeverity;
  title: string;
  message: string;
  link: string | null;
  entityType: string | null;
  entityId: string | null;
  actionLabel: string | null;
  metadataJson: string | null;
  createdAt: string;
  readAt: string | null;
  dismissedAt: string | null;
  expiresAt: string | null;
}

export interface NotificationPreference {
  type: NotificationType;
  isEnabled: boolean;
  warningThreshold: number | null;
  criticalThreshold: number | null;
  updatedAt: string | null;
}

export interface NotificationPreferenceInput {
  type: NotificationType;
  isEnabled: boolean;
  warningThreshold?: number | null;
  criticalThreshold?: number | null;
}

export interface NotificationListParams {
  unreadOnly?: boolean;
  type?: NotificationType;
  severity?: NotificationSeverity;
  page?: number;
  pageSize?: number;
}

export interface PaginatedNotifications {
  items: NotificationItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface UnreadNotificationCount {
  count: number;
}
