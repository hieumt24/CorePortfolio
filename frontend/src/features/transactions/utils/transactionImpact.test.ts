import { describe, expect, it } from 'vitest';
import { TransactionType } from '../types';
import { calculateCashImpact } from './transactionImpact';

describe('calculateCashImpact', () => {
  it('includes fees when buying', () => {
    expect(calculateCashImpact(TransactionType.Buy, 10, 100, 5)).toBe(-1005);
  });

  it('deducts fees from sale proceeds', () => {
    expect(calculateCashImpact(TransactionType.Sell, 10, 100, 5)).toBe(995);
  });
});
