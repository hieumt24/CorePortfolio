import { apiClient } from '../../../shared/api/baseClient';
import type { SaveSavingGoalRequest, SavingGoal } from '../types';

export const savingGoalsApi = {
  getGoals: () => apiClient<SavingGoal[]>('/saving-goals'),

  createGoal: (request: SaveSavingGoalRequest) =>
    apiClient<{ id: string }>('/saving-goals', {
      method: 'POST',
      body: JSON.stringify(request),
    }),

  updateGoal: (id: string, request: SaveSavingGoalRequest) =>
    apiClient<void>(`/saving-goals/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    }),

  deleteGoal: (id: string) =>
    apiClient<void>(`/saving-goals/${id}`, {
      method: 'DELETE',
    }),
};
