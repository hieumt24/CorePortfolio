export const TransactionType = {
  Buy: 0,
  Sell: 1,
  Deposit: 2,
  Withdrawal: 3,
  Dividend: 4,
  Earn: 5
} as const;
export type TransactionType = typeof TransactionType[keyof typeof TransactionType];

export type TransactionAssetGroup = 'all' | 'crypto' | 'stock' | 'fund';

export interface TransactionDto {
  id: string;
  type: TransactionType;
  quantity: number;
  price: number;
  fee: number;
  notes: string;
  timestamp: string;
}

export interface CreateTransactionRequest {
  portfolioId: string;
  assetId: string;
  type: TransactionType;
  quantity: number;
  price: number;
  fee?: number;
  notes?: string;
  currency?: string;
  timestamp?: string;
}

export interface UpdateTransactionRequest {
  type: TransactionType;
  quantity: number;
  price: number;
  fee?: number;
  notes?: string;
  currency?: string;
  timestamp?: string;
}

export interface GlobalTransactionDto {
  id: string;
  portfolioId: string;
  portfolioName: string;
  assetId: string;
  symbol: string;
  assetName: string;
  categoryName: string;
  currency: string;
  type: TransactionType;
  quantity: number;
  price: number;
  fee: number;
  notes: string;
  date: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface TransactionFacetCounts {
  all: number;
  crypto: number;
  stock: number;
  fund: number;
}

export interface TransactionPageResult extends PaginatedResult<GlobalTransactionDto> {
  facets: TransactionFacetCounts;
}

export interface TransactionSearchFilters {
  portfolioId?: string;
  assetId?: string;
  type?: number;
  startDate?: string;
  endDate?: string;
  search?: string;
  assetGroup?: TransactionAssetGroup;
  minAmount?: number;
  maxAmount?: number;
  sortBy?: 'date' | 'amount' | 'quantity' | 'fee' | 'symbol';
  sortDirection?: 'asc' | 'desc';
  page?: number;
  pageSize?: number;
}
