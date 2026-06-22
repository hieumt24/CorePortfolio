export interface WatchlistDto {
  id: string;
  marketAssetId: string;
  symbol: string;
  name: string;
  currentPrice: number;
  targetPrice?: number;
  addedAt: string;
  assetCategoryName: string;
}

export interface AddToWatchlistCommand {
  marketAssetId: string;
  targetPrice?: number;
}
