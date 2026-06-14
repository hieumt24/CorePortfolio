export const TransactionType = {
  Buy: 0,
  Sell: 1,
  Deposit: 2,
  Withdrawal: 3,
  Dividend: 4
} as const;
export type TransactionType = typeof TransactionType[keyof typeof TransactionType];

export interface TransactionDto {
  id: string;
  type: TransactionType;
  quantity: number;
  price: number;
  timestamp: string;
}

export interface CreateTransactionRequest {
  portfolioId: string;
  assetId: string;
  type: TransactionType;
  quantity: number;
  price: number;
  currency?: string;
  timestamp?: string;
}

export interface UpdateTransactionRequest {
  type: TransactionType;
  quantity: number;
  price: number;
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
  date: string;
}

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
