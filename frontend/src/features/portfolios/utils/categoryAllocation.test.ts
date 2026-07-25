import { describe, expect, it } from 'vitest';
import type { AssetSummaryDto } from '../types';
import { calculateCategoryAllocations, classifyPortfolioCategory } from './categoryAllocation';

const asset = (
  categoryName: string,
  currency: string,
  currentValue: number,
): AssetSummaryDto => ({
  assetId: crypto.randomUUID(),
  marketAssetId: crypto.randomUUID(),
  symbol: 'TEST',
  name: 'Test asset',
  categoryName,
  currency,
  currentPrice: 0,
  totalQuantity: 1,
  totalCost: 0,
  currentValue,
  totalBought: 0,
  averageCost: 0,
  realizedPnl: 0,
  unrealizedPnl: 0,
  fees: 0,
  priceUpdatedAt: new Date(0).toISOString(),
});

describe('portfolio category allocation', () => {
  it('recognizes Vietnamese and English category names', () => {
    expect(classifyPortfolioCategory('Tiền mã hóa')).toBe('crypto');
    expect(classifyPortfolioCategory('Chứng chỉ quỹ')).toBe('fund');
    expect(classifyPortfolioCategory('Cổ phiếu')).toBe('stock');
    expect(classifyPortfolioCategory('ETF')).toBe('fund');
  });

  it('converts USD holdings to VND before calculating percentages', () => {
    const result = calculateCategoryAllocations([
      asset('Crypto', 'USD', 2),
      asset('Cổ phiếu', 'VND', 50_000),
      asset('CCQ', 'VND', 25_000),
    ], 125_000, 25_000);

    expect(result.find(item => item.key === 'crypto')?.percentage).toBe(40);
    expect(result.find(item => item.key === 'stock')?.percentage).toBe(40);
    expect(result.find(item => item.key === 'fund')?.percentage).toBe(20);
  });
});
