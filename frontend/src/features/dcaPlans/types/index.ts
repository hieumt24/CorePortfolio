export const DcaFrequency = {
  Weekly: 0,
  Monthly: 1,
  Quarterly: 2,
} as const;

export type DcaFrequency = typeof DcaFrequency[keyof typeof DcaFrequency];

export interface DcaPlan {
  id: string;
  portfolioId: string;
  portfolioName: string;
  marketAssetId: string;
  symbol: string;
  assetName: string;
  categoryName: string;
  currentPrice: number;
  amount: number;
  currency: string;
  frequency: DcaFrequency;
  startDate: string;
  nextExecutionDate: string;
  endDate: string | null;
  isActive: boolean;
  notes: string;
  estimatedQuantity: number;
  cashBalance: number;
  hasEnoughCash: boolean;
  upcomingExecutions: string[];
}

export interface DcaMarketAsset {
  id: string;
  categoryId: string;
  categoryName: string;
  symbol: string;
  name: string;
  currentPrice: number;
  currency: string;
}

export interface SaveDcaPlanRequest {
  portfolioId: string;
  marketAssetId: string;
  amount: number;
  currency: string;
  frequency: DcaFrequency;
  startDate: string;
  nextExecutionDate: string;
  endDate?: string | null;
  isActive: boolean;
  notes: string;
}
