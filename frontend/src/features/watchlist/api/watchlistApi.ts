import { apiClient } from '../../../shared/api/baseClient';
import type { WatchlistDto, AddToWatchlistCommand } from '../types';

export const watchlistApi = {
  getWatchlist: async (): Promise<WatchlistDto[]> => {
    return apiClient<WatchlistDto[]>('/watchlist');
  },

  addToWatchlist: async (data: AddToWatchlistCommand): Promise<{ id: string }> => {
    return apiClient<{ id: string }>('/watchlist', {
      method: 'POST',
      body: JSON.stringify(data)
    });
  },

  removeFromWatchlist: async (id: string): Promise<void> => {
    return apiClient<void>(`/watchlist/${id}`, {
      method: 'DELETE'
    });
  }
};
