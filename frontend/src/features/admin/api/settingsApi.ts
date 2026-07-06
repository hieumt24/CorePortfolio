import { apiClient } from '../../../shared/api/baseClient';

export const settingsApi = {
  getSetting: async (key: string): Promise<string | null> => {
    try {
      const data = await apiClient<{ key: string; value: string }>(`/settings/${key}`);
      return data.value;
    } catch (error) {
      console.error('Error fetching setting:', error);
      return null;
    }
  },

  updateSetting: async (key: string, value: string): Promise<boolean> => {
    try {
      await apiClient<void>(`/admin/settings/${key}`, {
        method: 'PUT',
        body: JSON.stringify({ value }),
      });
      return true;
    } catch (error) {
      console.error('Error updating setting:', error);
      return false;
    }
  }
};
