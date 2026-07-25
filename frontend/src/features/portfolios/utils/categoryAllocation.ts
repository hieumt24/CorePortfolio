import type { AssetSummaryDto } from '../types';

export type PortfolioAssetGroup = 'crypto' | 'fund' | 'stock';

export interface CategoryAllocation {
  key: PortfolioAssetGroup;
  label: string;
  valueVnd: number;
  percentage: number;
  assetCount: number;
}

const GROUP_LABELS: Record<PortfolioAssetGroup, string> = {
  crypto: 'Crypto',
  fund: 'CCQ / ETF',
  stock: 'Stock',
};

const normalizeCategory = (value: string) => value
  .normalize('NFD')
  .replace(/[\u0300-\u036f]/g, '')
  .replace(/đ/g, 'd')
  .toLowerCase();

export const classifyPortfolioCategory = (categoryName: string): PortfolioAssetGroup | null => {
  const normalized = normalizeCategory(categoryName);
  if (
    normalized.includes('crypto')
    || normalized.includes('tien ma hoa')
    || normalized.includes('tien dien tu')
  ) return 'crypto';

  if (
    normalized.includes('ccq')
    || normalized.includes('chung chi quy')
    || normalized.includes('mutual fund')
    || normalized.includes('fund')
    || normalized.includes('etf')
  ) return 'fund';

  if (
    normalized.includes('stock')
    || normalized.includes('co phieu')
    || normalized.includes('chung khoan')
    || normalized.includes('equity')
  ) return 'stock';

  return null;
};

export const calculateCategoryAllocations = (
  assets: AssetSummaryDto[],
  totalHoldingsVnd: number,
  usdToVndRate: number,
): CategoryAllocation[] => {
  const totals: Record<PortfolioAssetGroup, { valueVnd: number; assetCount: number }> = {
    crypto: { valueVnd: 0, assetCount: 0 },
    fund: { valueVnd: 0, assetCount: 0 },
    stock: { valueVnd: 0, assetCount: 0 },
  };

  assets.forEach(asset => {
    const group = classifyPortfolioCategory(asset.categoryName);
    if (!group) return;
    const valueVnd = asset.currency.toUpperCase() === 'USD'
      ? asset.currentValue * usdToVndRate
      : asset.currentValue;
    totals[group].valueVnd += valueVnd;
    if (asset.currentValue > 0) totals[group].assetCount += 1;
  });

  return (Object.keys(totals) as PortfolioAssetGroup[]).map(key => ({
    key,
    label: GROUP_LABELS[key],
    valueVnd: totals[key].valueVnd,
    percentage: totalHoldingsVnd > 0 ? (totals[key].valueVnd / totalHoldingsVnd) * 100 : 0,
    assetCount: totals[key].assetCount,
  }));
};
