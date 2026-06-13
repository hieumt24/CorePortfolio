const API_BASE_URL = 'http://localhost:5211/api';

export const settingsApi = {
  getSetting: async (key: string): Promise<string | null> => {
    try {
      const response = await fetch(`${API_BASE_URL}/settings/${key}`);
      if (!response.ok) {
        return null;
      }
      const data = await response.json();
      return data.value;
    } catch (error) {
      console.error('Error fetching setting:', error);
      return null;
    }
  },

  updateSetting: async (key: string, value: string): Promise<boolean> => {
    try {
      const response = await fetch(`${API_BASE_URL}/settings/${key}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ value }),
      });
      return response.ok;
    } catch (error) {
      console.error('Error updating setting:', error);
      return false;
    }
  }
};
