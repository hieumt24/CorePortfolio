import { TransactionType } from '../types';
import type { TransactionType as TransactionTypeValue } from '../types';

export const calculateCashImpact = (
  type: TransactionTypeValue,
  quantity: number,
  price: number,
  fee: number,
) => {
  const gross = quantity * price;
  if (type === TransactionType.Buy) return -(gross + fee);
  if (type === TransactionType.Sell || type === TransactionType.Dividend) return gross - fee;
  if (type === TransactionType.Deposit) return gross;
  if (type === TransactionType.Withdrawal) return -gross;
  return 0;
};
