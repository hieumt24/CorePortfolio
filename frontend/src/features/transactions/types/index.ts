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
}
