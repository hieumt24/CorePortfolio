import { describe, expect, it } from 'vitest';
import { deriveTransactionAmount } from './transactionAmount';

describe('deriveTransactionAmount', () => {
  it('derives total from quantity and price', () => {
    expect(deriveTransactionAmount(
      { quantity: 2, price: 150, total: 0 },
      ['quantity', 'price'],
    )).toEqual({ total: 300 });
  });

  it('derives price from quantity and total', () => {
    expect(deriveTransactionAmount(
      { quantity: 2, price: 0, total: 300 },
      ['quantity', 'total'],
    )).toEqual({ price: 150 });
  });

  it('derives quantity from price and total', () => {
    expect(deriveTransactionAmount(
      { quantity: 0, price: 150, total: 300 },
      ['price', 'total'],
    )).toEqual({ quantity: 2 });
  });
});
