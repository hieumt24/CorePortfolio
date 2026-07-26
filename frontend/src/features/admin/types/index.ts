export interface AssetCategory {
  id: string;
  name: string;
  defaultCurrency: string;
}

export interface CreateCategoryRequest {
  name: string;
  defaultCurrency: string;
}

export interface MarketAsset {
  id: string;
  categoryId: string;
  categoryName: string;
  symbol: string;
  name: string;
  currentPrice: number;
  lastUpdated: string;
  priceSource: 'Manual' | 'KBS' | 'CoinGecko' | string;
  externalId: string | null;
  priceStatus: 'Manual' | 'Fresh' | 'Stale' | 'Error' | string;
  lastPriceError: string | null;
}

export interface CreateMarketAssetRequest {
  categoryId: string;
  symbol: string;
  name: string;
  currentPrice: number;
  priceSource?: string;
  externalId?: string | null;
}

export interface KbsInstrument {
  symbol: string;
  marketId: string;
  securityGroupId: string;
  shortName: string;
  name: string;
  indexName: string[];
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PriceRefreshResult {
  marketAssetId: string;
  symbol: string;
  status: string;
  price: number | null;
  error: string | null;
}

export interface SyncVn100Result {
  providerCount: number;
  created: number;
  updated: number;
  unchanged: number;
  withReferencePrice: number;
}

export interface SyncFundsResult {
  providerCount: number;
  created: number;
  updated: number;
  unchanged: number;
  withNav: number;
}

export interface AdminOverview {
  totalUsers: number;
  activeUsers: number;
  adminUsers: number;
  totalPortfolios: number;
  totalAssets: number;
  totalTransactions: number;
  totalCashflows: number;
  totalMarketAssets: number;
  marketAssetsNeedingAttention: number;
  generatedAt: string;
}

export interface AdminUser {
  id: string;
  username: string;
  role: string;
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
  lastLoginIpAddress: string | null;
  lastActivityAt: string | null;
  isOnline: boolean;
  portfolioCount: number;
  transactionCount: number;
}

export interface AdminUserFilters {
  search?: string;
  role?: string;
  isActive?: boolean;
  isOnline?: boolean;
  page?: number;
  pageSize?: number;
}

export interface BackgroundJobStatus {
  name: string;
  state: 'NeverRun' | 'Running' | 'Succeeded' | 'Failed' | string;
  lastStartedAt: string | null;
  lastSucceededAt: string | null;
  lastFailedAt: string | null;
  successCount: number;
  failureCount: number;
  lastDurationMilliseconds: number | null;
  lastError: string | null;
}

export interface ProductionOperations {
  isMaintenanceMode: boolean;
  maintenanceReason: string | null;
  maintenanceStartedAt: string | null;
  jobs: BackgroundJobStatus[];
}

export interface AuditEvent {
  id: string;
  actorUserId: string | null;
  action: string;
  entityType: string;
  entityId: string | null;
  outcome: string;
  ipAddress: string | null;
  correlationId: string | null;
  metadataJson: string | null;
  occurredAt: string;
  actorUsername?: string | null;
}

export interface AdminCapabilities {
  role: string;
  roles: string[];
  permissions: string[];
}

export interface AdminUserDetail extends AdminUser {
  displayName: string | null;
  email: string | null;
  cashflowCount: number;
  activeSessionCount: number;
}

export interface UserSession {
  id: string;
  ipAddress: string | null;
  userAgent: string | null;
  createdAt: string;
  lastSeenAt: string;
  expiresAt: string;
  revokedAt: string | null;
  revokeReason: string | null;
  isActive: boolean;
}

export interface SecurityEvent {
  id: string;
  action: string;
  outcome: string;
  ipAddress: string | null;
  metadataJson: string | null;
  occurredAt: string;
}

export interface ProviderHealth {
  provider: string;
  total: number;
  fresh: number;
  stale: number;
  errors: number;
  lastUpdated: string;
}

export interface MarketDataAttention {
  id: string;
  symbol: string;
  name: string;
  priceSource: string;
  priceStatus: string;
  lastUpdated: string;
  lastPriceError: string | null;
}

export interface MarketDataHealth {
  providers: ProviderHealth[];
  attention: MarketDataAttention[];
  generatedAt: string;
}

export interface NotificationCampaign {
  id: string;
  title: string;
  message: string;
  severity: string;
  createdAt: string;
  expiresAt: string | null;
  recipientCount: number;
  readCount: number;
}

export interface IntegrityCheck {
  key: string;
  label: string;
  count: number;
  severity: string;
  status: string;
}

export interface IntegrityReport {
  checks: IntegrityCheck[];
  generatedAt: string;
}

export interface DatabaseBackup {
  fileName: string;
  createdAt: string;
  sizeBytes: number;
  sha256: string;
  schemaVersion: string;
}

export interface AdminSystemConfiguration {
  settings: Record<string, string>;
  runtime: {
    backupDirectoryConfigured: boolean;
    retentionCount: number;
    databaseProvider: string;
  };
}

export interface AuditEventPage {
  items: AuditEvent[];
  total: number;
  page: number;
  pageSize: number;
}
